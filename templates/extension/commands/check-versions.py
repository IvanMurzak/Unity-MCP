#!/usr/bin/env python3
"""check-extension-versions — assert a Unity extension's committed version state
is self-consistent so version skew is a *red build*, never a latent surprise.

Authored once in the software repo and **synced into each extension** by the
release cascade as ``commands/check-versions.py`` (design D10). The synced copy
runs in each extension's CI (``python commands/check-versions.py``) and as a
cascade gate (step 03b) with **no core checkout available** — so the expected
McpPlugin/ReflectorNet versions, the core version they were synced for, and the
known-good Unity-version list are **injected at sync time** (see the sibling
``inject-checker-versions.py``); the authored copy here carries placeholder
sentinels.

Assertions (04-version-sync §4.2 — all messages are ``file:line``-precise):

  C1  Extension ``package.json`` core pin X exists and is a valid semver.
  C2  Every ``packages-lock.json`` *registry* core entry ``version`` == X.
  C3  Every ``packages-lock.json`` extension-entry transitive core requirement == X.
  C4  Every *registry* core entry: ``source == "registry"`` and
      ``url == https://package.openupm.com`` (supply-chain guard, 09 T1).
  C5  Every ``.nuget-installed.json`` McpPlugin == Y and ReflectorNet == Z
      (skipped for the Template — no DLL refresh, D12; **degrades to a WARNING**
      when the extension pin X != X_SYNCED_FOR — "DLL expectations unverified").
  C6  DLL presence: ``McpPlugin.dll`` + ``ReflectorNet.dll`` exist in every
      project's ``Assets/Plugins/NuGet/`` (skipped for the Template — D12/D14).
  C7  Unity-version lockstep: every ``Unity-Tests/<ver>`` also exists in the
      synced known-good list of core canonical-drop versions.
  C8  Compile-gate defines: in every project's ``ProjectSettings.asset``
      ``scriptingDefineSymbols``, each platform line carrying the core's ready
      define (``UNITY_MCP_READY``) carries **exactly one** dependency-generation
      define (``<prefix><n>``, e.g. ``UNITY_MCP_DEPS_6``) and every such line in
      the extension carries the **same** one (structural — always strict); and
      that one equals the core's current generation (**degrades to a WARNING**
      when X != X_SYNCED_FOR, like C5). Extension CI skips the NuGet resolver, so
      a missing define drops the core out of compilation (CS0234) — and the
      cascade (03b) pushes without waiting for that CI. A line without the ready
      define is never checked, so the Template (no ready define) passes trivially.

The Template (D12/D14) runs the **text-level subset only: C1–C4 + C7 + C8** — C5/C6
are skipped because it is never DLL-injected nor opened in Unity. A lock's core
entry with ``source == "local"`` is a Template scaffold reference (a ``file:``
path, not a published registry pin), so C2/C4 do not apply to it — real
extensions always carry a *registry* core entry (that is what C2/C4 guard).

Exit: 0 clean · 1 violations (listed) · 2 usage error. Pure stdlib; no network.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import NamedTuple

# --------------------------------------------------------------------------- #
# Identity / invariant constants (authored — travel verbatim into every copy)
# --------------------------------------------------------------------------- #

CORE_PACKAGE_ID = "com.ivanmurzak.unity.mcp"
MCPPLUGIN_PKG_ID = "com.IvanMurzak.McpPlugin"
REFLECTORNET_PKG_ID = "com.IvanMurzak.ReflectorNet"
OPENUPM_URL = "https://package.openupm.com"

# Bump when the checker's *logic* changes. Travels verbatim into each synced
# copy; cascade step 03b compares it across the fleet to detect checker drift
# (04 §4.3). This is authored, NOT injected — it is the checker's own version.
CHECKER_VERSION = "2"  # 2: + C8 compile-gate defines

# --------------------------------------------------------------------------- #
# Injected-at-sync-time constants (placeholder sentinels in the authored copy).
# inject-checker-versions.py substitutes these from the core repo. A copy still
# carrying a sentinel degrades gracefully (the check that needs it is skipped /
# warned) rather than asserting against a placeholder.
# --------------------------------------------------------------------------- #

EXPECTED_MCPPLUGIN = "8.7.0"
EXPECTED_REFLECTORNET = "5.4.1"
X_SYNCED_FOR = "0.93.3"
KNOWN_GOOD_UNITY_VERSIONS = ["2022.3.62f3", "2023.2.22f1", "6000.3.1f1", "6000.5.0b3", "6000.6.0a2", "6000.6.3f1"]
# C8 — the core's compile-gate defines (NuGetConfig.cs names, cross-checked
# against the core asmdefs' defineConstraints by the injector).
READY_DEFINE = "UNITY_MCP_READY"
GENERATION_DEFINE_PREFIX = "UNITY_MCP_DEPS_"
EXPECTED_GENERATION_DEFINE = "UNITY_MCP_DEPS_7"

_PLACEHOLDER_PREFIX = "__INJECT_"
_SEMVER_RE = re.compile(r"^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.\-+]+)?$")

TEMPLATE_EXTENSION = "Unity-AI-Tools-Template"


class Violation(NamedTuple):
    code: str       # e.g. "C2"
    location: str   # "path:line" or "path"
    message: str


class Warning(NamedTuple):
    code: str
    location: str
    message: str


# --------------------------------------------------------------------------- #
# Small pure helpers
# --------------------------------------------------------------------------- #


def _is_placeholder(value: object) -> bool:
    """True for an un-injected sentinel value (string starting with __INJECT_)."""
    return isinstance(value, str) and value.startswith(_PLACEHOLDER_PREFIX)


def _is_placeholder_list(values: object) -> bool:
    return not isinstance(values, list) or any(_is_placeholder(v) for v in values)


def _is_semver(value: object) -> bool:
    return isinstance(value, str) and bool(_SEMVER_RE.match(value))


def _rel(path: Path, root: Path) -> str:
    """Path for messages — relative to the extension root when possible."""
    try:
        return str(path.relative_to(root)).replace("\\", "/")
    except ValueError:
        return str(path).replace("\\", "/")


def _loc(path: Path, root: Path, line: int | None) -> str:
    rel = _rel(path, root)
    return f"{rel}:{line}" if line else rel


def _key_line(text: str, key: str, *, after: int = 0, string_value: bool = False) -> int | None:
    """1-based line number of a JSON key ``"key":`` at/after 1-based line ``after``.

    ``string_value=True`` matches only ``"key": "..."`` (a string value), which
    distinguishes an extension's transitive *requirement* (a string) from the
    core *entry* object (``"key": {``) when both share the key name.
    """
    token = f'"{key}"'
    lines = text.splitlines()
    for i in range(max(after - 1, 0), len(lines)):
        ln = lines[i]
        idx = ln.find(token)
        if idx == -1:
            continue
        rest = ln[idx + len(token):].lstrip()
        if not rest.startswith(":"):
            continue
        if string_value and not rest[1:].lstrip().startswith('"'):
            continue
        return i + 1
    return None


def _discover_unity_projects(ext_root: Path) -> list[Path]:
    """Every Unity project under an extension: Unity-Package plus every
    Unity-Tests/<ver>/ — mirrors cli._discover_extension_unity_projects."""
    projects: list[Path] = []
    pkg = ext_root / "Unity-Package"
    if pkg.is_dir():
        projects.append(pkg)
    tests_root = ext_root / "Unity-Tests"
    if tests_root.is_dir():
        projects.extend(
            p for p in sorted(tests_root.iterdir())
            if p.is_dir() and not p.name.startswith(".")
        )
    return projects


def find_package_json(ext_root: Path) -> Path | None:
    """Locate the extension's UPM package.json (mirrors cli._find_package_json,
    but returns None instead of raising so main() can emit a usage error).

    New layout: Unity-Package/Packages/<pkg-id>/package.json (branded, or the
    Template placeholder folder). Old layout: Unity-Package/Assets/root/package.json.
    """
    packages_dir = ext_root / "Unity-Package" / "Packages"
    if packages_dir.is_dir():
        matches = sorted(packages_dir.glob(f"{CORE_PACKAGE_ID}.*/package.json"))
        if not matches:
            matches = sorted(packages_dir.glob("*/package.json"))
        if matches:
            return matches[0]
    old_pkg = ext_root / "Unity-Package" / "Assets" / "root" / "package.json"
    if old_pkg.is_file():
        return old_pkg
    return None


# `  scriptingDefineSymbols:` opens a YAML mapping of `    <Platform>: A;B;C`
# (platform keys may contain spaces). Mirrors cli._SDS_HEADER_RE/_SDS_ENTRY_RE.
_SDS_HEADER_RE = re.compile(r"^(?P<indent> *)scriptingDefineSymbols:[ \t]*$")
_SDS_ENTRY_RE = re.compile(r"^(?P<indent> +)(?P<key>[^:]+): ?(?P<value>.*?)$")


def _scripting_define_entries(text: str) -> list[tuple[int, str, list[str]]]:
    """``(line, platform, symbols)`` for every ``scriptingDefineSymbols`` entry."""
    out: list[tuple[int, str, list[str]]] = []
    block_indent: int | None = None
    for i, line in enumerate(text.splitlines(), start=1):
        if block_indent is not None:
            m = _SDS_ENTRY_RE.match(line)
            if m and len(m.group("indent")) > block_indent:
                out.append((i, m.group("key"), m.group("value").split(";")))
                continue
            block_indent = None
        h = _SDS_HEADER_RE.match(line)
        if h:
            block_indent = len(h.group("indent"))
    return out


def _read_json(path: Path) -> dict | None:
    try:
        return json.loads(path.read_text(encoding="utf-8", errors="replace"))
    except (OSError, json.JSONDecodeError):
        return None


# --------------------------------------------------------------------------- #
# The checker
# --------------------------------------------------------------------------- #


def check_extension(
    ext_root: Path,
    *,
    template: bool = False,
    expected_mcpplugin: str | None = None,
    expected_reflectornet: str | None = None,
    x_synced_for: str | None = None,
    known_good_unity_versions: list[str] | None = None,
    ready_define: str | None = None,
    generation_define_prefix: str | None = None,
    expected_generation_define: str | None = None,
) -> tuple[list[Violation], list[Warning]]:
    """Run C1–C8 against an extension checkout. Injected expectations default to
    this module's (possibly placeholder) constants; tests pass them explicitly."""
    if ready_define is None:
        ready_define = READY_DEFINE
    if generation_define_prefix is None:
        generation_define_prefix = GENERATION_DEFINE_PREFIX
    if expected_generation_define is None:
        expected_generation_define = EXPECTED_GENERATION_DEFINE
    if expected_mcpplugin is None:
        expected_mcpplugin = EXPECTED_MCPPLUGIN
    if expected_reflectornet is None:
        expected_reflectornet = EXPECTED_REFLECTORNET
    if x_synced_for is None:
        x_synced_for = X_SYNCED_FOR
    if known_good_unity_versions is None:
        known_good_unity_versions = KNOWN_GOOD_UNITY_VERSIONS

    violations: list[Violation] = []
    warnings: list[Warning] = []

    projects = _discover_unity_projects(ext_root)

    # --- C1: package.json core pin X exists and is a valid semver -----------
    pkg_json = find_package_json(ext_root)
    pin_x: str | None = None
    if pkg_json is not None and pkg_json.is_file():
        pkg_text = pkg_json.read_text(encoding="utf-8", errors="replace")
        pkg_data = _read_json(pkg_json) or {}
        pin_x = (pkg_data.get("dependencies") or {}).get(CORE_PACKAGE_ID)
        pin_line = _key_line(pkg_text, CORE_PACKAGE_ID)
        if pin_x is None:
            violations.append(Violation(
                "C1", _loc(pkg_json, ext_root, pin_line),
                f"core pin {CORE_PACKAGE_ID!r} missing from dependencies"))
            pin_x = None
        elif not _is_semver(pin_x):
            violations.append(Violation(
                "C1", _loc(pkg_json, ext_root, pin_line),
                f"core pin X={pin_x!r} is not a valid semver"))
            pin_x = None  # can't compare locks against an invalid pin

    # --- C2 / C3 / C4: per-project packages-lock.json -----------------------
    for project in projects:
        lock = project / "Packages" / "packages-lock.json"
        if not lock.is_file():
            continue  # a project without a lock asserts nothing (Template Unity-Package)
        lock_text = lock.read_text(encoding="utf-8", errors="replace")
        data = _read_json(lock)
        if not isinstance(data, dict):
            violations.append(Violation("C2", _loc(lock, ext_root, None),
                                        "packages-lock.json is not valid JSON"))
            continue
        deps = data.get("dependencies")
        if not isinstance(deps, dict):
            continue

        core = deps.get(CORE_PACKAGE_ID)
        if isinstance(core, dict) and core.get("source") != "local":
            core_line = _key_line(lock_text, CORE_PACKAGE_ID)
            # C4 — supply-chain guard on the registry core entry.
            src, url = core.get("source"), core.get("url")
            if src != "registry" or url != OPENUPM_URL:
                src_line = _key_line(lock_text, "source", after=core_line or 0) or core_line
                violations.append(Violation(
                    "C4", _loc(lock, ext_root, src_line),
                    f"core entry must be source=\"registry\" url={OPENUPM_URL!r}; "
                    f"got source={src!r} url={url!r}"))
            # C2 — registry core entry version must equal pin X.
            if pin_x is not None and core.get("version") != pin_x:
                ver_line = _key_line(lock_text, "version", after=core_line or 0) or core_line
                violations.append(Violation(
                    "C2", _loc(lock, ext_root, ver_line),
                    f"core entry version {core.get('version')!r} != pin X={pin_x!r}"))

        # C3 — every extension-entry transitive core requirement must equal X.
        for key, entry in deps.items():
            if not (key.startswith(CORE_PACKAGE_ID + ".") and isinstance(entry, dict)):
                continue
            req = (entry.get("dependencies") or {}).get(CORE_PACKAGE_ID)
            if req is not None and pin_x is not None and req != pin_x:
                ext_key_line = _key_line(lock_text, key)
                req_line = _key_line(lock_text, CORE_PACKAGE_ID,
                                     after=(ext_key_line or 0) + 1, string_value=True)
                violations.append(Violation(
                    "C3", _loc(lock, ext_root, req_line or ext_key_line),
                    f"{key} transitive requirement {req!r} != pin X={pin_x!r}"))

    # --- C5 / C6: vendored-DLL manifests + presence (skipped for Template) --
    if not template:
        degrade = _is_placeholder(x_synced_for) or (pin_x is not None and pin_x != x_synced_for)
        for project in projects:
            nuget_dir = project / "Assets" / "Plugins" / "NuGet"
            manifest = nuget_dir / ".nuget-installed.json"
            # C5 — .nuget-installed.json McpPlugin==Y, ReflectorNet==Z.
            if manifest.is_file() and not _is_placeholder(expected_mcpplugin):
                if degrade:
                    warnings.append(Warning(
                        "C5", _loc(manifest, ext_root, None),
                        f"DLL expectations unverified for X={pin_x!r} "
                        f"(X_SYNCED_FOR={x_synced_for!r}); strict (Y,Z) check deferred to next sync"))
                else:
                    m_text = manifest.read_text(encoding="utf-8", errors="replace")
                    pkgs = (_read_json(manifest) or {}).get("packages") or {}
                    got_mcp = (pkgs.get(MCPPLUGIN_PKG_ID) or {}).get("version")
                    got_refl = (pkgs.get(REFLECTORNET_PKG_ID) or {}).get("version")
                    if got_mcp != expected_mcpplugin:
                        mcp_line = _key_line(m_text, MCPPLUGIN_PKG_ID)
                        v_line = _key_line(m_text, "version", after=mcp_line or 0) or mcp_line
                        violations.append(Violation(
                            "C5", _loc(manifest, ext_root, v_line),
                            f"McpPlugin {got_mcp!r} != expected Y={expected_mcpplugin!r}"))
                    if got_refl != expected_reflectornet:
                        refl_line = _key_line(m_text, REFLECTORNET_PKG_ID)
                        v_line = _key_line(m_text, "version", after=refl_line or 0) or refl_line
                        violations.append(Violation(
                            "C5", _loc(manifest, ext_root, v_line),
                            f"ReflectorNet {got_refl!r} != expected Z={expected_reflectornet!r}"))
            # C6 — DLL presence (independent of version; not degraded).
            for dll in ("McpPlugin.dll", "ReflectorNet.dll"):
                if not (nuget_dir / dll).is_file():
                    violations.append(Violation(
                        "C6", _loc(nuget_dir / dll, ext_root, None),
                        f"vendored {dll} missing"))

    # --- C7: Unity-Tests versions must be a subset of the known-good list ----
    if not _is_placeholder_list(known_good_unity_versions):
        tests_root = ext_root / "Unity-Tests"
        if tests_root.is_dir():
            for d in sorted(tests_root.iterdir()):
                if not d.is_dir() or d.name.startswith("."):
                    continue
                if d.name not in known_good_unity_versions:
                    violations.append(Violation(
                        "C7", _loc(d, ext_root, None),
                        f"Unity-Tests version {d.name!r} not in the known-good core "
                        f"canonical-drop list {sorted(known_good_unity_versions)}"))

    # --- C8: compile-gate defines in every ProjectSettings.asset -------------
    # Runs for the Template too (text-level); a line without the ready define is
    # never checked, so a Template (none) passes trivially.
    if not (_is_placeholder(ready_define) or _is_placeholder(generation_define_prefix)):
        gen_re = re.compile(re.escape(generation_define_prefix) + r"\d+")
        seen: dict[str, str] = {}  # generation define -> first location carrying it
        for project in projects:
            asset = project / "ProjectSettings" / "ProjectSettings.asset"
            if not asset.is_file():
                continue
            text = asset.read_text(encoding="utf-8", errors="replace")
            for line, key, symbols in _scripting_define_entries(text):
                if ready_define not in symbols:
                    continue
                loc = _loc(asset, ext_root, line)
                gens = [s for s in symbols if gen_re.fullmatch(s)]
                if len(gens) != 1:
                    violations.append(Violation(
                        "C8", loc,
                        f"{key!r} carries {ready_define} but "
                        + (f"no {generation_define_prefix}<n> define" if not gens
                           else f"{len(gens)} generation defines {gens}")
                        + " — the core's gated assemblies will not compile in CI"))
                    continue
                seen.setdefault(gens[0], loc)
        if len(seen) > 1:
            violations.append(Violation(
                "C8", ".",
                "projects disagree on the dependency-generation define: "
                + ", ".join(f"{g} (first at {where})" for g, where in sorted(seen.items()))))
        elif seen and not _is_placeholder(expected_generation_define):
            (got, where), = seen.items()
            if got != expected_generation_define:
                degrade = _is_placeholder(x_synced_for) or (pin_x is not None and pin_x != x_synced_for)
                if degrade:
                    warnings.append(Warning(
                        "C8", where,
                        f"generation define {got} unverified for X={pin_x!r} "
                        f"(synced expectation {expected_generation_define} is for "
                        f"X_SYNCED_FOR={x_synced_for!r})"))
                else:
                    violations.append(Violation(
                        "C8", where,
                        f"generation define {got} != core's {expected_generation_define} "
                        f"for X={pin_x!r}"))

    return violations, warnings


