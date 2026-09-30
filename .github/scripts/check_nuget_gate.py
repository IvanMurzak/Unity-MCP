#!/usr/bin/env python3
# ┌──────────────────────────────────────────────────────────────────┐
# │  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
# │  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
# │  Copyright (c) 2025 Ivan Murzak                                  │
# │  Licensed under the Apache License, Version 2.0.                 │
# │  See the LICENSE file in the project root for more information.  │
# └──────────────────────────────────────────────────────────────────┘
"""
Guards the invariant that broke 0.88.0 for every user (issue #957).

THE INVARIANT
-------------
The plugin's compiled code lives in asmdefs gated behind `defineConstraints`. Those defines are
stored in the CONSUMER's ProjectSettings, so they survive a package upgrade: they record that some
NuGet DLL set was restored, not WHICH one. When a package upgrade raises a NuGet pin, the still-alive
previous AppDomain evaluates the OUTGOING package's pins, concludes everything is installed, and
leaves the gate open — so Unity compiles the NEW sources against the OLD DLLs. The compile fails,
which blocks the domain reload, which stops the NEW resolver from ever repairing the DLL set.

The escape is `NuGetConfig.DependencyGenerationDefine`: bumped in lockstep with the pins, it is a
static asmdef-level signal that an install restored for an older generation cannot satisfy, so the
main assemblies are skipped, the compile succeeds, and the resolver self-heals on the next domain.

That only works if nobody ever changes a pin without bumping the define. This script enforces it:

  1. The pinned NuGet set and the generation define are read from NuGetConfig.cs and compared to the
     blessed pair recorded in `.github/nuget-gate.lock`. Pins changed => the define MUST have changed
     too. (fbbf3dab — "chore: bump McpPlugin to 8.1.0" — would have failed here.)
  2. Every asmdef gated on the ready define must ALSO be gated on the current generation define,
     and every ProjectSettings.asset scripting-define entry carrying the ready define must carry the
     current generation define — so a half-finished bump cannot ship either.

Usage:
    python .github/scripts/check_nuget_gate.py           # verify (CI)
    python .github/scripts/check_nuget_gate.py --write   # re-bless after a deliberate bump
"""

import argparse
import hashlib
import json
import os
import re
import sys

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NUGET_CONFIG = os.path.join(
    REPO_ROOT, "Unity-MCP-Plugin", "Packages", "com.ivanmurzak.unity.mcp",
    "Editor", "DependencyResolver", "NuGetConfig.cs")
LOCK_PATH = os.path.join(REPO_ROOT, ".github", "nuget-gate.lock")

# Directories that hold a Unity project whose ProjectSettings must carry the gate defines.
SCAN_ROOTS = ["Unity-MCP-Plugin", "Unity-Tests"]

PIN_RE = re.compile(r'new\s+NuGetPackage\(\s*"([^"]+)"\s*,\s*"([^"]+)"')
READY_RE = re.compile(r'ReadyDefine\s*=\s*"([^"]+)"')
GEN_PREFIX_RE = re.compile(r'DependencyGenerationDefinePrefix\s*=\s*"([^"]+)"')
GEN_SUFFIX_RE = re.compile(r'DependencyGenerationDefine\s*=\s*DependencyGenerationDefinePrefix\s*\+\s*"([^"]+)"')


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


def parse_config():
    """Extract the ready define, the generation define, and the ordered pin list."""
    text = read(NUGET_CONFIG)

    ready = READY_RE.search(text)
    prefix = GEN_PREFIX_RE.search(text)
    suffix = GEN_SUFFIX_RE.search(text)
    if not (ready and prefix and suffix):
        sys.exit("FAIL: could not parse ReadyDefine / DependencyGenerationDefine from "
                 + os.path.relpath(NUGET_CONFIG, REPO_ROOT)
                 + "\n      (this script pins their exact declaration shape — update both together)")

    pins = PIN_RE.findall(text)
    if not pins:
        sys.exit("FAIL: no `new NuGetPackage(\"id\", \"version\"...)` pins found — parser is stale.")

    return ready.group(1), prefix.group(1) + suffix.group(1), pins


def pins_digest(pins):
    """Order-insensitive, whitespace-insensitive digest of the pinned NuGet set."""
    canonical = "\n".join(sorted("%s@%s" % (pid, ver) for pid, ver in pins))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def iter_files(suffixes):
    for scan_root in SCAN_ROOTS:
        root = os.path.join(REPO_ROOT, scan_root)
        if not os.path.isdir(root):
            continue
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in (".git", "Library", "Temp", "obj")]
            for name in filenames:
                if name.endswith(suffixes):
                    yield os.path.join(dirpath, name)


