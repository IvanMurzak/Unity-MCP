# Windows Codex runner companion (v1)

This note is for the **external** Windows companion runtime that sits next to Unity-MCP. It exists so a companion implementer can start work without guessing which parts belong in this repository.

The v1 goal is narrow: consume a passive Windows handoff snapshot, run a bounded validation profile, emit a bounded `windows_lane_evidence_envelope`, submit it with `handoff submit-windows-evidence`, and stop so the mac leader can reconcile later.

## Governing boundary artifacts

The runner depends on these repo-owned contract docs; it does not replace or reopen them:

- [`cli/docs/windows-codex-lane-v1.md`](windows-codex-lane-v1.md)
- [`cli/docs/team-windows-testing-guide.md`](team-windows-testing-guide.md)
- [`cli/examples/windows-codex-lane/sample-windows-handoff-snapshot.json`](../examples/windows-codex-lane/sample-windows-handoff-snapshot.json)
- [`cli/examples/windows-codex-lane/sample-windows-evidence.json`](../examples/windows-codex-lane/sample-windows-evidence.json)
- [`cli/examples/windows-codex-lane/run-windows-validation-runner-v1.ps1`](../examples/windows-codex-lane/run-windows-validation-runner-v1.ps1)

## Repo-owned vs companion-owned boundary

### Unity-MCP owns
- the passive Windows handoff snapshot **contract** and examples in this repo
- the leader-owned handoff ledger
- bounded Windows evidence validation via `handoff submit-windows-evidence`
- queued evidence visibility for operators via `handoff list-windows-evidence`
- leader reconcile via `handoff reconcile-windows-evidence`

### External companion owns
- obtaining a passive snapshot from external coordination
- starting/supervising `psmux` or an equivalent Windows session/process manager
- launching Codex CLI against the Windows workspace
- exposing Unity-MCP to the worker as an MCP-capable tool target
- running the `windows_validation_smoke_v1` validation recipe
- producing a bounded `windows_lane_evidence_envelope`
- calling `unity-mcp-cli handoff submit-windows-evidence ...`
- stopping after submit and waiting for the mac leader to reconcile or request another run

Unity-MCP does **not** own companion process supervision, mailbox polling, assignment submission, assignment spool ownership, dispatch to Windows workers, or post-submit reconcile polling.

## Snapshot intake rules

Treat the passive snapshot as a **reference-only** artifact.

The runner may only use it to:
- validate the snapshot schema and required fields
- echo `handoffId` and `handoffRecordVersion` into local run metadata and the later evidence envelope
- resolve optional local path hints such as workspace/log/artifact directories

The runner must **not** use the snapshot to:
- claim assignment ownership
- infer lifecycle authority
- perform authoritative freshness checks against the handoff ledger
- run reconcile logic
- start any mailbox/polling loop after submit

## Named v1 validation profile

The v1 profile is named `windows_validation_smoke_v1`.

### Ordered steps
1. **CLI test pass**
   - run `npm test` in `cli/`
   - evidence target: `test_report` when a structured report already exists, otherwise `log`
2. **CLI build pass**
   - run `npm run build` in `cli/`
   - evidence target: `log`
3. **Command surface check**
   - run `node dist/index.js team --help`
   - evidence target: `log`
4. **Lifecycle smoke**
   - run `team launch`, `team status`, inspect saved state for `runtime.kind = process`, `team list`, `team stop`
   - evidence target: `log` plus optional `note`
5. **Degraded-state smoke**
   - kill one role process and confirm degraded state surfaces clearly
   - evidence target: `log` plus optional `screenshot`
6. **Optional Unity + MCP end-to-end validation**
   - run only when the local environment already has a matching Unity project/editor setup
   - evidence target: `log`, optional `screenshot`, optional `note`

## Outcome mapping

- `passed`: all required smoke steps completed and bounded evidence was produced
- `failed`: a required step completed with a deterministic failure
- `blocked`: the runner could not proceed because local preconditions were unavailable, such as a missing workspace, missing toolchain, Unity editor lock, or unavailable runtime

## Companion-local runtime shape

A deliberately small v1 companion can look like this:

```text
windows-companion/
  logs/
  outbox/
  sessions/
  snapshots/
```

These are **external companion state only**.

- `snapshots/` may cache passive snapshots locally for replay/debugging
- `logs/` stores the runner transcript and per-step command logs
- `outbox/` stores generated evidence payloads and optional notes/screenshots before submit
- `sessions/` is optional local state for `psmux` or equivalent session bookkeeping

Companion `outbox/` is **not** Unity-MCP's queue. Unity-MCP's only repo-owned evidence queue remains `.unity-mcp/handoff-spool/windows-evidence/`.

## Evidence handoff rules

- Keep `handoffId` and handoff version aligned with the passive snapshot.
- Emit only bounded evidence refs (`log`, `test_report`, `build_artifact`, `screenshot`, `note`).
- Prefer deterministic local file locations so operators can inspect failures.
- Submit only through `unity-mcp-cli handoff submit-windows-evidence ...`.
- Stop after submit and report that leader reconcile is pending.
- Do **not** call `handoff list-windows-evidence` as a runner polling loop.
- Do **not** call `handoff reconcile-windows-evidence` from the Windows runner.

## Recommended bootstrap order

1. Obtain a passive snapshot from external coordination.
2. Read the snapshot as reference-only metadata.
3. Resolve or create local `snapshots/`, `logs/`, `outbox/`, and optional `sessions/` directories.
4. Start or reuse a managed Windows session (for example `psmux`).
5. Launch Codex CLI in the target workspace.
6. Run `windows_validation_smoke_v1`.
7. Emit a bounded `windows_lane_evidence_envelope` JSON payload.
8. Queue that payload with `unity-mcp-cli handoff submit-windows-evidence ...`.
9. Await leader reconcile rather than mutating lifecycle state locally.

## Example runner entrypoint

The repo ships a thin example companion entrypoint:

```powershell
pwsh -File cli/examples/windows-codex-lane/run-windows-validation-runner-v1.ps1 `
  -SnapshotPath cli/examples/windows-codex-lane/sample-windows-handoff-snapshot.json `
  -ProjectPath D:\workSpace\Unity-MCP\Unity-MCP-Plugin `
  -WorkspacePath D:\workSpace\Unity-MCP
```

That script demonstrates the intended v1 shape:
- schema-check the passive snapshot
- echo `handoffId` and `handoffRecordVersion`
- run the bounded smoke profile
- write only bounded evidence refs
- submit through `handoff submit-windows-evidence`
- stop without polling Unity-MCP for reconcile completion

## Deferred follow-ups

These belong to later backlog slices, not this thin validation-first v1:

- Windows validation hardening / version-matched fixture policy
- session recovery and retry hardening beyond the local companion
- multi-run queue orchestration
- broader implementation-lane execution beyond validation-first smoke
- Discord bridge live-ops validation and deployment runbook
- planner/QA executionization on top of the existing bounded role model
