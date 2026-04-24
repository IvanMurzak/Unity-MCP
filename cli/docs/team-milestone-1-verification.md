# Team Milestone 1 Verification Report

## Scope

This report captures the closing evidence for the milestone-1 tmux orchestration feature on branch `codex/tmux-team-orchestration`.

Milestone-1 scope from the approved plan:
- local tmux team launcher
- persisted orchestration state
- basic lifecycle commands: `team launch`, `team status`, `team list`, `team stop`

## Commands Verified

### Automated checks

Run from `cli/`:

```bash
npm test
npm run build
node dist/index.js team --help
node dist/index.js team launch --help
node dist/index.js team status --help
node dist/index.js team list --help
node dist/index.js team stop --help
```

### Result

- `npm test` passed
  - 13 test files
  - 199 tests passed
- `npm run build` passed
- CLI help smoke passed for the root `team` command and all milestone-1 subcommands

Note: `npm test` intentionally prints some expected error output because the existing CLI suite contains negative-path tests for invalid projects, missing JSON, and unavailable local servers.

## Manual tmux Lifecycle Verification

Manual verification was performed with a temporary Unity-style fixture project containing:

- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

### Launch

Command:

```bash
node dist/index.js team launch --path <fixture-project> --session-name doc-auto-launch
```

Observed result:
- tmux session created successfully
- four deterministic panes created:
  - `leader`
  - `builder`
  - `verifier`
  - `notes`
- state file persisted under:
  - `.unity-mcp/team-state/doc-auto-launch.json`

### Status

Command:

```bash
node dist/index.js team status --path <fixture-project> doc-auto-launch
```

Observed result:
- session reported `ready`
- persisted state and live tmux panes matched

Representative output:

```text
Unity-MCP Team Status
  Project: /tmp/unity-mcp-team-docs-iQvjn1
  Session: doc-auto-launch
  tmux: doc-auto-launch
  Status: ready
──────────────────────────────────────────────────
  leader: ready — %86 (leader)
  builder: ready — %87 (builder)
  verifier: ready — %88 (verifier)
  notes: ready — %89 (notes)
──────────────────────────────────────────────────
SUCCESS: Persisted state matches the live tmux session.
```

### List

Command:

```bash
node dist/index.js team list --path <fixture-project>
```

Observed result:
- saved session listed successfully
- summary reported role health without manual tmux parsing

Representative output:

```text
Unity-MCP Team Sessions
  Project: /tmp/unity-mcp-team-docs-iQvjn1
──────────────────────────────────────────────────
  doc-auto-launch: ready — leader:ready, builder:ready, verifier:ready, notes:ready
```

### Stop

Command:

```bash
node dist/index.js team stop --path <fixture-project> doc-auto-launch
```

Observed result:
- tmux session stopped successfully
- saved state transitioned to `stopped`

## Degraded-State Verification

Command sequence:

1. launch a team session
2. kill the tmux session manually
3. run `team status` again

Observed result:
- `team status` returned exit code `1`
- saved session reported `degraded`
- actionable warnings explained that the tmux session and panes were missing

This confirms that milestone-1 status reconciliation does not silently trust stale saved state.

## Setup-Time Comparison

The approved plan measures success primarily by reduced team-session setup cost. Two views were recorded:

### Operator-step comparison

| Workflow | Operator commands | Notes |
| --- | ---: | --- |
| Manual tmux setup | 9 | `new-session`, 3x `split-window`, `select-layout`, 4x pane title assignment |
| Automated launcher | 1 | `unity-mcp-cli team launch ...` |

### Wall-clock microbenchmark

Recorded on the same local fixture project:

| Workflow | Time |
| --- | ---: |
| Manual tmux setup | 57.21 ms |
| Automated launcher | 222.34 ms |

Interpretation:
- the CLI materially reduces operator steps and makes the layout repeatable
- raw elapsed time in this microbenchmark is dominated by Node CLI startup overhead
- milestone-1 success is therefore strongest on repeatability and operator-effort reduction rather than raw shell latency

## Acceptance-Criteria Status

| Requirement | Status | Evidence |
| --- | --- | --- |
| `team` command family registered | Pass | `team` appears in CLI help and `cli/tests/cli.test.ts` / `cli/tests/team.test.ts` |
| `team launch` creates tmux layout + state | Pass | manual tmux lifecycle verification and mocked lifecycle tests |
| `team status` reconciles saved + live state | Pass | degraded-state verification and mocked reconciliation tests |
| `team list` enumerates known sessions | Pass | manual list output and integration-style list tests |
| Missing-tmux and stale-state messages are actionable | Pass | adapter tests plus degraded/manual checks |
| Docs describe milestone-1 local-only scope | Pass | `cli/README.md` team section |
| Setup-time reduction evidence captured | Pass, with caveat | operator-step reduction is clear; raw elapsed time remains Node-startup-bound |

## Known Gaps / Follow-ups

These are not blockers for milestone-1 completion, but they remain future work:

- only the built-in default four-pane layout is supported
- state remains project-local under `.unity-mcp/team-state`
- no remote/cloud orchestration or OMX-parity workflows are included
- deeper template packs, handoff metadata, and richer verification loops remain milestone-2+ work

## Related Docs

- Plan: `.omx/plans/prd-tmux-orchestration-fork-plan.md`
- Test spec: `.omx/plans/test-spec-tmux-orchestration-fork-plan.md`
- Testing guide: `cli/docs/team-testing-guide.md`
