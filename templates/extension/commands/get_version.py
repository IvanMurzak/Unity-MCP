#!/usr/bin/env python3
"""Print this package's version (from its package.json). Exit 1 when it cannot be read."""
import re
import sys
from pathlib import Path

PACKAGE_JSON = Path(__file__).resolve().parents[1] / "Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json"

if __name__ == "__main__":
    if PACKAGE_JSON.is_file():
        match = re.search(r'"version":\s*"([\d\.]+)"', PACKAGE_JSON.read_text(encoding="utf-8-sig"), re.IGNORECASE)
        if match:
            print(match.group(1))
            sys.exit(0)
    print(f"Could not find version in {PACKAGE_JSON}", file=sys.stderr)
    sys.exit(1)
