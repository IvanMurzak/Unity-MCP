# Unity orchestration positioning audit

Source plan:
- `.omx/plans/prd-unity-orchestration-positioning.md`
- `.omx/plans/test-spec-unity-orchestration-positioning.md`

## Keep / reframe / defer

| Area | Status | Why |
| --- | --- | --- |
| `cli/src/utils/team-runtime.ts` and backend-neutral runtime contract | Keep | Supports cross-platform Unity lifecycle behavior without making tmux the public abstraction |
| `cli/src/utils/team-orchestration.ts` and `cli/src/utils/team-state.ts` | Keep | Project-local lifecycle state and reconciliation remain central to the standalone value |
| `cli/src/commands/team/*.ts` runtime-backed command surface | Reframe | Keep the command surface, but describe it as a Unity project session lifecycle rather than generic orchestration |
| `cli/README.md` `team` section | Reframe | Standalone-first wording and explicit OMX complement boundary are needed |
| `cli/docs/team-runtime-architecture.md` | Reframe | Must explain the runtime layer as support for Unity lifecycle semantics rather than a generic orchestrator roadmap |
| generic worker dispatch / mailbox concepts | Defer / out of scope | PRD explicitly keeps them outside Unity-MCP responsibility |
| remote/cloud orchestration | Defer / out of scope | First-release scope stays local-only |

## Wording changes required

- Replace “local team orchestration” framing with “Unity project session lifecycle” where it describes product value.
- State explicitly that OMX is optional and additive, not required for the core lifecycle.
- Keep tmux described as the currently shipped backend, not the identity of the feature.
- Avoid language that implies Unity-MCP is trying to reach OMX parity or become a generic agent coordinator.

## Command naming note

`team` can remain as the public command name for v1 **if** the help text and docs keep the semantics anchored on Unity project lifecycle rather than generic worker orchestration.

Potential future follow-up:
- revisit whether a more lifecycle-specific public name would better match the product position after the standalone flow stabilizes

## Regression notes

The following behaviors should remain intact while the positioning is reframed:

- project-local saved state under `.unity-mcp/team-state/`
- lifecycle statuses (`launching`, `ready`, `degraded`, `stopped`)
- saved-state + live-runtime reconciliation
- runtime-backed implementation that can choose different local backends later
- degraded/missing-runtime messaging
