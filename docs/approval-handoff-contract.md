# Approval Handoff Contract v1

This contract defines the provider-neutral approval handoff used by the mixed mac/Windows agent workflow. In v1, the only concrete chat provider is **Discord**. Slack is intentionally deferred until the provider-neutral contract is proven with Discord.

## Scope

Chat integrations are limited to:

1. notifying humans that a leader-owned handoff is awaiting a decision;
2. collecting an approve or reject decision for a known handoff ID and record version; and
3. submitting a normalized approval intent back to the mac + OMX leader.

Chat integrations must not execute arbitrary commands, mutate handoff lifecycle state directly, dispatch CI/CD directly, or control the Unity Editor/runtime.

## Actors and authority

| Actor | v1 authority |
| --- | --- |
| mac + OMX leader | Owns the canonical handoff ledger and is the only writer for lifecycle state transitions. |
| Windows Codex lane | Submits execution or validation evidence envelopes only. |
| Approval adapter contract | Defines the normalized notification and approval-intent shape. |
| Discord adapter | Implements the v1 chat provider by sending notifications and submitting approval intents only. |
| Slack adapter | Deferred; must implement the same provider-neutral contract before activation. |

## Provider-neutral notification

A notification is a read-only view of a pending handoff. It may include only bounded context needed for a decision:

- handoff ID;
- handoff record version;
- gate type, such as `plan_to_execution` or `verification_to_ci_cd`;
- requested action summary;
- source lane and target lane;
- evidence references supplied by the leader;
- expiration or stale-action warning when available; and
- approve/reject controls bound to the exact handoff ID and record version.

Notifications do not carry executable commands. Any provider-specific message IDs are adapter metadata, not lifecycle state.

## Provider-neutral approval intent

An approval intent is the only mutation-like artifact a chat adapter may submit. It is an input for leader validation; it does not mutate the ledger by itself.

Required fields:

| Field | Meaning |
| --- | --- |
| `intent_id` | Provider-unique, idempotency-safe intent identifier. |
| `provider` | Chat provider name. v1 value: `discord`. |
| `handoff_id` | Leader-owned handoff ID being approved or rejected. |
| `handoff_version` | Exact record version shown to the approver. |
| `decision` | `approve` or `reject` only. |
| `actor` | Authenticated provider user identity plus any mapped organization/team identity. |
| `created_at` | Provider event timestamp. |
| `provider_event` | Provider interaction metadata needed for audit and replay protection. |
| `verification` | Signature/timestamp verification result and adapter validation metadata. |

Leader-side validation must reject intents when the handoff is unknown, not awaiting approval, already closed, version-mismatched, stale, replayed, or submitted by an unauthorized actor.

## Discord adapter v1

Discord is the first concrete provider because it satisfies the v1 interaction shape without requiring chat to become a control plane. The Discord adapter must:

- render leader-created notifications as Discord messages with approve/reject controls;
- verify Discord interaction signatures and timestamps before normalization;
- normalize Discord button interactions into the provider-neutral approval intent shape;
- submit only approval intents to the leader-owned ingestion path;
- keep Discord message/channel IDs as adapter metadata only;
- acknowledge provider interactions quickly while leaving lifecycle decisions to the leader; and
- surface rejection/stale/version errors as feedback messages without changing ledger state.

The Discord adapter must not:

- accept free-form chat commands;
- trigger GitHub Actions or other CI/CD workflows directly;
- write the handoff ledger directly;
- advance handoffs after leader outage; or
- control Unity Editor/runtime state.

## Deferred Slack adapter

Slack support is deferred for v1. A future Slack adapter may be added only if it implements the same notification and approval-intent contract, preserves the same ledger authority model, and differs only in provider bootstrap, signing, and transport details.

## Lifecycle placement

The approval contract applies to the default human approval gates in v1:

1. `plan_to_execution`;
2. `verification_to_ci_cd`.

Other handoff transitions may be recorded by the leader but do not require chat approval by default in v1.

## Failure behavior

- If the leader is unavailable, Discord may receive interactions but they must remain unconsumed until leader recovery and version validation.
- If an intent is stale or replayed, the leader rejects it and the adapter may notify the actor of the reason.
- If Discord delivery fails, the handoff remains leader-owned and awaiting approval until the leader retries, expires, or marks it for reconcile.
