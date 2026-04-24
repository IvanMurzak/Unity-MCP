# Team Command Testing Guide

This guide explains how to test the milestone-1 tmux orchestration feature locally.

## Scope

Milestone-1 covers:
- `team launch`
- `team status`
- `team list`
- `team stop`

It is intentionally limited to local tmux orchestration. It does **not** cover remote/cloud coordination or full OMX parity.

## Prerequisites

Before testing, make sure you have:

1. `tmux` installed and available on `PATH`
2. the CLI dependencies installed under `cli/`
3. a Unity-style project directory containing:
   - `Assets/`
   - `Packages/manifest.json`
   - `ProjectSettings/ProjectVersion.txt`

Example temporary fixture:

```bash
PROJECT=$(mktemp -d)
mkdir -p "$PROJECT/Assets" "$PROJECT/Packages" "$PROJECT/ProjectSettings"
printf '{"dependencies":{}}\n' > "$PROJECT/Packages/manifest.json"
printf 'm_EditorVersion: 6000.0.0f1\n' > "$PROJECT/ProjectSettings/ProjectVersion.txt"
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

## Manual Lifecycle Test

### 1) Launch a session

```bash
node dist/index.js team launch --path "$PROJECT" --session-name my-team-test
```

Expected:
- success message
- four panes created: `leader`, `builder`, `verifier`, `notes`
- state file created at:
  - `"$PROJECT/.unity-mcp/team-state/my-team-test.json"`

Optional live check:

```bash
tmux list-panes -t my-team-test -F '#{pane_id} #{pane_title} #{pane_current_path}'
```

### 2) Inspect status

```bash
node dist/index.js team status --path "$PROJECT" my-team-test
```

Expected:
- overall status is `ready`
- each pane shows `ready`
- output says persisted state matches live tmux

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
- the tmux session is no longer running

Optional file check:

```bash
cat "$PROJECT/.unity-mcp/team-state/my-team-test.json"
```

## Degraded-State Test

This validates that `team status` does not trust stale saved state.

### 1) Launch a session

```bash
node dist/index.js team launch --path "$PROJECT" --session-name my-team-degraded
```

### 2) Kill the tmux session manually

```bash
tmux kill-session -t my-team-degraded
```

### 3) Re-run status

```bash
node dist/index.js team status --path "$PROJECT" my-team-degraded
echo $?
```

Expected:
- status output becomes `degraded`
- warnings explain that the tmux session or panes are missing
- exit code is `1`

## Missing-tmux Test

If you want to validate the missing-tmux path, temporarily run the CLI in an environment where `tmux` is not available on `PATH`.

Expected:
- launch/status/list commands fail clearly
- output tells the operator to install tmux

## Suggested Regression Checklist

Use this short checklist before opening or refreshing a PR:

- [ ] `npm test`
- [ ] `npm run build`
- [ ] `team --help`
- [ ] manual `team launch`
- [ ] manual `team status`
- [ ] manual `team list`
- [ ] manual `team stop`
- [ ] degraded-state check after killing tmux session
- [ ] README examples still match the real commands

## Troubleshooting

### “Not a valid Unity project”

Make sure the target directory contains:
- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

### “tmux is required”

Install tmux and ensure it is visible on `PATH`.

### Session name conflict

If a session already exists, either:
- stop it first with `team stop`
- or launch with a different `--session-name`

### Saved state exists but tmux session is gone

This is expected after abnormal shutdowns. `team status` should report `degraded`, not `ready`.

## Related Docs

- Main CLI docs: `cli/README.md`
- Verification report: `cli/docs/team-milestone-1-verification.md`
