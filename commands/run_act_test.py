#!/usr/bin/env python3
"""
Run a GitHub Actions workflow job locally with `act`, using the Unity secrets from commands/.env.

Checks Docker and act first, reads UNITY_LICENSE / UNITY_EMAIL / UNITY_PASSWORD (and any other
KEY=VALUE line) from commands/.env (copy commands/.env.example) and passes each as an act secret.

Usage:
    python commands/run_act_test.py
    python commands/run_act_test.py --job-name test-unity-6000-3-27f1-editmode
    python commands/run_act_test.py --verbose --step-debug
    python commands/run_act_test.py --dry-run
    python commands/run_act_test.py --log-file ./act-logs.txt
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path
from typing import Dict, List, Optional

ROOT = Path(__file__).resolve().parents[1]
ENV_FILE = "./commands/.env"
ARTIFACT_PATH = "./.act-artifacts"
REQUIRED_SECRETS = ("UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD")


def probe(argv: List[str]) -> Optional[subprocess.CompletedProcess]:
    """Run a version/health probe; None when the program is not on PATH."""
    try:
        return subprocess.run(argv, cwd=ROOT, capture_output=True, text=True)
    except OSError:
        return None


def check_tool(program: str, label: str, install_hint: str) -> bool:
    result = probe([program, "--version"])
    if result is None:
        print(f"[ERROR] {label} is not installed or not in PATH")
        print(install_hint)
        return False
    if result.returncode != 0:
        print(f"[ERROR] {label} is not responding properly")
        return False
    print(f"[OK] {label} installed: {result.stdout.strip()}")
    return True


def preflight() -> bool:
    print("=== Pre-flight Checks ===\n")
    if not check_tool("docker", "Docker", "Please install Docker Desktop: https://www.docker.com/products/docker-desktop"):
        return False
    daemon = probe(["docker", "ps"])
    if daemon is None or daemon.returncode != 0:
        print("[ERROR] Docker daemon is not running")
        print("Please start Docker Desktop")
        return False
    print("[OK] Docker daemon is running")
    if not check_tool("act", "Act", "Please install act: https://github.com/nektos/act"):
        return False
    print()
    return True


def parse_env(text: str) -> Dict[str, str]:
    """KEY=VALUE lines (split on the first `=`); blanks and `#` comments skipped, quotes trimmed."""
    secrets: Dict[str, str] = {}
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        secrets[key.strip()] = value.strip().strip('"').strip("'")
    return secrets


def act_args(job: str, workflow: str, secrets: Dict[str, str], verbose: bool, dry_run: bool,
             step_debug: bool) -> List[str]:
    # An argument list, never a shell string: secret values cannot inject commands.
    args = ["workflow_dispatch", "-j", job, "-W", workflow]
    for key, value in secrets.items():
        args += ["-s", f"{key}={value}"]
    if verbose:
        args.append("--verbose")
    if dry_run:
        args.append("--dryrun")
    if step_debug:
        args += ["--env", "ACTIONS_STEP_DEBUG=true"]
    return args + ["--artifact-server-path", ARTIFACT_PATH, "--container-architecture", "linux/amd64"]


def run_act(args: List[str], log_file: Optional[str]) -> int:
    sys.stdout.flush()  # our header before act's own output
    if not log_file:
        return subprocess.run(["act", *args], cwd=ROOT).returncode
    # Tee: stream act's merged output to the console and to the log file.
    with open(ROOT / log_file, "w", encoding="utf-8") as log, \
            subprocess.Popen(["act", *args], cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                             text=True, encoding="utf-8", errors="replace") as proc:
        assert proc.stdout is not None
        for line in proc.stdout:
            log.write(line)
            sys.stdout.write(line)
            sys.stdout.flush()
    return proc.returncode


def print_troubleshooting(exit_code: int, job: str, log_file: Optional[str]) -> None:
    print("\n=== Troubleshooting Tips ===\n")
    if exit_code == 127:
        print("Exit code 127 typically means 'Command not found'")
        print("This often indicates:")
        print("  - Docker is not properly installed or not in PATH")
        print("  - A command within the container doesn't exist")
        print("  - Docker-in-Docker (DinD) issues with nested containers")
    elif exit_code == 1:
        print("Exit code 1 indicates a general failure")
        print("Check the output above for specific error messages")
    print()
    print("Debugging commands:")
    print("  # List all Docker containers (including stopped):")
    print("  docker ps -a\n")
    print("  # View logs from the most recent container:")
    print("  docker logs $(docker ps -aq | head -n 1)\n")
    print("  # Re-run with verbose output:")
    print(f"  python commands/run_act_test.py --job-name {job} --verbose --step-debug\n")
    print("  # Re-run with log file:")
    print(f"  python commands/run_act_test.py --job-name {job} --log-file ./act-debug.log\n")
    if log_file:
        print(f"Log file saved to: {log_file}")


def run(args: argparse.Namespace) -> int:
    if not preflight():
        return 1

    env_file = ROOT / ENV_FILE
    if not env_file.is_file():
        print(f"Error: .env file not found at {ENV_FILE}\n")
        print("Please create a .env file in the commands folder with the following format:")
        print("UNITY_LICENSE=<your-unity-license-content>")
        print("UNITY_EMAIL=<your-unity-email>")
        print("UNITY_PASSWORD=<your-unity-password>\n")
        print("You can copy commands/.env.example to commands/.env and fill in your values")
        return 1

    secrets = parse_env(env_file.read_text(encoding="utf-8-sig"))
    present = {key.upper() for key, value in secrets.items() if value.strip()}  # names are case-insensitive, as in the .ps1
    missing = [name for name in REQUIRED_SECRETS if name not in present]
    if missing:
        print("Error: Missing required secrets in .env file:")
        for name in missing:
            print(f"  - {name}")
        return 1

    print("=== Execution Info ===")
    print(f"Job:      {args.job_name}")
    print(f"Workflow: {args.workflow_file}")
    print(f"Secrets:  {', '.join(secrets)}")
    print(f"Verbose:  {args.verbose}")
    print(f"DryRun:   {args.dry_run}")
    print(f"StepDebug: {args.step_debug}")
    if args.log_file:
        print(f"LogFile:  {args.log_file}")
    print(f"Artifacts: {ARTIFACT_PATH}\n")

    print("=== Running Act ===\n")
    start = time.monotonic()
    exit_code = run_act(act_args(args.job_name, args.workflow_file, secrets, args.verbose, args.dry_run,
                                 args.step_debug), args.log_file)
    minutes, seconds = divmod(int(time.monotonic() - start), 60)

    print("\n=== Execution Complete ===")
    print(f"Duration: {minutes % 60:02d}:{seconds:02d}")
    print(f"Exit Code: {exit_code}")

    if exit_code != 0:
        print_troubleshooting(exit_code, args.job_name, args.log_file)
    else:
        print("\nWorkflow completed successfully!")
        if (ROOT / ARTIFACT_PATH).exists():
            print(f"Artifacts saved to: {ARTIFACT_PATH}")
    print()
    return exit_code


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("--job-name", default="test-unity-2022-3-62f3-standalone", help="the job to run")
    parser.add_argument("--workflow-file", default="./.github/workflows/test_pull_request_manual.yml",
                        help="the workflow file that defines the job")
    parser.add_argument("--verbose", action="store_true", help="verbose act output")
    parser.add_argument("--dry-run", action="store_true", help="pass --dryrun to act")
    parser.add_argument("--log-file", help="also save act's output to this file")
    parser.add_argument("--step-debug", action="store_true", help="set ACTIONS_STEP_DEBUG=true")
    return run(parser.parse_args(argv))


if __name__ == "__main__":
    # act prints emoji; never die on output the console code page cannot encode (Windows, piped output).
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(errors="backslashreplace")
    sys.exit(main())
