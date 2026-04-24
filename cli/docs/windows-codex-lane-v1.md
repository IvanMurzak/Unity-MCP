# Windows Codex lane (v1)

This document describes the **bounded** Windows lane for the mixed mac/Windows handoff model.

Concrete starter artifacts live under:

- `cli/examples/windows-codex-lane/sample-windows-handoff-snapshot.json`
- `cli/examples/windows-codex-lane/sample-windows-evidence.json`
- `cli/examples/windows-codex-lane/run-windows-validation-runner-v1.ps1`
- `cli/examples/windows-codex-lane/submit-windows-evidence.ps1`

Companion implementation guidance lives in [`cli/docs/windows-codex-runner-companion-v1.md`](windows-codex-runner-companion-v1.md).

## Boundary

Unity-MCP still does **not** own a generic worker task-dispatch system. The Windows lane is intentionally narrower:

- an **external** Windows runner (for example `psmux + Codex CLI + Unity-MCP`) performs the actual execution
- external coordination may hand that runner a **passive Windows handoff snapshot** for reference
- Unity-MCP does **not** receive or store snapshot submissions
- Unity-MCP accepts only a bounded `windows_lane_evidence_envelope`
- the mac leader later reconciles that evidence into the canonical handoff ledger

This keeps Unity-MCP inside its approved scope:

- standalone Unity lifecycle + handoff ledger ✅
- passive snapshot contract for external Windows runners ✅
- bounded evidence intake from a Windows lane ✅
- generic mailbox/inbox/task routing inside Unity-MCP ❌
- assignment submission, assignment spool ownership, polling, or dispatch inside Unity-MCP ❌

## Recommended external runtime shape

The recommended v1 Windows runtime remains external to Unity-MCP:

1. an external coordination source provides a **passive snapshot** that references the approved handoff
2. `psmux` (or equivalent Windows process/session manager) keeps Codex workers alive
3. Codex CLI executes the referenced implementation/validation work
4. Unity-MCP is exposed to the worker as an MCP tool/client target
5. the worker runs `windows_validation_smoke_v1`
6. the worker emits a bounded evidence envelope JSON file
7. Unity-MCP queues that evidence only through `handoff submit-windows-evidence`
8. the worker stops and waits for the mac leader to reconcile the evidence later

## Passive snapshot -> evidence -> reconcile loop

Bounded loop: passive snapshot -> external runner -> evidence envelope -> `submit-windows-evidence` -> `reconcile-windows-evidence`.

### 1. External coordination provides a passive snapshot

The passive snapshot is **descriptive/reference-only**. It tells the Windows runner which handoff/version it is working against and what evidence the leader expects, but it is not a command transport, polling contract, mailbox, or retry policy.

Example payload:

```json
{
  "snapshotId": "snapshot-verification-handoff-1-v3",
  "handoffId": "verification-handoff-1",
  "handoffRecordVersion": 3,
  "requestedAction": "Run Windows validation and produce bounded evidence for leader reconcile.",
  "sourceLane": "mac-omx-leader",
  "targetLane": "windows-codex",
  "createdAt": "2026-04-24T11:50:00.000Z",
  "evidenceExpectations": [
    {
      "evidenceType": "log",
      "description": "Unity/worker log for the run.",
      "required": true,
      "exampleUri": "file:///C:/unity-mcp-agent/logs/worker-1.log"
    },
    {
      "evidenceType": "test_report",
      "description": "Validation test report when available."
    }
  ],
  "projectHints": {
    "projectPathHint": "D:\\workSpace\\Unity-MCP\\Unity-MCP-Plugin",
    "unityProjectPathHint": "D:\\workSpace\\Unity-MCP\\Unity-MCP-Plugin",
    "unityEditorVersionHint": "6000.3.6f1"
  },
  "workspaceHints": {
    "workingDirectoryHint": "D:\\workSpace\\Unity-MCP",
    "artifactDirectoryHint": "C:\\unity-mcp-agent\\outbox",
    "logDirectoryHint": "C:\\unity-mcp-agent\\logs",
    "branchHint": "codex/tmux-team-orchestration"
  },
  "notes": [
    "Reference-only snapshot; Unity-MCP does not ingest it directly."
  ]
}
```

The runner may validate the schema and required fields, echo `handoffId` / `handoffRecordVersion`, and resolve local path hints. It must not perform authoritative freshness checks, lifecycle interpretation, or post-submit polling.

### 2. External worker emits a bounded envelope

Example payload:

```json
{
  "schemaVersion": 1,
  "kind": "windows_lane_evidence_envelope",
  "handoffId": "verification-handoff-1",
  "handoffVersion": 3,
  "sourceLane": {
    "kind": "windows_codex",
    "laneId": "windows-runner-1"
  },
  "submittedAt": "2026-04-24T12:00:00.000Z",
  "outcome": "passed",
  "summary": "Windows validation passed after Unity smoke + tests.",
  "evidenceRefs": [
    {
      "type": "log",
      "uri": "file:///C:/unity-mcp-agent/logs/worker-1.log"
    },
    {
      "type": "test_report",
      "uri": "file:///C:/unity-mcp-agent/outbox/vitest.xml"
    }
  ]
}
```

