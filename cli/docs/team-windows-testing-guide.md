# Windows Validation Guide for Unity Project Sessions

This guide is for validating the standalone **Windows-friendly** lifecycle path without migrating your primary development environment.

## Recommendation

Do **not** migrate your whole development workflow to Windows just to validate this feature.

Prefer:
- your current OS as the main implementation environment
- a separate **Windows validation environment** for smoke and regression checks

That gives you:
- stable day-to-day productivity on the current machine
- real Windows-native Unity evidence before shipping Windows-specific behavior
- better cross-platform discipline because implementation and validation stay distinct

## When a Windows-first development setup becomes worth it

Consider making Windows your primary environment only if most future work shifts toward:
- Windows process management
- Unity Editor and Hub lifecycle on Windows
- Windows Terminal / PowerShell integration
- Windows-specific path, quoting, signal, or permission issues

Until then, a dedicated Windows validation lane is the lower-risk option.

## Suggested validation environment

Recommended minimum setup:
- Windows 11 or Windows 10
- Unity Hub installed natively on Windows
- at least one local Unity project
- Git
- Node.js + npm
- PowerShell 7 preferred (Windows PowerShell is acceptable for smoke tests)
- optional: Codex CLI and/or Claude Code CLI if you also want MCP client validation

## Install and build

In the Windows environment:

```powershell
git clone <your-fork-url>
cd Unity-MCP
git switch codex/tmux-team-orchestration
cd cli
npm install
npm test
npm run build
```

Expected:
- tests pass
- build succeeds
- `dist/` is generated

## Minimal lifecycle smoke

Prepare or choose a Unity-style project. Then run:

```powershell
node dist/index.js team launch --path C:\path\to\YourUnityProject --session-name win-team-smoke
node dist/index.js team status --path C:\path\to\YourUnityProject win-team-smoke
node dist/index.js team list --path C:\path\to\YourUnityProject
node dist/index.js team stop --path C:\path\to\YourUnityProject win-team-smoke
```

Expected:
- launch succeeds without requiring tmux
- the reported runtime is `process`
- status becomes `ready`
- list shows the session
- stop marks the session `stopped`

## Saved-state checks

After `team launch`, inspect:

```powershell
Get-Content C:\path\to\YourUnityProject\.unity-mcp\team-state\win-team-smoke.json
```

Expected fields:
- `schemaVersion: 2`
- `runtime.kind: "process"`
- `runtime.sessionHandle: "win-team-smoke"`
- per-role `runtimeHandle` values populated

## Degraded-state smoke

To validate recovery behavior:

1. launch a session
2. terminate one spawned role process from Task Manager or PowerShell
3. rerun `team status`

Helpful PowerShell command:

```powershell
Get-Process node | Sort-Object StartTime -Descending | Select-Object -First 10 Id, ProcessName, StartTime
```

After killing one of the fresh role processes:

```powershell
node dist/index.js team status --path C:\path\to\YourUnityProject win-team-smoke
$LASTEXITCODE
```

Expected:
- session status is `degraded`
- the missing role is called out in the output
- exit code is `1`

## Unity-aware checks

Once lifecycle smoke works, validate the Windows-native Unity path itself:

- Unity project still opens normally from Windows Explorer or Unity Hub
- project-local `.unity-mcp/team-state/` does not interfere with editor startup
- if using `wait-for-ready`, verify it against a real local Unity-MCP server session
- if using `setup-mcp codex` or `setup-mcp claude-code`, verify the generated config from the same Windows machine

## Priority checklist

Use this order for the first Windows pass:

1. `npm test`
2. `npm run build`
3. `team --help`
4. `team launch`
5. `team status`
6. inspect saved state for `runtime.kind = process`
7. `team list`
8. `team stop`
9. degraded-state test by killing one role process
10. optional Unity + MCP end-to-end validation

## Known current limitations

At the current stage:
- the process runtime is lifecycle-first, not pane-first
- Windows pane UX is **not** the shipping promise
- Windows-native Unity validation is still more important than terminal layout fidelity
- Unity-aware readiness/recovery can still be strengthened in follow-up work

## Exit criteria for “Windows-friendly enough”

Treat the feature as Windows-friendly enough for the next step only if all of these are true:
- no tmux dependency is required on Windows
- `team launch/status/list/stop` work on a real Windows machine
- degraded state is surfaced clearly after killing a role process
- saved state remains project-local and understandable
- normal Unity usage on Windows remains unaffected
