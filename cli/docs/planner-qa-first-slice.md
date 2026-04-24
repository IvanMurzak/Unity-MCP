# Planner + QA First Slice Modeling

This document captures the first multi-discipline modeling slice for Unity-MCP after the mixed mac/Windows handoff model.

## Bounded scope

The first slice adds only two bounded role types:

- `planner`
- `qa`

Deferred for later:

- `artist`
- `map-designer`

Planner and QA are not new canonical lanes. They operate inside the existing **leader-owned** coordination model and may only produce advisory tasks, artifacts, and evidence.

## Canonical authority rule

- Canonical lifecycle state stays in the leader-owned handoff ledger.
- Planner and QA outputs must reference:
  - `relatedHandoffId`
  - `relatedHandoffRecordVersion`
- Planner and QA must not mutate canonical lifecycle state directly.
- Discord remains review/approval only.
- GitHub Issues remain ops tracking only.

## Role registry

### Planner
- Responsibilities:
  - PRD and plan authoring
  - task graph decomposition
  - dependency/risk framing for existing approval gates
- Allowed outputs:
  - `prd_brief`
  - `plan_spec`
  - `task_graph`
- Handoff targets:
  - `leader`
  - `qa`

### QA
- Responsibilities:
  - verification verdicts
  - bug triage
  - regression assessment
  - readiness signals
- Allowed outputs:
  - `qa_verdict`
  - `bug_triage`
  - `regression_report`
  - `readiness_assessment`
- Handoff targets:
  - `leader`
  - `planner`
  - `approval-surface-via-leader`

## Task schema

Required first-slice fields:

- `taskId`
- `schemaVersion`
- `roleType`
- `taskType`
- `status`
- `priority`
- `riskLevel`
- `relatedHandoffId`
- `relatedHandoffRecordVersion`
- `laneBinding`
- `requestedBy`
- `ownedBy`
- `createdAt`
- `inputs[]`
- `acceptanceCriteria[]`
- `dependsOn[]`
- `produces[]`
- `reviewPolicy`
- `escalationPolicy`

Planner task types:
- `prd_author`
- `plan_author`
- `task_graph_decompose`
- `replan_from_feedback`

QA task types:
- `verify_change`
- `triage_bug`
- `regression_check`
- `release_readiness_review`

## Artifact schema

Required first-slice fields:

- `artifactId`
- `schemaVersion`
- `artifactType`
- `producerRole`
- `taskId`
- `version`
- `relatedHandoffId`
- `relatedHandoffRecordVersion`
- `laneBinding`
- `summary`
- `inlineBody` or `bodyRef`
- `createdAt`
- `inputRefs[]`
- `evidenceRefs[]`
- `decisionMetadata.confidence`
- `decisionMetadata.riskLevel`
- `decisionMetadata.recommendedNextAction`
- `reviewState`

Planner artifact types:
- `prd_brief`
- `plan_spec`
- `task_graph`

QA artifact types:
- `qa_verdict`
- `bug_triage`
- `regression_report`
- `readiness_assessment`

## QA HITL policy

### L1 / Low
- leader may ingest QA evidence directly
- no new approval gate is created
- promotion may continue through existing gates only

### L2 / Medium
- human QA review is required
- promotion readiness cannot be asserted until review arrives
- canonical handoff state does not advance during the hold

### L3 / High
- explicit human QA approval is required
- leader should use existing `frozen` or `reconcile_needed` behavior
- no unfreeze/promotion until the human QA hold is resolved

## Gate mapping

- Planner outputs feed the existing `plan -> execution` gate through leader review.
- QA outputs feed existing leader-owned readiness decisions.
- QA does not create a parallel approval lane.
- Medium/high QA outcomes may block an existing promotion gate, but they do not create a new canonical state machine.
