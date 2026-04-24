# Windows Codex lane (v1)

This document describes the **bounded** Windows lane for the mixed mac/Windows handoff model.

Concrete starter artifacts live under:

- `cli/examples/windows-codex-lane/sample-windows-evidence.json`
- `cli/examples/windows-codex-lane/submit-windows-evidence.ps1`

## Boundary

Unity-MCP still does **not** own a generic worker task-dispatch system. The Windows lane is intentionally narrower:

- an **external** Windows runner (for example `psmux + Codex CLI + Unity-MCP`) performs the actual execution
- Unity-MCP accepts only a bounded `windows_lane_evidence_envelope`
- the mac leader later reconciles that evidence into the canonical handoff ledger

This keeps Unity-MCP inside its approved scope:

- standalone Unity lifecycle + handoff ledger ✅
- bounded evidence intake from a Windows lane ✅
- generic mailbox/inbox/task routing inside Unity-MCP ❌

## Recommended external runtime shape

The recommended v1 Windows runtime remains external to Unity-MCP:

1. `psmux` (or equivalent Windows process/session manager) keeps Codex workers alive
2. Codex CLI executes the assigned implementation/validation work
3. Unity-MCP is exposed to the worker as an MCP tool/client target
4. the worker emits a bounded evidence envelope JSON file
5. Unity-MCP queues and later reconciles that evidence through `handoff` commands

## Queue + reconcile flow

### 1. External worker emits a bounded envelope

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

### 2. Queue the Windows evidence in Unity-MCP

This step is safe to run on Windows because it only validates and stores the bounded envelope under `.unity-mcp/handoff-spool/windows-evidence/`.

```bash
unity-mcp-cli handoff submit-windows-evidence ./MyGame --input-file windows-evidence.json
```

To inspect the queue before the leader reconciles:

```bash
unity-mcp-cli handoff list-windows-evidence ./MyGame
unity-mcp-cli handoff list-windows-evidence ./MyGame --handoff-id verification-handoff-1
```

### 3. Reconcile from the mac leader

Later, the leader applies queued evidence into the canonical ledger:

```bash
unity-mcp-cli handoff reconcile-windows-evidence ./MyGame --leader-actor mac-omx-leader
```

Optional handoff scoping:

```bash
unity-mcp-cli handoff reconcile-windows-evidence ./MyGame --handoff-id verification-handoff-1
```

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

1. watch a task source outside Unity-MCP (OMX, a local script, or a handoff assignment artifact)
2. start Codex CLI in a managed `psmux` session
3. run the Windows-native validation
4. write a bounded evidence JSON file
5. call `unity-mcp-cli handoff submit-windows-evidence ...`

The included PowerShell example shows one simple pattern:

```powershell
pwsh -File cli/examples/windows-codex-lane/submit-windows-evidence.ps1 `
  -ProjectPath D:\workSpace\Unity-MCP\Unity-MCP-Plugin `
  -HandoffId verification-handoff-1 `
  -HandoffVersion 3
```

That means Unity-MCP stays responsible for:

- project-local handoff records
- bounded evidence validation
- leader reconcile

And the external runtime stays responsible for:

- task execution
- process/session supervision
- Windows-native editor/tool usage

## Non-goals

This v1 slice still does **not** add:

- a Unity-MCP-owned Windows mailbox/task runner
- generic multi-agent scheduling inside Unity-MCP
- remote dispatch or cloud worker coordination
