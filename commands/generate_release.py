#!/usr/bin/env python3
"""
Generate release notes for the commits between two refs.

Writes `release_<from>_to_<to>.md` in the repo root: a compare link plus one line per commit
(newest first) with its short SHA, subject and the GitHub login of its author (`gh api`).

Usage:
    python commands/generate_release.py --version-from 0.17.0 --version-to 0.18.0
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import List, Optional

ROOT = Path(__file__).resolve().parents[1]


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout.strip()


def repo_url() -> str:
    url = re.sub(r"\.git$", "", git("remote", "get-url", "origin"))
    match = re.match(r"^git@github\.com:(.+)", url)
    return f"https://github.com/{match.group(1)}" if match else url


def author_login(repo_path: str, sha: str) -> Optional[str]:
    """The commit's GitHub login (or author name), or None when the API gives nothing usable."""
    result = subprocess.run(["gh", "api", f"repos/{repo_path}/commits/{sha}",
                             "--jq", ".author.login // .commit.author.name"],
                            cwd=ROOT, capture_output=True, text=True, encoding="utf-8")
    data = result.stdout.strip()
    if data.startswith("{"):
        print(f"GitHub API error for commit {sha}: {data}")
        return None
    return data or None


def run(version_from: str, version_to: str) -> int:
    url = repo_url()
    repo_path = url.replace("https://github.com/", "")
    filename = ROOT / f"release_{version_from}_to_{version_to}.md"

    lines: List[str] = [
        "## Comparison",
        f"See every change: [Compare {version_from}...{version_to}]({url}/compare/{version_from}...{version_to})",
        "",
        "---",
        "",
        "## Commit Summary (Newest → Oldest)",
    ]
    log = git("log", "--pretty=format:%H %s", f"{version_from}..{version_to}")
    for sha, _, message in (line.partition(" ") for line in log.splitlines()):
        username = author_login(repo_path, sha)
        if not username:
            # Kept from the .ps1: a commit whose author the API cannot name is left out.
            print(f"Using git fallback for commit {sha}")
            continue
        lines.append(f"- [`{sha[:6]}`]({url}/commit/{sha}) — {message} by @{username}")

    with filename.open("w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"Release notes generated successfully in {filename.name}")
    return 0


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--version-from", required=True, help="the older ref (tag or SHA)")
    parser.add_argument("--version-to", required=True, help="the newer ref (tag or SHA)")
    args = parser.parse_args(argv)
    return run(args.version_from, args.version_to)


if __name__ == "__main__":
    # Never die on a path or tool output the console code page cannot encode (Windows, piped output).
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(errors="backslashreplace")
    sys.exit(main())
