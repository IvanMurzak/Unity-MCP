#!/usr/bin/env python3
"""
Update the com.ivanmurzak.unity.mcp dependency to the latest Unity-MCP release.

Reads the latest GitHub release of IvanMurzak/Unity-MCP (falling back to the newest tag) and pins
it in package.json and in each test project's packages-lock.json (the dependency pin, every
transitive requirement and the lock's own core entry). `--what-if` previews without writing.

Usage:
    python commands/update_ai_game_developer.py
    python commands/update_ai_game_developer.py --what-if
Exit 0 = updated, previewed or already current; exit 1 = an error.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.request
from pathlib import Path
from typing import List, Optional

ROOT = Path(__file__).resolve().parents[1]
PACKAGE_NAME = "com.ivanmurzak.unity.mcp"
GITHUB_REPO = "IvanMurzak/Unity-MCP"
# Keep each packages-lock.json in lockstep with the UPM pin.
TARGET_FILES = (
    "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json",
    "Unity-Tests/2022.3.62f3/Packages/packages-lock.json",
    "Unity-Tests/2023.2.22f1/Packages/packages-lock.json",
    "Unity-Tests/6000.3.1f1/Packages/packages-lock.json",
)
NAME = re.escape(f'"{PACKAGE_NAME}"')
# (1) The dependency-pin form "com.ivanmurzak.unity.mcp": "X": the package.json pin AND every
#     packages-lock.json transitive requirement.
PIN_RE = re.compile("(" + NAME + r':\s*")[^"]+"', re.IGNORECASE)
# (2) The packages-lock.json core ENTRY's own "version" field, which (1) never matches. Scoped to
#     the core entry alone, never any other package's version.
LOCK_CORE_RE = re.compile("(" + NAME + r'\s*:\s*\{\s*"version"\s*:\s*")[^"]+(")', re.IGNORECASE)
CURRENT_RE = re.compile(NAME + r':\s*"([^"]+)"', re.IGNORECASE)


def read_text(path: Path) -> str:
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        return f.read()


def write_text(path: Path, content: str) -> None:
    with path.open("w", encoding="utf-8", newline="") as f:
        f.write(content)


def get_json(url: str):
    request = urllib.request.Request(url, headers={"User-Agent": "update-ai-game-developer"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def latest_version(repo: str) -> str:
    try:
        try:
            tag = get_json(f"https://api.github.com/repos/{repo}/releases/latest")["tag_name"]
            print(f"   Found latest release: {tag}")
        except Exception:
            print("   No releases found, checking tags...")
            tags = get_json(f"https://api.github.com/repos/{repo}/tags")
            if not tags:
                raise RuntimeError("No tags found in repository")
            tag = tags[0]["name"]
            print(f"   Found latest tag: {tag}")
    except Exception as exc:
        raise RuntimeError(f"Failed to fetch version from GitHub: {exc}") from exc
    return re.sub(r"^v", "", tag)  # v1.0.0 -> 1.0.0


def current_version(path: Path) -> Optional[str]:
    if not path.is_file():
        return None
    match = CURRENT_RE.search(read_text(path))
    return match.group(1) if match else None


def update_file(path: Path, rel: str, new_version: str, what_if: bool) -> bool:
    if not path.is_file():
        print(f"   File not found: {rel}")
        return False
    content = read_text(path)
    new_content = PIN_RE.sub(lambda m: m.group(1) + new_version + '"', content)
    new_content = LOCK_CORE_RE.sub(lambda m: m.group(1) + new_version + m.group(2), new_content)
    if new_content == content:
        print(f"   No changes needed in: {rel}")
        return False
    if not what_if:
        write_text(path, new_content)
    return True


def run(root: Path, what_if: bool) -> int:
    print("Update AI Game Developer Package")
    print("================================")

    current = current_version(root / TARGET_FILES[0])
    print(f"Current version: {current or 'not found'}")

    print("\nFetching latest version from GitHub...")
    latest = latest_version(GITHUB_REPO)
    print(f"Latest version: {latest}")

    if current == latest:
        print("\nAlready up to date!")
        return 0

    print("\nUpdating files...")
    updated = 0
    for rel in TARGET_FILES:
        print(f"   Processing: {rel}")
        if update_file(root / rel, rel, latest, what_if):
            updated += 1
            print(f"   Updated: {rel}")

    if what_if:
        print("\nPreview Summary:")
        print(f"   Files to update: {updated}")
        print(f"   Version change: {current} -> {latest}")
        print("\nPreview completed. Run without --what-if to apply changes.")
    elif updated:
        print("\nUpdate completed successfully!")
        print(f"   Updated {updated} file(s)")
        print(f"   Version: {current} -> {latest}")
        print("\nRemember to commit these changes to git")
    else:
        print("\n[warn] No files were updated")
    return 0


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--what-if", "--dry-run", dest="what_if", action="store_true",
                        help="preview the changes without writing any file")
    parser.add_argument("--root", type=Path, default=ROOT, help="repo root to edit (default: this checkout)")
    args = parser.parse_args(argv)
    try:
        return run(args.root, args.what_if)
    except Exception as exc:  # report and exit 1, like the former PowerShell script
        print(f"\n[fail] Script failed: {exc}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
