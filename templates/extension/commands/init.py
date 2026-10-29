#!/usr/bin/env python3
"""
Initialize this Unity package project by replacing the template placeholders.

Replaces the placeholders in file content, file names and directory names:
    YOUR_PACKAGE_ID, YOUR_PACKAGE_ID_LOWERCASE, YOUR_PACKAGE_NAME, YOUR_PACKAGE_NAME_INSTALLER,
    YOUR_PACKAGE_NAME_INSTALLER_FILE, YOUR_GITHUB_USERNAME_REPOSITORY,
    YOUR_FEATURE_NAME            (C# identifier: RQL module namespace and class names),
    YOUR_FEATURE_NAME_LOWERCASE  (the RQL source name agents type, e.g. "coolpackage | call ...").

--installer-extra-path inserts a path segment between Assets/ and the Installer folder ("Editor" ->
Assets/Editor/<Package> Installer); you are prompted for it when it is not passed (Enter skips).
--feature-name defaults to the package name without spaces and symbols ("Cool Package" ->
"CoolPackage"); it must not equal a Unity type name the module uses (e.g. "Terrain", "Light").

Usage:
    python commands/init.py --package-id com.mycompany.coolpackage --package-name "Cool Package" \\
        --github-repository myusername/cool-package
    python commands/init.py --package-id com.mycompany.coolpackage --package-name "Cool Package" \\
        --github-repository myusername/cool-package --installer-extra-path Editor
"""
from __future__ import annotations

import argparse
import os
import re
import shutil
import sys
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

ROOT = Path(__file__).resolve().parents[1]
BUILD_DIRS = ("Library", "Temp", "Logs", "obj")
# (target path, ignore patterns relative to it). `*` matches one path segment.
TARGET_PATHS: Tuple[Tuple[str, Sequence[str]], ...] = (
    ("commands/bump_version.py", ()),
    ("commands/get_version.py", ()),
    ("commands/update_ai_game_developer.py", ()),
    ("Installer", BUILD_DIRS),
    ("Unity-Package", BUILD_DIRS),
    ("Unity-Tests", tuple(f"*/{d}" for d in BUILD_DIRS)),
    ("README.md", ()),
    (".github", ()),
)


def feature_name_from(package_name: str) -> str:
    return "".join(part[0].upper() + part[1:] for part in re.split(r"[^A-Za-z0-9]+", package_name) if part)


def is_ignored(rel: str, patterns: Sequence[str]) -> bool:
    return any(re.match("^" + re.escape(p).replace(r"\*", "[^/]+") + "(/.*)?$", rel, re.IGNORECASE)
               for p in patterns)


def collect(root: Path, include_dirs: bool) -> List[Path]:
    """Every file (and, with include_dirs, directory) under the target paths, minus the ignored ones."""
    items: List[Path] = []
    for target, ignore in TARGET_PATHS:
        path = root / target
        if not path.exists():
            if not include_dirs:
                print(f"WARNING: Path not found: {path}")
            continue
        if not path.is_dir():
            items.append(path)
            continue
        for dirpath, dirnames, filenames in os.walk(path):
            base = Path(dirpath)
            # Prune ignored folders (a test project's Library) instead of walking and discarding them.
            dirnames[:] = [d for d in dirnames if not is_ignored((base / d).relative_to(path).as_posix(), ignore)]
            names = (dirnames + filenames) if include_dirs else filenames
            items.extend(base / n for n in names if not is_ignored((base / n).relative_to(path).as_posix(), ignore))
    return items


def replace_all(text: str, replacements: Dict[str, str]) -> str:
    # Longest key first, so YOUR_PACKAGE_ID never eats the start of YOUR_PACKAGE_ID_LOWERCASE.
    for key in sorted(replacements, key=len, reverse=True):
        text = re.sub(re.escape(key), lambda _m, v=replacements[key]: v, text, flags=re.IGNORECASE)
    return text


def replace_content(path: Path, replacements: Dict[str, str]) -> bool:
    raw = path.read_bytes()
    # surrogateescape round-trips non-UTF-8 (binary) bytes unchanged; a BOM is dropped.
    text = raw.decode("utf-8", errors="surrogateescape")
    if text.startswith("﻿"):
        text = text[1:]
    new_text = replace_all(text, replacements)
    if new_text == text:
        return False
    path.write_bytes(new_text.encode("utf-8", errors="surrogateescape"))
    return True


