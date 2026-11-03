#!/usr/bin/env python3
"""
Automated version bump for this Unity package.

Updates the version in package.json and the Installer `Version` constant in lock-step; add any new
version-bearing file to VERSION_FILES. `--what-if` previews without writing.

Edits are regex substitutions on the raw text, so a bump changes only the version strings: line
endings are preserved and files are written back as UTF-8 without a BOM.

Usage:
    python commands/bump_version.py --new-version 1.0.1
    python commands/bump_version.py --new-version 1.0.1 --what-if
Exit 0 = bumped (or previewed, or already at that version); exit 1 = invalid version, nothing to
update, or an error.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import List, Optional

ROOT = Path(__file__).resolve().parents[1]
PACKAGE_JSON = "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json"
SEMVER_RE = re.compile(r"^\d+\.\d+\.\d+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$")
CURRENT_VERSION_RE = re.compile(r'"version":\s*"([\d\.]+)"', re.IGNORECASE)


@dataclass(frozen=True)
class VersionFile:
    path: str
    pattern: re.Pattern
    replace: str  # `{VERSION}` is substituted with the new version
    description: str


# package.json is the version single-source (current_version reads it).
VERSION_FILES = (
    VersionFile(PACKAGE_JSON, re.compile(r'"version":\s*"[\d\.]+"', re.IGNORECASE), '"version": "{VERSION}"',
                "Unity package version"),
    VersionFile("Installer/Assets/YOUR_PACKAGE_NAME_INSTALLER/Installer.cs",
                re.compile(r'public const string Version = "[\d\.]+";', re.IGNORECASE),
                'public const string Version = "{VERSION}";', "Installer C# version constant"),
)


@dataclass
class Change:
    path: Path
    matches: int
    content: str


def read_text(path: Path) -> str:
    # newline="" keeps CRLF/LF exactly as on disk; utf-8-sig drops a BOM.
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        return f.read()


def write_text(path: Path, content: str) -> None:
    with path.open("w", encoding="utf-8", newline="") as f:
        f.write(content)


def is_semantic_version(version: str) -> bool:
    return SEMVER_RE.match(version) is not None


def current_version(root: Path) -> str:
    path = root / PACKAGE_JSON
    if not path.is_file():
        raise RuntimeError(f"Could not find package.json at: {PACKAGE_JSON}")
    match = CURRENT_VERSION_RE.search(read_text(path))
    if not match:
        raise RuntimeError("Could not extract current version from package.json")
    return match.group(1)


def plan_changes(root: Path, new_version: str) -> List[Change]:
    changes: List[Change] = []
    for vf in VERSION_FILES:
        path = root / vf.path
        if not path.is_file():
            print(f"[warn] File not found: {vf.path}")
            continue
        content = read_text(path)
        replacement = vf.replace.replace("{VERSION}", new_version)
        new_content, count = vf.pattern.subn(lambda _m: replacement, content)
        if new_content == content:
            print(f"[warn] No matches found in: {vf.path}")
            print(f"   Pattern: {vf.pattern.pattern}")
            continue
        changes.append(Change(path, count, new_content))
        print(f"[edit] {vf.description}: {count} occurrence(s)")
        for match in vf.pattern.finditer(content):
            print(f"   {match.group(0)} -> {replacement}")
    return changes


def run(root: Path, new_version: str, what_if: bool) -> int:
    print("Package Version Bump Script")
    print("===========================")

    if not is_semantic_version(new_version):
        print(f"[fail] Invalid semantic version format: {new_version}")
        print("Expected format: major.minor.patch (e.g., '1.2.3')")
        return 1

    current = current_version(root)
    print(f"Current version: {current}")
    print(f"New version:     {new_version}")

    if current == new_version:
        print("[warn] New version is the same as current version")
        return 0

    print("\nScanning for version references...")
    changes = plan_changes(root, new_version)
    if not changes:
        print("[fail] No version references found to update!")
        return 1

    total = sum(c.matches for c in changes)
    if what_if:
        print("\nPreview Summary:")
        print(f"Files to be modified: {len(changes)}")
        print(f"Total replacements: {total}")
        print("\nPreview completed. Use without --what-if to apply changes.")
        return 0

    for change in changes:
        write_text(change.path, change.content)

    print("\nVersion bump completed successfully!")
    print(f"   Updated {len(changes)} files")
    print(f"   Total replacements: {total}")
    print(f"   Version: {current} -> {new_version}")
    print("\nRemember to commit these changes to git")
    return 0


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--new-version", required=True, help='new semver version, e.g. "1.0.1"')
    parser.add_argument("--what-if", "--dry-run", dest="what_if", action="store_true",
                        help="preview the changes without writing any file")
    parser.add_argument("--root", type=Path, default=ROOT, help="repo root to edit (default: this checkout)")
    args = parser.parse_args(argv)
    try:
        return run(args.root, args.new_version, args.what_if)
    except Exception as exc:  # report and exit 1, like the former PowerShell script
        print(f"\n[fail] Script failed: {exc}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
