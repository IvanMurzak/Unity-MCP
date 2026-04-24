# Team Runtime Architecture and Cross-platform Direction

This document captures the post-milestone-1 runtime direction for `unity-mcp-cli team`.

## Why this exists

Milestone 1 proved that the CLI/state/lifecycle surface is useful, but it also proved that a tmux-only implementation is too narrow for the long-term goal of local multi-agent orchestration across macOS, Windows, and Linux.

The current code now treats tmux as a **backend**, not as the governing architecture.

## Shared semantics that remain stable

These semantics stay common across operating systems and future backends:

- command surface:
  - `team launch`
  - `team status`
  - `team list`
  - `team stop`
- project-local state under `.unity-mcp/team-state/`
- lifecycle states:
  - `launching`
  - `ready`
  - `degraded`
  - `stopped`
- role model:
  - `leader`
  - `builder`
  - `verifier`
  - `notes`
- reconciliation model:
  - persisted state + live runtime inspection

## What changed in the implementation

The orchestration layer now depends on a backend-neutral runtime contract:

- `cli/src/utils/team-runtime.ts`
- `cli/src/utils/team-runtime-tmux.ts`
- `cli/src/utils/team-orchestration.ts`
- `cli/src/utils/team-state.ts`

Key shifts:

- persisted state now stores `runtime.kind` and `runtime.sessionHandle`
- role state now stores `runtimeHandle` instead of assuming a tmux pane id
- schema-v1 tmux state is migrated on read into the new runtime-neutral shape
- lifecycle logic reconciles normalized runtime role health instead of directly reading tmux panes

## Current shipped backend

Today, the shipped backend is still **tmux**.

That means:

- macOS/Linux can keep the current tmux-oriented workflow
- Windows can still use tmux manually if the user chooses to provide it
- the architecture no longer requires future backends to pretend they are tmux

## Shared vs backend-specific expectations

### Shared contract

Every backend should be able to answer:

- is the session available?
- which roles are healthy?
- which roles are missing or degraded?
- can the session be stopped cleanly?

### Backend-specific differences that are acceptable

These may vary by backend without breaking the operator model:

- whether roles have pane ids
- whether pane titles exist
- whether split layouts are native concepts
- whether session visibility is pane-oriented or process-oriented

Pane semantics are therefore **preferred UX**, not a hard architectural requirement.

## Windows-oriented backend evaluation

The evaluation target is **Windows-native Unity workflow first**. Pane fidelity matters, but it is not allowed to outrank Unity usability.

### Candidate A — Windows Terminal control

Why it is viable:

- `wt` supports opening tabs and split panes from the command line
- it preserves a familiar pane-oriented operator model on Windows

Why it is risky:

- session/process lifecycle ownership is weaker than a process-first controller
- orchestration reliability can become coupled to terminal window behavior instead of the runtime model itself

Official docs:

- Windows Terminal command line arguments: https://learn.microsoft.com/en-us/windows/terminal/command-line-arguments
- Windows Terminal panes overview: https://learn.microsoft.com/en-au/windows/terminal/panes

### Candidate B — Windows-native process/state-first runtime

Why it is viable:

- process lifecycle control maps directly onto orchestration concerns
- PowerShell already exposes process launch/wait primitives that fit a local controller model
- it aligns best with “Unity stays native on Windows” because it does not require tmux semantics first

Tradeoff:

- the default UX is weaker for pane visibility unless a terminal/view layer is added on top

Official docs:

- PowerShell `Start-Process`: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/start-process?view=powershell-7.6
- PowerShell `Wait-Process`: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/wait-process?view=powershell-7.5

### Candidate C — WezTerm as a cross-platform terminal runtime

Why it is interesting:

- WezTerm exposes pane control from its CLI
- it offers a more uniform cross-platform pane model than Windows Terminal + tmux split ownership

Why it is not the current recommendation:

- it adds a new terminal dependency across platforms
- it still centers the solution on terminal semantics rather than on orchestration lifecycle/state

Official docs:

- WezTerm CLI overview: https://wezterm.org/cli/cli/index.html
- WezTerm split pane command: https://wezterm.org/cli/cli/split-pane.html

## Recommended direction

The current recommendation is:

1. keep tmux as the shipping backend for the current milestone
2. preserve the shared runtime contract in code
3. prototype a **Windows-native process/state-first backend** next
4. optionally add a terminal/view layer later for pane-friendly UX

This ordering best matches the clarified success criterion: **Windows-native Unity development flow must win over tmux parity**.

## Evaluation matrix

| Candidate | Windows-native Unity fit | Pane UX | Lifecycle control | Dependency burden | Current recommendation |
| --- | --- | --- | --- | --- | --- |
| tmux everywhere | Low | High on macOS/Linux | Medium | Medium/High on Windows | No |
| Windows Terminal backend | Medium | High | Medium | Low/Medium | Possible follow-up |
| Process/state-first backend | High | Medium/Low by default | High | Low | **Recommended next backend** |
| WezTerm-first backend | Medium | High | Medium | Medium | Not first choice |

## Manual verification guidance for this refactor

When verifying this runtime abstraction work:

1. run the existing tmux lifecycle checks from `cli/docs/team-testing-guide.md`
2. confirm state files contain `runtime.kind` and `runtime.sessionHandle`
3. confirm old schema-v1 tmux state still loads successfully
4. confirm docs do not promise identical pane fidelity across every future backend

## Decision summary

- **Do not** keep tmux as the architecture center
- **Do** keep tmux as one backend
- **Do** prioritize Windows-native Unity workflow for the next backend decision
- **Do** keep pane semantics as best-effort UX rather than a universal invariant
