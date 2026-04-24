# Mixed mac/Windows agent handoff model (v1)

This v1 contract keeps **mac + OMX as the sole orchestration control plane**. Other lanes may execute work, collect evidence, request approvals, or relay CI/CD dispatches, but they do not become alternate lifecycle authorities.

## Control-plane rule

- **Only:** mac + OMX leader
- **Owns:** plan state, routing, handoff lifecycle mutation, promotion/freeze decisions, reconciliation after outage
- **Does not delegate in v1:** canonical lifecycle state writes, promotion authority, or leader failover

Every v1 handoff lifecycle mutation must be made by the mac + OMX leader. If another lane has useful output, it submits an evidence envelope or an approval/dispatch event for the leader to validate and apply.

## Lane responsibilities

| Lane | v1 role | May mutate lifecycle state? | Allowed output |
| --- | --- | --- | --- |
| mac + OMX leader | control plane | yes | versioned handoff records and lifecycle transitions |
| Windows Codex CLI | execution/validation lane | no | bounded evidence envelopes |
| Slack or Discord | approval hub | no | signed approve/reject intents for known handoff IDs and versions |
| Bot CI/CD bridge | dispatch relay | no | dispatch provenance/results for leader reconciliation |

## Operating invariant

If the mac + OMX leader is unavailable, promotions freeze. In-flight lanes may finish local work and queue evidence, but no downstream handoff or CI/CD dispatch proceeds until the leader resumes and reconciles the queued inputs.


## Current CLI bridge surface

The repository now exposes six bounded handoff commands:

- `unity-mcp-cli handoff notify-discord <handoff-id> <project-path>`
- `unity-mcp-cli handoff serve <project-path>`
- `unity-mcp-cli handoff dispatch-approved <handoff-id> <project-path>`
- `unity-mcp-cli handoff submit-windows-evidence <project-path>`
- `unity-mcp-cli handoff list-windows-evidence <project-path>`
- `unity-mcp-cli handoff reconcile-windows-evidence <project-path>`

`notify-discord` reads a leader-owned handoff record that is already `awaiting_approval`, sends a Discord approval message with approve/reject buttons, and persists message metadata under `.unity-mcp/handoff-spool/discord-notifications/`.

`serve` exposes a local HTTP bridge for Discord interactions:

- `GET /healthz`
- `POST /discord/interactions`

The bridge validates Discord request signatures (`X-Signature-Ed25519`) and timestamps (`X-Signature-Timestamp`), normalizes the button click into a provider-neutral approval intent, writes a queued approval-intent spool record under `.unity-mcp/handoff-spool/approval-intents/`, and then applies the decision through the leader-owned handoff ledger.

## Bridge env-file keys

The bridge reads direct environment variables or an optional `--env-file` containing:

- `UNITY_MCP_HANDOFF_DISCORD_BOT_TOKEN`
- `UNITY_MCP_HANDOFF_DISCORD_PUBLIC_KEY`
- `UNITY_MCP_HANDOFF_DISCORD_APPROVAL_CHANNEL_ID`
- `UNITY_MCP_HANDOFF_ALLOWED_APPROVER_IDS`
- `UNITY_MCP_HANDOFF_LEADER_ACTOR`
- `UNITY_MCP_HANDOFF_PORT`


`dispatch-approved` is the bounded GitHub relay path for v1. It requires an already `approved_not_dispatched` `verification_to_cicd` handoff, emits `repository_dispatch` with event type `unity-mcp-approved-verification` by default, and records GitHub dispatch provenance back into the leader-owned ledger as the handoff moves to `dispatched`.

`submit-windows-evidence` is the bounded Windows lane intake path. It validates a `windows_lane_evidence_envelope` JSON payload and stores it under `.unity-mcp/handoff-spool/windows-evidence/` without mutating lifecycle state.

`list-windows-evidence` gives operators visibility into queued/applied/pending-error Windows evidence spool records without inspecting the spool directory manually.

`reconcile-windows-evidence` is the leader-only replay step. It reads queued Windows evidence spool records, normalizes the structured evidence refs into the existing string-based handoff ledger, applies them through the leader-owned ledger, and leaves stale/error cases queued with a recorded `lastError`.


## Planner + QA bounded role types

The first multi-discipline slice adds `planner` and `qa` as **bounded role types** inside the current leader-owned model.

- They are not new canonical lanes.
- They do not mutate lifecycle state.
- They must reference `relatedHandoffId` and `relatedHandoffRecordVersion` when their outputs can affect promotion or readiness.
- Discord remains a review/approval surface only, and GitHub Issues remain ops tracking only.

Planner outputs feed the existing `plan -> execution` gate through leader review. QA outputs feed existing leader-owned readiness decisions and may block promotion at medium/high risk without creating a separate QA approval plane.

## External Windows runner shape

The Windows lane is still intentionally external to Unity-MCP. A practical v1 setup is:

- `psmux` (or similar) supervises Codex CLI worker processes
- Codex CLI uses Unity-MCP against the local Windows Unity project
- the worker emits a bounded evidence JSON file
- Unity-MCP queues that file with `submit-windows-evidence`
- the mac leader later runs `reconcile-windows-evidence`

See [`cli/docs/windows-codex-lane-v1.md`](windows-codex-lane-v1.md) for the concrete queue/reconcile contract.
