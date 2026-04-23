# Milestone 1 Scope Guard — tmux orchestration fork

## Purpose
Preserve the approved milestone-1 boundary for the tmux orchestration fork so implementation stays limited to the smallest CLI-first slice.

## Source of truth
- `.omx/plans/prd-tmux-orchestration-fork-plan.md`
- `.omx/plans/test-spec-tmux-orchestration-fork-plan.md`

## In scope
1. New CLI `team` command family attached to the existing CLI entrypoint.
2. Local tmux session launch with a deterministic default role/pane layout.
3. Persisted orchestration state sufficient for `team status` and `team list`.
4. Basic management commands for milestone 1:
   - `team launch`
   - `team status`
   - `team list`
   - `team stop` is optional but acceptable inside the same lifecycle slice.
5. Live tmux reconciliation for status reporting, including degraded/stale-state messaging.
6. Tests/docs strictly required to support the above command surface.

## Preferred implementation surface
- `cli/src/index.ts`
- `cli/src/commands/team/*`
- `cli/src/utils/tmux.ts`
- `cli/src/utils/team-state.ts`
- `cli/src/utils/team-templates.ts`
- matching tests/docs for the same surface

## Out of scope for milestone 1
- OMX feature parity
- remote/cloud/multi-machine orchestration
- HUD/question/interview workflows
- deep role automation or handoff engines
- Unity plugin or Unity-MCP server refactors as part of this milestone
- broad CLI architecture reshaping beyond adding the `team` family and its supporting utilities

## Acceptance guardrails
Implementation should satisfy all of the following:
1. The new behavior enters through the existing CLI registration surface.
2. Session state is persisted independently from raw tmux inspection.
3. `team list` can summarize saved sessions without requiring manual tmux parsing.
4. Missing tmux and stale session cases produce actionable output.
5. Documentation describes the feature as local-only and milestone-1 scoped.

## Scope drift signals
Escalate immediately if implementation starts to introduce any of the following:
- changes in Unity plugin/server runtime behavior
- cloud or remote session management
- OMX-style workflow engines beyond simple role presets/metadata
- unrelated command rewrites outside the `team` family
- state model expansion driven by future milestones rather than milestone-1 needs

## Review checklist
Use this quick review before accepting milestone-1 implementation changes:
- Does the diff stay centered on `team` command registration, tmux helpers, state helpers, templates, and directly related tests/docs?
- Does any new option/behavior serve launch, status, list, or optional stop directly?
- Can the saved state still be understood without live tmux access?
- Are missing-tmux and stale-state paths explicit and actionable?
- Did the change avoid introducing remote/cloud or non-local orchestration assumptions?

## Notes for review
If a change is ambiguous, prefer the narrower interpretation that keeps the first deliverable to launcher + state + basic management only.
