#!/usr/bin/env python3
"""
Run the Unity tests locally with your own Unity installation (no Docker, no act).

Runs the Unity test runner in batch mode for each selected platform, writes the NUnit results and
editor logs under the output dir and prints a summary. Relative paths resolve from the repo root.

Usage:
    python commands/run_unity_tests.py --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe"
    python commands/run_unity_tests.py --unity-path <Unity> --test-mode editmode
    python commands/run_unity_tests.py --unity-path <Unity> --test-mode all --verbose
Exit 0 = every selected platform passed; exit 1 = a failure, or a bad path.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional

ROOT = Path(__file__).resolve().parents[1]
TEST_MODES: Dict[str, List[str]] = {
    "editmode": ["EditMode"],
    "playmode": ["PlayMode"],
    "standalone": ["StandaloneWindows64"],
    "all": ["EditMode", "PlayMode", "StandaloneWindows64"],
}


@dataclass
class RunResult:
    platform: str
    exit_code: int
    seconds: float
    results_file: Path
    log_file: Path


@dataclass
class Parsed:
    success: bool = False
    total: int = 0
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    error: Optional[str] = None


def section(title: str) -> None:
    print(f"\n=== {title} ===\n")


def mmss(seconds: float) -> str:
    minutes, secs = divmod(int(seconds), 60)
    return f"{minutes % 60:02d}:{secs:02d}"


def run_unity(unity: Path, project: Path, platform: str, results_file: Path, log_file: Path,
              verbose: bool) -> RunResult:
    section(f"Running {platform} Tests")
    print(f"[INFO] Test Platform: {platform}")
    print(f"[INFO] Results: {results_file}")
    print(f"[INFO] Log: {log_file}\n")
    argv = [str(unity), "-runTests", "-batchmode", "-projectPath", str(project), "-testResults", str(results_file),
            "-testPlatform", platform, "-logFile", str(log_file), "-CI", "true", "-GITHUB_ACTIONS", "true"]

    print("Unity is running tests...")
    if not verbose:
        print("(Use --verbose to see real-time output)")
    sys.stdout.flush()
    start = time.monotonic()
    # stderr always reaches the console; stdout only with --verbose.
    exit_code = subprocess.run(argv, stdout=None if verbose else subprocess.DEVNULL).returncode
    seconds = time.monotonic() - start

    print(f"\n[INFO] Duration: {mmss(seconds)}")
    print(f"[INFO] Exit Code: {exit_code}")
    return RunResult(platform, exit_code, seconds, results_file, log_file)


def _int(element: ET.Element, name: str) -> int:
    value = element.get(name)
    return int(value) if value else 0


def parse_results(path: Path) -> Parsed:
    if not path.is_file():
        return Parsed(error="Results file not found")
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as exc:
        return Parsed(error=str(exc))
    if root.tag == "test-run":  # NUnit 3
        return Parsed(success=root.get("failed") == "0" and root.get("result") == "Passed",
                      total=_int(root, "total"), passed=_int(root, "passed"), failed=_int(root, "failed"),
                      skipped=_int(root, "skipped"))
    if root.tag == "test-results":  # NUnit 2
        total, failures, errors, not_run = (_int(root, n) for n in ("total", "failures", "errors", "not-run"))
        return Parsed(success=failures == 0 and errors == 0, total=total, passed=total - failures - errors - not_run,
                      failed=failures + errors, skipped=not_run)
    return Parsed(error="Unknown XML format")


def run(args: argparse.Namespace) -> int:
    section("Unity Local Test Runner")
    print(f"[INFO] Repository Root: {ROOT}")

    project = ROOT / args.project_path
    if not project.exists():
        print(f"[ERROR] Project path not found: {args.project_path}")
        return 1
    project = project.resolve()
    print(f"[INFO] Project: {project}")

    unity = ROOT / args.unity_path
    if not unity.exists():
        print(f"[ERROR] Unity executable not found at: {args.unity_path}\n")
        print("Please provide a valid path to the Unity executable:")
        print('  Example: --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe"')
        return 1
    print(f"[OK] Unity Executable: {args.unity_path}")

    output_dir = (ROOT / args.output_dir).resolve()
    logs_dir = output_dir / "Logs"
    logs_dir.mkdir(parents=True, exist_ok=True)
    print(f"[OK] Output Directory: {output_dir}")

    modes = TEST_MODES[args.test_mode]
    print(f"[INFO] Test Modes: {', '.join(modes)}")

    results: List[RunResult] = []
    for mode in modes:
        results_file = output_dir / f"TestResults-{mode}.xml"
        log_file = logs_dir / f"Unity-{mode}.log"
        for old in (results_file, log_file):
            if old.exists():
                old.unlink()
        results.append(run_unity(unity, project, mode, results_file, log_file, args.verbose))

    section("Test Results Summary")
    success = True
    total = passed = failed = skipped = 0
    for result in results:
        print(f"\n--- {result.platform} ---")
        if result.exit_code != 0:
            print(f"Status: FAILED (Exit Code: {result.exit_code})")
            print(f"Log file: {result.log_file}")
            success = False
        else:
            parsed = parse_results(result.results_file)
            if parsed.error:
                print("Status: ERROR")
                print(f"Error: {parsed.error}")
                success = False
            else:
                if parsed.success:
                    print("Status: PASSED")
                    print(f"Tests: {parsed.passed}/{parsed.total} passed")
                    if parsed.skipped > 0:
                        print(f"Skipped: {parsed.skipped}")
                else:
                    print("Status: FAILED")
                    print(f"Tests: {parsed.passed}/{parsed.total} passed, {parsed.failed} failed")
                    success = False
                total += parsed.total
                passed += parsed.passed
                failed += parsed.failed  # 0 when it passed
                skipped += parsed.skipped
        print(f"Duration: {mmss(result.seconds)}")
        print(f"Results: {result.results_file}")

    section("Overall Summary")
    print(f"Total Tests: {total}")
    print(f"Passed: {passed}")
    if failed > 0:
        print(f"Failed: {failed}")
    if skipped > 0:
        print(f"Skipped: {skipped}")
    print(f"\nResults Location: {output_dir}")

    if success:
        print("\nAll tests PASSED!\n")
        return 0
    print("\nSome tests FAILED!\n")
    print("Troubleshooting:")
    print(f"  - Check log files in: {logs_dir}")
    print(f"  - Check test results in: {output_dir}")
    print("  - Run with --verbose for detailed output\n")
    return 1


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--test-mode", choices=sorted(TEST_MODES), default="all", help="which tests to run")
    parser.add_argument("--unity-path", required=True, help="the Unity editor executable")
    parser.add_argument("--project-path", default="./Unity-MCP-Plugin", help="the Unity project")
    parser.add_argument("--output-dir", default="./commands/TestResults", help="where results and logs go")
    parser.add_argument("--verbose", action="store_true", help="show Unity's output while it runs")
    return run(parser.parse_args(argv))


if __name__ == "__main__":
    # Never die on a path or tool output the console code page cannot encode (Windows, piped output).
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(errors="backslashreplace")
    sys.exit(main())
