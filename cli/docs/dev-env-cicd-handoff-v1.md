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