def check_propagation(ready_define, generation_define):
    """Every place gated on the ready define must also carry the current generation define."""
    problems = []

    for path in iter_files((".asmdef",)):
        try:
            data = json.loads(read(path))
        except ValueError:
            continue  # not our business to police unrelated malformed asmdefs
        constraints = data.get("defineConstraints") or []
        if ready_define not in constraints:
            continue
        if generation_define not in constraints:
            problems.append("%s: defineConstraints has %r but not %r"
                            % (os.path.relpath(path, REPO_ROOT), ready_define, generation_define))

    for path in iter_files(("ProjectSettings.asset",)):
        for lineno, line in enumerate(read(path).splitlines(), 1):
            # scriptingDefineSymbols entries look like:  `    Standalone: FOO;UNITY_MCP_READY`
            if ready_define not in line:
                continue
            symbols = line.split(":", 1)[-1].strip().split(";")
            if ready_define not in symbols:
                continue
            if generation_define not in symbols:
                problems.append("%s:%d: scripting defines carry %r but not %r"
                                % (os.path.relpath(path, REPO_ROOT), lineno,
                                   ready_define, generation_define))

    return problems


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--write", action="store_true",
                        help="re-bless the lock after a deliberate pin + generation bump")
    args = parser.parse_args()

    ready_define, generation_define, pins = parse_config()
    digest = pins_digest(pins)

    failures = list(check_propagation(ready_define, generation_define))

    lock = None
    if os.path.exists(LOCK_PATH):
        lock = json.loads(read(LOCK_PATH))

    if args.write:
        if failures:
            print("Refusing to re-bless — fix the propagation problems first:")
            for problem in failures:
                print("  - " + problem)
            return 1
        if lock and lock.get("pinsSha256") != digest and \
                lock.get("dependencyGenerationDefine") == generation_define:
            print("Refusing to re-bless: the pinned NuGet set changed but "
                  "NuGetConfig.DependencyGenerationDefine is still %r.\n"
                  "Bump it (e.g. %s -> next number) so consumers upgrading from an older "
                  "generation skip the stale-DLL compile instead of dead-locking on it."
                  % (generation_define, generation_define))
            return 1
        with open(LOCK_PATH, "w", encoding="utf-8", newline="\n") as handle:
            json.dump({
                "_comment": "Blessed pairing of the pinned NuGet set and the asmdef gate "
                            "generation. Regenerate with: python .github/scripts/"
                            "check_nuget_gate.py --write  (see issue #957).",
                "dependencyGenerationDefine": generation_define,
                "pinsSha256": digest,
                "pins": ["%s@%s" % (pid, ver) for pid, ver in sorted(pins)],
            }, handle, indent=2)
            handle.write("\n")
        print("Blessed %s with %d pins (%s)." % (generation_define, len(pins), digest[:12]))
        return 0

    if lock is None:
        print("FAIL: %s is missing. Create it with:\n"
              "  python .github/scripts/check_nuget_gate.py --write"
              % os.path.relpath(LOCK_PATH, REPO_ROOT))
        return 1

    if lock.get("pinsSha256") != digest and \
            lock.get("dependencyGenerationDefine") == generation_define:
        failures.insert(0,
            "The pinned NuGet set changed but NuGetConfig.DependencyGenerationDefine is still %r.\n"
            "      A consumer upgrading from the previous release still has the OLD DLLs on disk and\n"
            "      the gate defines already set, so Unity would compile the new sources against them\n"
            "      and dead-lock in Safe Mode (issue #957).\n"
            "      Fix: bump DependencyGenerationDefine, propagate it to every asmdef +\n"
            "      ProjectSettings.asset, then run: python .github/scripts/check_nuget_gate.py --write"
            % generation_define)
    elif lock.get("pinsSha256") != digest or \
            lock.get("dependencyGenerationDefine") != generation_define:
        failures.insert(0,
            "%s is stale (pins and/or generation define changed).\n"
            "      Re-bless with: python .github/scripts/check_nuget_gate.py --write"
            % os.path.relpath(LOCK_PATH, REPO_ROOT))

    if failures:
        print("NuGet gate check FAILED:")
        for problem in failures:
            print("  - " + problem)
        return 1

    print("NuGet gate OK: %d pins, generation %s, propagation consistent."
          % (len(pins), generation_define))
    return 0


if __name__ == "__main__":
    sys.exit(main())
