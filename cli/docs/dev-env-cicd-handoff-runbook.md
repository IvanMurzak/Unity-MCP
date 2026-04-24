# Dev Environment CI/CD Handoff Runbook (v1)

This runbook defines the v1 approval and verification lane for mixed mac/Windows agent work.
It maps the PRD for mac + OMX leadership, Windows-native Codex execution, chat approval handoffs,
and bot-mediated GitHub Actions dispatch into an auditable operating contract.

## v1 authority model

| Plane | v1 owner | Allowed actions | Explicitly not allowed |
| --- | --- | --- | --- |
| Control | mac + OMX leader | Own the handoff ledger, route work, consume evidence, mutate lifecycle state | Leader failover or alternate control planes |
| Execution | Windows Codex CLI or another approved lane | Perform delegated implementation/validation and submit evidence envelopes | Mutate handoff lifecycle state |
| Approval | Slack or Discord adapter | Notify approvers and submit signed approve/reject intents for known handoff IDs and record versions | Free-form command execution or direct Unity runtime control |
| Automation | Bot bridge | Dispatch allowlisted GitHub Actions from immutable leader-approved snapshots | Write ledger state directly or trigger arbitrary workflows |

Only the leader mutates lifecycle state. Other lanes submit evidence or approval intents that the leader validates against the current handoff record version.

## Handoff lifecycle and default human approval gates

The finite v1 lifecycle contains four handoffs, but human approval is mandatory by default only for the two high-risk gates.

| Handoff | Default human approval? | Owner of transition | Why |
| --- | --- | --- | --- |
| `plan -> execution` | **Required** | Leader after signed approval intent | Authorizes work to leave planning and enter execution lanes |
| `execution -> verification` | Not required by default | Leader-internal | Records implementation evidence ready for formal verification |
| `verification -> CI/CD` | **Required** | Leader after signed approval intent, then bot dispatch | Authorizes expensive/external GitHub Actions automation |
| `CI/CD result -> release/recovery` | Not required by default | Leader-internal unless a failed/ambiguous result needs an operator decision | Records workflow outcome and chooses release or remediation path |

Implementation rule: do not add new mandatory human approval gates in v1 unless a later rollout explicitly promotes a lower-risk transition.

## Handoff record requirements

Every handoff record version must contain enough data to reject stale approvals and reconstruct the audit trail:

- handoff ID
- source lane and target lane
- lifecycle state
- evidence references
- requested action
- approver identity when present
- record version / approval version
- timestamps
- downstream dispatch target when applicable
- dispatch provenance when applicable
- append-only audit events

Allowed states are `draft`, `awaiting_approval`, `approved_not_dispatched`, `dispatched`, `completed`, `rejected`, `frozen`, and `reconcile_needed`.

## Approval adapter contract

Approval messages in Slack or Discord must stay narrow:

1. Show status context, evidence references, handoff ID, record version, and the requested gate.
2. Offer only approve/reject controls for that exact handoff ID and record version.
3. Verify callback signatures and approver identity in the provider adapter.
4. Submit a normalized approval intent to the leader.
5. Let the leader reject stale, replayed, closed, superseded, or unauthorized approvals.

Provider differences may affect transport and bootstrap only. They must not change lifecycle states, approval meaning, replay checks, or CI/CD dispatch policy.

## Verification -> CI/CD relay

The initial concrete v1 relay receiver is:

- `.github/workflows/test_pull_request_manual.yml`

Default path:

1. The leader marks a verified handoff `awaiting_approval` for the `verification -> CI/CD` gate.
2. A chat approver approves the exact handoff ID and record version.
3. The leader validates the signed intent and records `approved_not_dispatched`.
4. The bot bridge dispatches GitHub Actions with `repository_dispatch` event type `unity-mcp-approved-verification`.
5. The dispatch payload includes minimal approved context such as `handoff_id`, `record_version`, `approval_gate`, and `dispatch_provenance`.
6. The leader records dispatch provenance and later workflow result evidence back into the ledger.

`workflow_dispatch` remains available only as an operator fallback, not as the default v1 automation path.

## Freeze / reconcile behavior

If the mac + OMX leader is unavailable:

- do not open new handoffs
- do not consume approvals into downstream execution
- do not dispatch `approved_not_dispatched` records
- allow in-flight lanes to finish local work and queue evidence envelopes
- resume only after the leader reconciles current record versions and applies queued evidence once

Approval intents or evidence envelopes that target stale record versions must move to manual recovery (`reconcile_needed`) instead of silently advancing.

## Acceptance mapping

| Acceptance criterion | Evidence in this repo |
| --- | --- |
| mac + OMX is the only v1 control plane | Authority model above |
| Windows Codex is an execution/validation lane | Authority model above |
| Four finite handoffs with only two default human gates | Handoff lifecycle table above |
| Chat is notify + approve/reject only | Approval adapter contract above |
| Approved verification handoff can trigger GitHub Actions without manual UI | Repository dispatch relay section and `.github/workflows/test_pull_request_manual.yml` |
| Leader outage freezes promotions and requires reconcile | Freeze / reconcile behavior above |
| No direct Unity runtime chat control | Authority and approval adapter tables above |
| Provider-neutral lifecycle semantics | Approval adapter contract above |
| Only leader mutates lifecycle state | Authority model and handoff record requirements above |

## Verification checklist

Before closing v1 changes, verify:

- docs state exactly two default mandatory human approval gates: `plan -> execution` and `verification -> CI/CD`
- `.github/workflows/test_pull_request_manual.yml` accepts `repository_dispatch` for `unity-mcp-approved-verification`
- repository dispatch payload validation rejects missing approval context before CI jobs run
- `workflow_dispatch` remains available as fallback only
- tests assert the docs/workflow mapping so future edits cannot silently add approval gates or remove the relay
