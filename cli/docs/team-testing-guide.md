# Team Command Testing Guide

This guide explains how to test the Unity project session lifecycle locally.

## Scope

The current local lifecycle surface covers:
- `team launch`
- `team status`
- `team list`
- `team stop`

It is intentionally limited to **local project session lifecycle management**. It does **not** cover remote/cloud coordination or generic task dispatch.

## Runtime expectations by platform

`unity-mcp-cli team` chooses the local runtime automatically:

- **Windows** → prefers the standalone `process` runtime
- **macOS / Linux** → prefers the `tmux` runtime

The command surface stays the same across runtimes. What changes is the operator experience:

- `tmux` runtime: pane-oriented session management
- `process` runtime: detached local processes with persisted lifecycle state

## Prerequisites

Before testing, make sure you have:

1. CLI dependencies installed under `cli/`
2. a Unity-style project directory containing:
   - `Assets/`
   - `Packages/manifest.json`
   - `ProjectSettings/ProjectVersion.txt`
3. the backend-specific prerequisite:
   - **macOS / Linux (`tmux`)**: `tmux` installed and on `PATH`
   - **Windows (`process`)**: no tmux requirement; regular local process spawning must work

Example temporary fixture:

```bash
PROJECT=$(mktemp -d)
mkdir -p "$PROJECT/Assets" "$PROJECT/Packages" "$PROJECT/ProjectSettings"
printf '{"dependencies":{}}\n' > "$PROJECT/Packages/manifest.json"
printf 'm_EditorVersion: 6000.0.0f1\n' > "$PROJECT/ProjectSettings/ProjectVersion.txt"
```

On Windows PowerShell, create the same fixture like this:

```powershell
$Project = Join-Path $env:TEMP ("unity-mcp-test-" + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Force -Path (Join-Path $Project 'Assets') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $Project 'Packages') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $Project 'ProjectSettings') | Out-Null
Set-Content -Path (Join-Path $Project 'Packages/manifest.json') -Value '{"dependencies":{}}'
Set-Content -Path (Join-Path $Project 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 6000.0.0f1'
```

## Fast Preflight Checks

Run these from `cli/`:

```bash
npm test
npm run build
node dist/index.js team --help
```

What to expect:
- `npm test` passes
- `npm run build` passes
- `team --help` shows `launch`, `status`, `list`, and `stop`

## Cross-platform lifecycle smoke

These commands are the same on every platform.

### 1) Launch a session

```bash
node dist/index.js team launch --path "$PROJECT" --session-name my-team-test
```

Expected:
- success message
- state file created at:
  - `"$PROJECT/.unity-mcp/team-state/my-team-test.json"`
- output shows the chosen runtime kind and the created role handles

### 2) Inspect status

```bash
node dist/index.js team status --path "$PROJECT" my-team-test
```

Expected:
- overall status is `ready`
- each role reports `ready`
- output reconciles persisted state with the live runtime

### 3) List saved sessions

```bash
node dist/index.js team list --path "$PROJECT"
```

Expected:
- the session appears in the list
- the summary includes per-role health

### 4) Stop the session

```bash
node dist/index.js team stop --path "$PROJECT" my-team-test
```

Expected:
- success message
- state transitions to `stopped`
- runtime resources are no longer running

Optional file check:

```bash
cat "$PROJECT/.unity-mcp/team-state/my-team-test.json"
```

## Backend-specific checks

### macOS / Linux: tmux runtime

Optional live check after launch:

```bash
tmux list-panes -t my-team-test -F '#{pane_id} #{pane_title} #{pane_current_path}'
```

Expected:
- four panes created: `leader`, `builder`, `verifier`, `notes`

Degraded-state test:

```bash
node dist/index.js team launch --path "$PROJECT" --session-name my-team-degraded
tmux kill-session -t my-team-degraded
node dist/index.js team status --path "$PROJECT" my-team-degraded
echo $?
```

Expected:
- status becomes `degraded`
- warnings explain that the tmux session or panes are missing
- exit code is `1`

### Windows: process runtime

The process runtime has no pane semantics. Validate lifecycle using the saved state and status output instead.

After `team launch`, inspect the saved state file and confirm:
- `schemaVersion` is `2`
- `runtime.kind` is `process`
- `runtime.sessionHandle` matches the session name
- each role has a `runtimeHandle` PID-like value

PowerShell example:

```powershell
Get-Content (Join-Path $Project '.unity-mcp/team-state/my-team-test.json')
```

A practical degraded-state check on Windows is:
1. `team launch`
2. manually terminate one of the launched role processes
3. run `team status` again

Expected:
- overall status becomes `degraded`
- output identifies the missing runtime role
- exit code is `1`

For a fuller Windows walkthrough, see [`team-windows-testing-guide.md`](team-windows-testing-guide.md).

## Suggested Regression Checklist

Use this short checklist before opening or refreshing a PR:

- [ ] `npm test`
- [ ] `npm run build`
- [ ] `team --help`
- [ ] manual `team launch`
- [ ] manual `team status`
- [ ] manual `team list`
- [ ] manual `team stop`
- [ ] one degraded-state check for the current runtime
- [ ] README examples still match the real commands

## Troubleshooting

### “Not a valid Unity project”

Make sure the target directory contains:
- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

### “tmux is required”

This usually means you are on a tmux-oriented runtime path and `tmux` is not installed or not visible on `PATH`.

### Session name conflict

If a session already exists, either:
- stop it first with `team stop`
- or launch with a different `--session-name`

### Saved state exists but the live runtime is gone

This is expected after abnormal shutdowns. `team status` should report `degraded`, not `ready`.

## Related Docs

- Main CLI docs: `cli/README.md`
- Verification report: `cli/docs/team-milestone-1-verification.md`
- Runtime architecture: `cli/docs/team-runtime-architecture.md`
- Windows validation guide: `cli/docs/team-windows-testing-guide.md`