Valid outcomes remain bounded to `passed`, `failed`, or `blocked`.

### 3. Queue the Windows evidence in Unity-MCP

This step is safe to run on Windows because it only validates and stores the bounded envelope under `.unity-mcp/handoff-spool/windows-evidence/`.

```bash
unity-mcp-cli handoff submit-windows-evidence ./MyGame --input-file windows-evidence.json
```

The runner stops here. It does **not** poll Unity-MCP with `list-windows-evidence` and it does **not** run `reconcile-windows-evidence`.

### 4. Reconcile from the mac leader

Later, the leader applies queued evidence into the canonical ledger:

```bash
unity-mcp-cli handoff reconcile-windows-evidence ./MyGame --leader-actor mac-omx-leader
```

Optional handoff scoping:

```bash
unity-mcp-cli handoff reconcile-windows-evidence ./MyGame --handoff-id verification-handoff-1
```

## Validation-first v1 profile

The external runner's named v1 profile is `windows_validation_smoke_v1`.

1. `npm test` in `cli/`
2. `npm run build` in `cli/`
3. `node dist/index.js team --help`
4. lifecycle smoke with `team launch`, `team status`, saved-state inspection for `runtime.kind = process`, `team list`, and `team stop`
5. degraded-state smoke by killing one role process and confirming `team status` becomes degraded
6. optional Unity + MCP end-to-end validation when the local editor/project setup already exists

Evidence stays bounded to `log`, `test_report`, `build_artifact`, `screenshot`, and `note` refs.

## Companion-local directories

The external runner may keep its own local directories such as:

- `snapshots/`
- `logs/`
- `outbox/`
- optional `sessions/`

These remain companion-owned local state only. Companion `outbox/` is not a Unity-MCP queue; `.unity-mcp/handoff-spool/windows-evidence/` remains the only repo-owned evidence spool.

## Spool layout

The Windows lane writes only to the bounded evidence spool:

```text
.unity-mcp/
  handoff-spool/
    windows-evidence/
      <sha256>.json
```

Each spool record tracks:

- original envelope
- handoff id + version
- lane id
- stored timestamp
- consumed/applied status
- last reconcile error, if any

## Reconcile rules

The leader reconcile pass currently follows these rules:

- apply only **unconsumed** Windows evidence spool records
- require the handoff record to exist
- require the live handoff record version to be **at least** the queued evidence version
- normalize structured evidence refs into the string-based handoff ledger
- keep failed/stale records queued with `lastError` for a later reconcile pass

This preserves the v1 `freeze-and-wait` model:

- Windows may finish local validation while the leader is unavailable
- canonical lifecycle state still changes only when the leader reconciles

## Practical psmux/Codex integration

The external Windows runner can be very small. A practical shape is:

1. obtain a passive handoff snapshot from external coordination
2. start Codex CLI in a managed `psmux` session
3. run the Windows-native validation against the referenced project/workspace hints
4. write a bounded evidence JSON file
5. call `unity-mcp-cli handoff submit-windows-evidence ...`
6. stop and wait for the mac leader to reconcile the evidence

The included PowerShell examples show one simple pattern:

```powershell
pwsh -File cli/examples/windows-codex-lane/run-windows-validation-runner-v1.ps1 `
  -SnapshotPath cli/examples/windows-codex-lane/sample-windows-handoff-snapshot.json `
  -ProjectPath D:\workSpace\Unity-MCP\Unity-MCP-Plugin `
  -WorkspacePath D:\workSpace\Unity-MCP
```

```powershell
pwsh -File cli/examples/windows-codex-lane/submit-windows-evidence.ps1 `
  -ProjectPath D:\workSpace\Unity-MCP\Unity-MCP-Plugin `
  -HandoffId verification-handoff-1 `
  -HandoffVersion 3
```

That means Unity-MCP stays responsible for:

- project-local handoff records
- passive snapshot contract validation in-repo
- bounded evidence validation
- leader reconcile

And the external runtime stays responsible for:

- obtaining snapshots from external coordination
- task execution
- process/session supervision
- Windows-native editor/tool usage
- evidence emission
- stopping after submit without any post-submit polling loop

## Non-goals

This v1 slice still does **not** add:

- a Unity-MCP-owned Windows mailbox/task runner
- generic multi-agent scheduling inside Unity-MCP
- snapshot submission commands or assignment spools inside Unity-MCP
- polling APIs or dispatch ownership inside Unity-MCP
- remote dispatch or cloud worker coordination
