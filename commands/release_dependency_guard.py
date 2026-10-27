#!/usr/bin/env python3
"""
Release dependency guard: refuse a release that would ship a dependency users cannot restore.

Between test waves the plugin's `main` may consume an UNRELEASED dependency: RQL packed from its main
commit as `com.IvanMurzak.Rql 0.3.0-dev.<sha>`, its DLLs committed to the test projects and the
asmdef gate generation (`UNITY_MCP_DEPS_<N>`) moved for it. Fine for CI; but the package's NuGet
resolver installs `NuGetConfig.Packages` from nuget.org in every consumer project, so a release cut in
that state ships a plugin users cannot restore. This guard runs as the FIRST step of `release.yml` and
`release_extensions.yml` and fails the run (dry runs too) before anything is built or published.

What it checks (everything the core, extension and CLI releases ship):
  * the NuGet pins of the resolver (`new NuGetPackage("<id>", "<version>")` in `NuGetConfig.cs`):
    a pre-release version (any `-` suffix) or a version not listed on nuget.org fails, and the
    resolver's `NuGetBaseUrl` must be the nuget.org flat container (no local / pre-release feed);
    a pin must be written exactly as nuget.org lists it (the resolver downloads `<id>/<version>` verbatim).
    Not covered: the transitive dependencies the resolver reads from those packages' nuspecs;
  * the vendored Code-Exec sources (`Editor/Scripts/CodeExec/VENDORED.md`): the recorded release must
    be a published `com.IvanMurzak.CodeExec` version (`release **<version>**`), not a pre-release;
  * the UPM dependencies of the core package and of every extension package
    (`extensions/*/Packages/*/package.json`): pre-release or non-registry (`file:`, git) specs fail;
  * the npm dependencies of `unity-mcp-cli` (`cli/package.json`) the same way.
Extensions ship no NuGet DLLs of their own: they compile against the core's resolver set (asmdefs gated
on the same generation define), so the core pins above are their NuGet pins too.

Usage:
    python commands/release_dependency_guard.py [--root <repo>]
Exit 0 = every pin is a published release; exit 1 = problems listed (file:line: message).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Optional

ROOT = Path(__file__).resolve().parents[1]
NUGET_INDEX_URL = "https://api.nuget.org/v3-flatcontainer/{id}/index.json"

NPM_PRERELEASE_RE = re.compile(r"\d+\.\d+\.\d+-")
# A registry semver spec (`1.2.3`, `^1.2`, `~1`, `>=1 <2`, `v1.2.3`); dist-tags, paths, git and URLs are not.
NPM_REGISTRY_SPEC_RE = re.compile(r"^\s*[0-9^~<>=v]")


@dataclass(frozen=True)
class Pin:
    """One dependency version as written in a shipped file."""

    ecosystem: str  # "nuget": pre-release + nuget.org check; "semver": pre-release check only
    package: str
    version: str
    file: Path
    line: int


@dataclass(frozen=True)
class Problem:
    file: Path
    line: int
    message: str


class RegistryError(Exception):
    """nuget.org could not be queried (network / HTTP error other than 404)."""


class NuGetRegistry:
    """Listed versions per package id from the nuget.org flat container (cached; 404 = unknown package)."""

    def __init__(self, fetch: Optional[Callable[[str], Optional[bytes]]] = None) -> None:
        self._fetch = fetch or _http_get
        self._cache: dict[str, Optional[set[str]] | RegistryError] = {}

    def versions(self, package: str) -> Optional[set[str]]:
        key = package.lower()
        if key not in self._cache:
            try:
                body = self._fetch(NUGET_INDEX_URL.format(id=key))
                self._cache[key] = None if body is None else {
                    normalize_nuget_version(v) for v in json.loads(body.decode("utf-8")).get("versions", [])
                }
            except RegistryError as error:  # remembered: one retry cycle per package, not per pin
                self._cache[key] = error
        cached = self._cache[key]
        if isinstance(cached, RegistryError):
            raise cached
        return cached


def _http_get(url: str) -> Optional[bytes]:
    """GET with retries; None on 404; RegistryError when nuget.org stays unreachable."""
    last: Exception | None = None
    for attempt in range(3):
        try:
            with urllib.request.urlopen(url, timeout=30) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return None
            last = error
        except (urllib.error.URLError, TimeoutError, OSError) as error:
            last = error
        if attempt < 2:
            time.sleep(2 ** attempt)
    raise RegistryError(f"{url}: {last}")


# --- versions ---------------------------------------------------------------------------------


def is_prerelease(version: str) -> bool:
    """SemVer/NuGet pre-release: any `-` suffix on the version (build metadata `+x` alone is not)."""
    return "-" in version.split("+", 1)[0]


def normalize_nuget_version(version: str) -> str:
    """NuGet's normalized form: `1.0` -> `1.0.0`, `1.0.0.0` -> `1.0.0`, leading zeros dropped, metadata dropped."""
    core, _, prerelease = version.split("+", 1)[0].strip().partition("-")
    parts = [str(int(p)) if p.isdigit() else p for p in core.split(".")]
    while len(parts) < 3:
        parts.append("0")
    if len(parts) == 4 and parts[3] == "0":
        parts.pop()
    normalized = ".".join(parts)
    return (normalized + "-" + prerelease if prerelease else normalized).lower()


