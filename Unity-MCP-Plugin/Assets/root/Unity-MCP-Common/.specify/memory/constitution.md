# Unity-MCP-Common Constitution

<!--
Sync Impact Report
- Version change: unversioned → 1.0.0
- Modified principles: N/A (initial definition)
- Added sections: Core Principles (5), Additional Constraints, Development Workflow & Quality Gates, Governance
- Removed sections: None
- Templates requiring updates:
  - .specify/templates/plan-template.md ✅ updated
  - .specify/templates/spec-template.md ✅ updated
  - .specify/templates/tasks-template.md ✅ updated
  - .specify/templates/commands/*.md ⚠ pending (no command templates present in repo)
- Deferred TODOs: None
-->

## Core Principles

### I. Stable Wire Contracts First (NON-NEGOTIABLE)

The public JSON data models and SignalR payloads defined in this library are the single
source of truth for interoperability across Unity-MCP-Server and Unity-MCP-Plugin. Any
change to public models is governed by semantic versioning and MUST include tests.

Rules:

- JSON serialization MUST use the central configuration applied via RpcJsonConfiguration
  and Reflector serializer options (naming policy, ignore conditions, converters).
- Public model changes MUST be backward compatible for MINOR; breaking changes require
  a MAJOR version bump and a migration note.
- Round-trip tests (serialize → deserialize) and schema regression tests MUST exist for
  all public DTOs used over SignalR.
- No ad-hoc serializers in feature code—use the configured serializer pipeline only.

Rationale: Cross-repo compatibility depends on stable contracts; tests prevent accidental
breakage and drift between client, server, and plugin.

### II. Attribute-Driven MCP Discovery

MCP Tools, Prompts, and Resources are discovered at runtime via custom attributes.

Rules:

- Handlers MUST be public, discoverable via reflection, and tagged with accurate metadata
  (name, description, input/output schema) to enable generation and routing.
- Handlers SHOULD be deterministic and side-effect aware; any side effects MUST be
  documented in metadata and tested.
- Scanning scope MUST be well-defined to avoid unintended discovery; avoid dynamic code
  generation that evades analysis.

Rationale: Predictable discovery ensures consistent behavior across environments and
enables the server to construct correct MCP objects.

### III. Test-First for Contracts, Runners, and Routing

Testing drives implementation. The Unity-MCP-Common.Tests~ project MUST provide strong
coverage for contracts, runners, and RpcRouter integrations.

Rules:

- Write failing tests for new/changed contracts and runner behaviors before implementing.
- Include negative tests (invalid input, timeouts, cancellation) and idempotency checks
  where applicable.
- Contract tests MUST cover JSON shape, required/optional fields, and default values.

Rationale: This library is shared by multiple projects—tests are the safety net preventing
cross-project regressions.

### IV. Observability and Structured Logging

Use Microsoft.Extensions.Logging with structured messages to trace behavior without
exposing sensitive data.

Rules:

- Log connection lifecycle transitions, retries, and failures with correlation context
  (e.g., the ConnectionManager GUID).
- Prefer Trace/Debug for verbose flow, Info for successful milestones, Warning for
  recoverable issues, Error for failures.
- Do not log payloads that may contain PII or secrets.

Rationale: Distributed debugging requires consistent, low-noise, structured logs.

### V. Resilience, Cancellation, and Unity-Friendly Disposal

Connection and runner code MUST respect cancellation, avoid blocking Unity domain reload,
and implement predictable retry behavior.

Rules:

- Use FixedRetryPolicy or equivalent explicit policies; avoid unbounded tight loops.
- Respect CancellationToken in all async flows; never swallow OperationCanceledException.
- Ensure Dispose/DisposeAsync patterns do not deadlock Unity reload (no .Wait() on async
  tasks in domain reload paths).

Rationale: Unity editor/runtime constraints demand careful resource management.

## Additional Constraints

- Target framework: netstandard2.1; public API compatibility with Unity editor/runtime.
- SignalR is the transport for all remote interactions; RPC routing MUST use the shared
  abstractions in this library.
- Namespaces and assembly definitions MUST remain stable to avoid breaking Unity asmdef
  references.
- JSON serializer configuration is centralized—do not override per feature.
- Performance: reflection scanning SHOULD be cached; avoid scanning hot paths repeatedly.

## Development Workflow & Quality Gates

- Contract Change Process:
  - Propose changes with examples of old/new JSON.
  - Add/adjust round-trip and schema regression tests in Unity-MCP-Common.Tests~.
  - Determine semantic version bump (MAJOR/MINOR/PATCH) and document rationale.
- Testing Gates (required before merge):
  - All unit tests pass; new/changed contracts have tests.
  - Connection behaviors (connect, reconnect, cancellation) covered where touched.
- Code Review:
  - Verify adherence to logging, cancellation, and serializer rules.
  - Check that reflection discovery remains deterministic and scoped.
- Documentation:
  - Update README or feature docs when environment variables or supported transports
    change behavior.

## Governance

- This constitution supersedes ad-hoc practices for contracts, tests, and logging.
- Amendments require a PR with: change summary, version bump rationale, and migration
  notes if breaking.
- Versioning policy for this document:
  - MAJOR: Backward-incompatible governance/principle removals or redefinitions.
  - MINOR: New principle/section added or materially expanded guidance.
  - PATCH: Clarifications, wording, typo fixes without semantic changes.
- Compliance reviews MUST verify Constitution Check gates in plans and PRs.

**Version**: 1.0.0 | **Ratified**: 2025-10-14 | **Last Amended**: 2025-10-14