def _is_template(ext_root: Path, pkg_json: Path | None, force: bool) -> bool:
    """A Template checkout runs the C1–C4+C7+C8 subset (C5/C6 skipped). Detected by
    the --template flag, the canonical folder name, or a placeholder package id
    (its ``name`` is not a branded ``com.ivanmurzak.unity.mcp.<name>`` id)."""
    if force or ext_root.name == TEMPLATE_EXTENSION:
        return True
    if pkg_json is not None and pkg_json.is_file():
        name = (_read_json(pkg_json) or {}).get("name")
        if isinstance(name, str) and not name.startswith(CORE_PACKAGE_ID + "."):
            return True
    return False


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    parser = argparse.ArgumentParser(
        prog="check-extension-versions",
        description="Assert a Unity extension's committed version state is self-consistent "
                    "(pin == locks == vendored-DLL versions). Exit 0 clean / 1 violations / 2 usage.",
    )
    parser.add_argument("extension_root", nargs="?", default=".",
                        help="Extension repo root (default: current directory).")
    parser.add_argument("--template", action="store_true",
                        help="Force Template mode (C1-C4+C7+C8 subset; skip C5/C6).")
    try:
        args = parser.parse_args(argv)
    except SystemExit as exc:  # argparse exits 2 on bad usage — honor the contract
        return int(exc.code) if isinstance(exc.code, int) else 2

    ext_root = Path(args.extension_root).resolve()
    if not ext_root.is_dir():
        print(f"error: extension root not found: {ext_root}", file=sys.stderr)
        return 2
    pkg_json = find_package_json(ext_root)
    if pkg_json is None:
        print(f"error: no Unity-Package/Packages/*/package.json under {ext_root} "
              f"(is this a Unity extension?)", file=sys.stderr)
        return 2

    template = _is_template(ext_root, pkg_json, args.template)
    violations, warnings = check_extension(ext_root, template=template)

    mode = "Template subset (C1-C4+C7+C8)" if template else "full (C1-C8)"
    for w in warnings:
        print(f"WARNING {w.code} {w.location}: {w.message}")
    if violations:
        print(f"\nFAIL {ext_root.name}: {len(violations)} consistency violation(s) "
              f"[checker v{CHECKER_VERSION}, {mode}]")
        for v in violations:
            print(f"  {v.code} {v.location}: {v.message}")
        return 1
    print(f"OK {ext_root.name}: version state consistent "
          f"[checker v{CHECKER_VERSION}, {mode}]")
    return 0


if __name__ == "__main__":
    sys.exit(main())