# --- pin sources ------------------------------------------------------------------------


def line_of(text: str, *needles: str) -> int:
    """1-based line of the first line containing every needle (0 when none does)."""
    for number, line in enumerate(text.splitlines(), 1):
        if all(n in line for n in needles):
            return number
    return 0


def rel(path: Path, root: Path) -> str:
    try:
        return path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def scan_regex_pins(path: Path, pattern: re.Pattern[str]) -> list[Pin]:
    """NuGet pins written as code (`pattern` captures the package id and the version)."""
    text = path.read_text(encoding="utf-8-sig")
    return [Pin("nuget", m.group(1), m.group(2), path, text.count("\n", 0, m.start()) + 1)
            for m in pattern.finditer(text)]


def scan_package_json(path: Path) -> tuple[list[Pin], list[Problem]]:
    """`dependencies` / `optionalDependencies` (what installs with the package; dev dependencies do not)."""
    text = path.read_text(encoding="utf-8-sig")
    data = json.loads(text)
    pins: list[Pin] = []
    problems: list[Problem] = []
    for section in ("dependencies", "optionalDependencies"):
        for package, spec in (data.get(section) or {}).items():
            line = line_of(text, f'"{package}"')
            if not isinstance(spec, str) or not NPM_REGISTRY_SPEC_RE.match(spec):
                problems.append(Problem(path, line, f"{package}: '{spec}' is not a registry version"))
            else:
                pins.append(Pin("semver", package, spec, path, line))
    return pins, problems


def check_pins(pins: list[Pin], registry: NuGetRegistry) -> list[Problem]:
    problems: list[Problem] = []
    for pin in pins:
        prerelease = is_prerelease(pin.version) if pin.ecosystem == "nuget" else bool(NPM_PRERELEASE_RE.search(pin.version))
        if prerelease:
            problems.append(Problem(pin.file, pin.line, f"{pin.package} {pin.version} is a pre-release version"))
            continue
        if pin.ecosystem != "nuget":
            continue
        try:
            listed = registry.versions(pin.package)
        except RegistryError as error:
            problems.append(Problem(pin.file, pin.line, f"{pin.package} {pin.version}: cannot verify on nuget.org ({error})"))
            continue
        if listed is None:
            problems.append(Problem(pin.file, pin.line, f"{pin.package} is not a package on nuget.org"))
        elif pin.version.lower() not in listed:
            # NuGetDownloader fetches `<id>/<version>/` exactly as written: `1.3` 404s where `1.3.0` exists.
            problems.append(Problem(pin.file, pin.line, f"{pin.package} {pin.version} is not published on nuget.org "
                                                        f"(write the exact version nuget.org lists)"))
    return problems


def require(path: Path, problems: list[Problem]) -> bool:
    """A shipped file the guard expects; missing = the guard is out of date, which must fail too."""
    if path.is_file():
        return True
    problems.append(Problem(path, 1, f"expected file is missing (update {Path(__file__).name})"))
    return False


# --- this repository --------------------------------------------------------------------------

