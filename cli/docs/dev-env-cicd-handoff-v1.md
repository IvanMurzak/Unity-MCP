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

## Windows Codex lane contract

Windows Codex CLI is a Windows-native execution and validation lane. It may run delegated implementation, Unity, and validation work on Windows, then submit evidence envelopes back to the mac + OMX leader. It is not a standby leader and must not open, approve, promote, dispatch, or close handoffs.

Evidence from Windows Codex must include the handoff ID and record version it belongs to. If the leader has advanced or superseded the record, the evidence is queued for manual reconcile instead of changing lifecycle state directly.

## Canonical ledger contract

The mac + OMX leader owns one append-only ledger for v1 handoffs. Records use the normalized state set:

`draft -> awaiting_approval -> approved_not_dispatched -> dispatched -> completed`

Terminal/exception states are `rejected`, `frozen`, and `reconcile_needed`. `approved_not_dispatched` is intentional: it records that a human-approved handoff has not yet crossed into the bot/CI boundary, so an outage cannot accidentally trigger downstream work.

Every record version must carry the handoff ID, type, source lane, target lane, state, evidence refs, approval/dispatch provenance when present, timestamps, and record version. Non-leader lanes must reference a handoff ID plus record version; stale versions are ignored during reconcile.

## Freeze and reconcile behavior

If the mac + OMX leader is unavailable:

1. No new handoffs open.
2. `awaiting_approval` and `approved_not_dispatched` records freeze instead of promoting.
3. Already-dispatched work moves to `reconcile_needed` until the leader can inspect results.
4. Windows Codex may finish local work, but it queues evidence envelopes only.
5. On resume, the leader applies matching evidence once, ignores duplicates/stale versions, and then decides the next lifecycle transition.

This is freeze-and-wait, not failover.

## Chat and CI/CD boundaries

Slack or Discord messages may contain status context plus approve/reject controls bound to known handoff IDs and record versions. They must not contain arbitrary command execution or direct Unity runtime controls.

The bot bridge may dispatch only leader-approved snapshots to allowlisted GitHub Actions targets. For v1, `.github/workflows/test_pull_request_manual.yml` is the documented relay target; machine-driven integrations should use `repository_dispatch`, while `workflow_dispatch` remains an operator fallback.
