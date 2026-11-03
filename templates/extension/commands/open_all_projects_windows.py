#!/usr/bin/env python3
"""
Open every Unity project of this repository in its Unity Editor (Windows).

Reads each project's editor version from ProjectSettings/ProjectVersion.txt and launches that
editor from the Unity Hub editors folder: Installer, Unity-Package and the Unity-Tests projects.
The editors folder is --editors-path, else Unity Hub's secondaryInstallPath.json, else a common
location. On macOS and Linux use open-all-projects-unix.sh.

Usage:
    python commands/open_all_projects_windows.py
    python commands/open_all_projects_windows.py --editors-path "D:/Unity/Hub/Editor"
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path
from typing import List, Optional

ROOT = Path(__file__).resolve().parents[1]
PROJECTS = ("Installer", "Unity-Package", "Unity-Tests/2022.3.62f3", "Unity-Tests/2023.2.22f1",
            "Unity-Tests/6000.3.1f1")
COMMON_PATHS = (r"C:\Program Files\Unity\Hub\Editor", r"D:\Program Files\Unity\Hub\Editor",
                r"D:\Unity\Hub\Editor", r"C:\Unity\Hub\Editor")
# Stagger between launches: Unity's LicensingClient and UPM package-cache extraction both race when
# several Editors start within a few seconds of each other. 2s is too short: the first Editor spawns
# its LicensingClient (e.g. protocol 1.16.2 for Unity 2023.2+) and an older Editor (e.g. 2022.3)
# then fails to attach with "Unsupported protocol version" + exit 199. The UPM cache also corrupts
# under ENOENT when two Editors extract the same `.tgz` into %LOCALAPPDATA%/Unity/cache/packages at
# once. 30 seconds lets the first Editor finish the licensing handshake AND the UPM extraction.
STAGGER_SECONDS = 30


def detect_editors_path() -> Optional[str]:
    appdata = os.environ.get("APPDATA")
    config = Path(appdata or ".") / "UnityHub" / "secondaryInstallPath.json"
    if appdata and config.is_file():
        try:
            path = json.loads(config.read_text(encoding="utf-8-sig"))
        except ValueError:
            path = None
        if isinstance(path, str) and path and Path(path).exists():
            return path
    return next((p for p in COMMON_PATHS if Path(p).exists()), None)


def editor_version(project: Path) -> Optional[str]:
    match = re.search(r"m_EditorVersion:\s*(.+)", (project / "ProjectSettings/ProjectVersion.txt").read_text(
        encoding="utf-8-sig"))
    return match.group(1).strip() if match else None


def run(editors_path: Optional[str]) -> int:
    editors = editors_path or detect_editors_path()
    if not editors or not Path(editors).exists():
        print("Unity editors folder not found.\nPlease specify the correct path using --editors-path.\n"
              "Example: python commands/open_all_projects_windows.py --editors-path 'D:/Unity/Editor'",
              file=sys.stderr)
        return 1

    print("Opening Unity projects...")
    print(f"Editors path: {editors}\n")
    for project in PROJECTS:
        project_path = ROOT / project
        if not project_path.exists():
            print(f"WARNING: Project not found: {project_path}")
            continue
        if not (project_path / "ProjectSettings/ProjectVersion.txt").is_file():
            print(f"WARNING: ProjectVersion.txt not found for: {project}")
            continue
        version = editor_version(project_path)
        if not version:
            print(f"WARNING: Could not parse Unity version for: {project}")
            continue
        unity = Path(editors) / version / "Editor" / "Unity.exe"
        if not unity.exists():
            print(f"WARNING: Unity {version} not installed. Skipping: {project}")
            continue

        print(f"Opening: {project} (Unity {version})")
        subprocess.Popen([str(unity), "-projectPath", str(project_path)])
        time.sleep(STAGGER_SECONDS)

    print("\nDone! All projects are being opened.")
    return 0


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--editors-path", help=r'the Unity Hub editors folder (default: auto-detect)')
    return run(parser.parse_args(argv).editors_path)


if __name__ == "__main__":
    sys.exit(main())