def run(root: Path, package_id: str, package_name: str, github_repository: str, installer_extra_path: str,
        feature_name: Optional[str]) -> int:
    extra = installer_extra_path.strip().replace("\\", "/").strip("/")
    feature = feature_name if feature_name and feature_name.strip() else feature_name_from(package_name)
    if not re.match(r"^[A-Za-z][A-Za-z0-9]*$", feature):
        print(f"FeatureName '{feature}' is not a valid identifier (letters and digits, starting with a letter). "
              "Pass --feature-name.", file=sys.stderr)
        return 1

    installer_base = f"{package_name} Installer"
    installer_file = installer_base.replace(" ", "-")
    installer = f"{extra}/{installer_base}" if extra else installer_base
    # File content gets the installer name with the extra path; file and folder names get the base.
    content_map = {
        "YOUR_PACKAGE_ID": package_id,
        "YOUR_PACKAGE_ID_LOWERCASE": package_id.lower(),
        "YOUR_PACKAGE_NAME": package_name,
        "YOUR_PACKAGE_NAME_INSTALLER_FILE": installer_file,
        "YOUR_PACKAGE_NAME_INSTALLER": installer,
        "YOUR_GITHUB_USERNAME_REPOSITORY": github_repository,
        "YOUR_FEATURE_NAME": feature,
        "YOUR_FEATURE_NAME_LOWERCASE": feature.lower(),
    }
    rename_map = {**content_map, "YOUR_PACKAGE_NAME_INSTALLER": installer_base}

    print("Initializing package with:")
    print(f"  ID: {package_id}")
    print(f"  Name: {package_name}")
    print(f"  Installer: {installer}")
    print(f"  Installer File: {installer_file}")
    print(f"  GitHub Repository: {github_repository}")
    print(f"  RQL Feature Name: {feature} (source: {feature.lower()})")
    if extra:
        print(f"  Installer Extra Path: {extra}")
    print()

    # 1. Replace content in files.
    print("Replacing content in files...")
    for path in collect(root, include_dirs=False):
        if replace_content(path, content_map):
            print(f"  Updated: {path}")

    # 1.5. Move the installer folder under the extra path.
    if extra:
        print("Creating extra path and moving installer folder...")
        assets = root / "Installer" / "Assets"
        extra_dir = assets / extra
        if not extra_dir.exists():
            extra_dir.mkdir(parents=True)
            print(f"  Created: {extra_dir}")
        source = assets / "YOUR_PACKAGE_NAME_INSTALLER"
        if source.exists():
            shutil.move(str(source), str(extra_dir / "YOUR_PACKAGE_NAME_INSTALLER"))
            print(f"  Moved installer to: {extra}/")

    # 2. Rename files and directories, deepest path first so a parent is renamed after its children.
    print("Renaming files and directories...")
    for path in sorted(set(collect(root, include_dirs=True)), key=str, reverse=True):
        new_name = replace_all(path.name, rename_map)
        if new_name != path.name:
            path.rename(path.with_name(new_name))
            print(f"  Renamed: {path.name} -> {new_name}")

    print("Done!")
    return 0


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--package-id", required=True, help='the package ID, e.g. "com.company.package"')
    parser.add_argument("--package-name", required=True, help='the package name, e.g. "My Package"')
    parser.add_argument("--github-repository", required=True, help='"Username/Repository-Name"')
    parser.add_argument("--installer-extra-path", help='extra path before the Installer folder, e.g. "Editor"')
    parser.add_argument("--feature-name", help="C# identifier for the RQL module (default: from the package name)")
    parser.add_argument("--root", type=Path, default=ROOT, help="repo root to initialize (default: this checkout)")
    args = parser.parse_args(argv)
    extra = args.installer_extra_path
    if extra is None:
        print("Optional: Enter an extra path for the Installer (e.g., 'Editor').")
        print("Press Enter to skip.")
        extra = input("Installer Extra Path: ")
    return run(args.root, args.package_id, args.package_name, args.github_repository, extra, args.feature_name)


if __name__ == "__main__":
    sys.exit(main())
