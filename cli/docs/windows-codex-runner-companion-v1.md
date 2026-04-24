# Windows Codex runner companion (v1)

This note is for the **external** Windows companion runtime that sits next to Unity-MCP. It exists so a companion implementer can start work without guessing which parts belong in this repository.

## Repo-owned vs companion-owned boundary

### Unity-MCP owns
- the passive Windows handoff snapshot **contract** and examples in this repo
- the leader-owned handoff ledger
- bounded Windows evidence validation via `handoff submit-windows-evidence`
- queued evidence visibility via `handoff list-windows-evidence`
- leader reconcile via `handoff reconcile-windows-evidence`

### External companion owns
- obtaining a passive snapshot from external coordination
- starting/supervising `psmux` or an equivalent Windows session/process manager
- launching Codex CLI against the Windows workspace
- exposing Unity-MCP to the worker as an MCP-capable tool target
- producing a bounded `windows_lane_evidence_envelope`
- calling `unity-mcp-cli handoff submit-windows-evidence ...`
- waiting for the mac leader to reconcile or request another run

Unity-MCP does **not** own companion process supervision, mailbox polling, assignment submission, assignment spool ownership, or dispatch to Windows workers.

## Recommended bootstrap order

1. Obtain a passive snapshot from external coordination.
2. Read the snapshot as **reference-only** metadata:
   - `handoffId`
   - `handoffRecordVersion`
   - `requestedAction`
   - optional `evidenceExpectations`
   - optional `projectHints` / `workspaceHints`
3. Start or reuse a managed Windows session (for example `psmux`).
4. Launch Codex CLI in the target workspace.
5. Run the local Windows validation or implementation slice.
6. Emit a bounded `windows_lane_evidence_envelope` JSON payload.
7. Queue that payload with `unity-mcp-cli handoff submit-windows-evidence ...`.
8. Await leader reconcile rather than mutating lifecycle state locally.

## Minimal runtime shape

A deliberately small v1 companion can look like this:

```text
windows-companion/
  inbox/             # external coordination only; not owned by Unity-MCP
  logs/
  outbox/
  sessions/
  snapshots/
```

The companion may cache snapshots locally, but those cached files remain **external companion state**, not Unity-MCP state.

## Snapshot handling rules

- Treat the snapshot as a reference artifact, not a command transport.
- Do not infer lifecycle authority from the snapshot.
- Do not mutate the leader-owned handoff ledger directly.
- Do not replace `submit-windows-evidence` with a side channel.
- Keep any retry/supervision policy local to the companion runtime.

## Evidence handoff rules

- Keep `handoffId` and handoff version aligned with the passive snapshot.
- Emit only bounded evidence refs (`log`, `test_report`, `build_artifact`, `screenshot`, `note`).
- Prefer deterministic file locations so operators can inspect failures.
- Leave lifecycle changes to the mac leader after reconcile.

## Deferred follow-ups

These belong to later backlog slices, not this repo boundary freeze:

- Windows validation hardening / version-matched fixture policy
- Discord bridge live-ops validation and deployment runbook
- planner/QA executionization on top of the existing bounded role model