PACKAGE_DIR = Path("Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp")
NUGET_CONFIG = PACKAGE_DIR / "Editor/DependencyResolver/NuGetConfig.cs"
CODE_EXEC_VENDORED = PACKAGE_DIR / "Editor/Scripts/CodeExec/VENDORED.md"
NUGET_PACKAGE_RE = re.compile(r'new\s+NuGetPackage\(\s*"([^"]+)"\s*,\s*"([^"]+)"')
NUGET_BASE_URL_RE = re.compile(r'NuGetBaseUrl\s*=\s*"([^"]*)"')
VENDORED_RELEASE_RE = re.compile(r"release\s+\*\*([^*\s]+)\*\*")
NUGET_FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer"


def scan_vendored(path: Path, package: str, marker: re.Pattern[str] = VENDORED_RELEASE_RE) -> tuple[list[Pin], list[Problem]]:
    """A vendored source copy records the package release it came from (`marker` captures the version)."""
    text = path.read_text(encoding="utf-8-sig")
    match = marker.search(text)
    if not match:
        return [], [Problem(path, 1, f"vendored {package}: no release version marker (copy it from a published release)")]
    return [Pin("nuget", package, match.group(1), path, text.count("\n", 0, match.start()) + 1)], []


def collect(root: Path) -> tuple[list[Pin], list[Problem]]:
    """Every dependency pin Unity-MCP's core, extension and CLI releases ship."""
    pins: list[Pin] = []
    problems: list[Problem] = []
    config = root / NUGET_CONFIG
    if require(config, problems):
        pins += scan_regex_pins(config, NUGET_PACKAGE_RE)
        text = config.read_text(encoding="utf-8-sig")
        base = NUGET_BASE_URL_RE.search(text)
        if not base or base.group(1).rstrip("/") != NUGET_FLAT_CONTAINER:
            problems.append(Problem(config, line_of(text, "NuGetBaseUrl"),
                                    f"NuGetBaseUrl must be {NUGET_FLAT_CONTAINER} (a release restores from nuget.org only)"))
        if not any(p.ecosystem == "nuget" for p in pins):
            problems.append(Problem(config, 1, f"no NuGetPackage pins found (update {Path(__file__).name})"))
    vendored = root / CODE_EXEC_VENDORED
    if require(vendored, problems):
        vendored_pins, vendored_problems = scan_vendored(vendored, "com.IvanMurzak.CodeExec")
        pins += vendored_pins
        problems += vendored_problems
    manifests = [root / PACKAGE_DIR / "package.json", *sorted(root.glob("extensions/*/Packages/*/package.json"))]
    for manifest in [*manifests, root / "cli" / "package.json"]:
        if not require(manifest, problems):
            continue
        found, found_problems = scan_package_json(manifest)
        pins += found
        problems += found_problems
    return pins, problems


def run(root: Path, registry: NuGetRegistry) -> tuple[list[Pin], list[Problem]]:
    pins, problems = collect(root)
    pins = list(dict.fromkeys(pins))
    problems += check_pins(pins, registry)
    unique = sorted(set(problems), key=lambda p: (rel(p.file, root), p.line, p.message))
    return pins, unique


def main(argv: Optional[list[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("--root", type=Path, default=ROOT, help="repository root (default: this checkout)")
    args = parser.parse_args(argv)
    root = args.root.resolve()
    pins, problems = run(root, NuGetRegistry())
    files = sorted({rel(p.file, root) for p in pins})
    print(f"Release dependency guard: {len(pins)} pins in {len(files)} shipped files")
    for name in files:
        print(f"  {name}")
    if not problems:
        print("OK: every dependency pin is a published release.")
        return 0
    annotate = os.environ.get("GITHUB_ACTIONS") == "true"
    print(f"\nFAIL: {len(problems)} problem(s); this release would ship dependencies users cannot restore:")
    for problem in problems:
        location, line = rel(problem.file, root), problem.line or 1
        print(f"  {location}:{line}: {problem.message}")
        if annotate:
            print(f"::error file={location},line={line}::{problem.message}")
    print("\nSwitch every pin back to a published release (and delete local pre-release feeds) before releasing.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
