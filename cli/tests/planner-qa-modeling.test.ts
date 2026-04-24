import { describe, expect, it } from 'vitest';
import {
  PLANNER_QA_ROLE_REGISTRY,
  assertPlannerQaArtifactRecord,
  assertPlannerQaTaskRecord,
  getQaHitlPolicyRule,
} from '../src/utils/planner-qa-modeling.js';
import {
  DEV_ENV_BOUNDED_ROLE_BINDINGS,
  DEV_ENV_CONTROL_PLANE_LANE_ID,
  getDevEnvBoundedRoleBinding,
} from '../src/utils/dev-env-handoff.js';

describe('planner + QA first-slice modeling', () => {
  it('defines bounded planner and QA role registry entries without canonical mutation authority', () => {
    expect(PLANNER_QA_ROLE_REGISTRY.map(role => role.roleId)).toEqual(['planner', 'qa']);
    expect(PLANNER_QA_ROLE_REGISTRY.every(role => role.operatesWithinLeaderOwnedLanes)).toBe(true);
    expect(PLANNER_QA_ROLE_REGISTRY.every(role => !role.canMutateCanonicalState)).toBe(true);
    expect(PLANNER_QA_ROLE_REGISTRY[0]?.allowedOutputs).toEqual(['prd_brief', 'plan_spec', 'task_graph']);
    expect(PLANNER_QA_ROLE_REGISTRY[1]?.allowedOutputs).toEqual(['qa_verdict', 'bug_triage', 'regression_report', 'readiness_assessment']);

    expect(getDevEnvBoundedRoleBinding('planner')).toEqual({
      roleType: 'planner',
      laneId: DEV_ENV_CONTROL_PLANE_LANE_ID,
      canMutateLifecycleState: false,
      lifecycleAuthority: 'leader-only',
    });
    expect(DEV_ENV_BOUNDED_ROLE_BINDINGS.every(binding => binding.laneId === DEV_ENV_CONTROL_PLANE_LANE_ID)).toBe(true);
  });

  it('validates planner/QA tasks with relatedHandoffRecordVersion and role-bounded task types', () => {
    const task = assertPlannerQaTaskRecord({
      schemaVersion: 1,
      taskId: 'task-1',
      roleType: 'planner',
      taskType: 'task_graph_decompose',
      status: 'pending',
      priority: 1,
      riskLevel: 'medium',
      relatedHandoffId: 'handoff-1',
      relatedHandoffRecordVersion: 3,
      laneBinding: 'leader-coordinated',
      requestedBy: 'leader',
      ownedBy: 'planner-1',
      createdAt: '2026-04-24T00:00:00.000Z',
      inputs: ['brief-1'],
      acceptanceCriteria: ['Produces bounded task graph'],
      dependsOn: [],
      produces: ['task_graph'],
      reviewPolicy: 'qa-review',
      escalationPolicy: 'blocked',
    });

    expect(task.relatedHandoffRecordVersion).toBe(3);
    expect(() => assertPlannerQaTaskRecord({
      ...task,
      relatedHandoffRecordVersion: 0,
    })).toThrowError(/relatedHandoffRecordVersion/);
    expect(() => assertPlannerQaTaskRecord({
      ...task,
      roleType: 'qa',
    })).toThrowError(/does not match roleType/);
  });

  it('validates planner/QA artifacts and ties them to current handoff versions', () => {
    const artifact = assertPlannerQaArtifactRecord({
      schemaVersion: 1,
      artifactId: 'artifact-1',
      artifactType: 'qa_verdict',
      producerRole: 'qa',
      taskId: 'task-2',
      version: 1,
      relatedHandoffId: 'handoff-2',
      relatedHandoffRecordVersion: 4,
      laneBinding: 'review-only',
      summary: 'Regression risk is medium',
      inlineBody: 'Review required before promotion.',
      createdAt: '2026-04-24T00:01:00.000Z',
      inputRefs: ['task-graph-1'],
      evidenceRefs: ['logs/regression.txt'],
      decisionMetadata: {
        confidence: 'medium',
        riskLevel: 'medium',
        recommendedNextAction: 'Request human QA review',
      },
      reviewState: 'under_review',
    });

    expect(artifact.artifactType).toBe('qa_verdict');
    expect(artifact.relatedHandoffRecordVersion).toBe(4);
    expect(() => assertPlannerQaArtifactRecord({
      ...artifact,
      producerRole: 'planner',
    })).toThrowError(/does not match roleType/);
  });

  it('encodes QA HITL low/medium/high behavior without creating a new QA lane', () => {
    expect(getQaHitlPolicyRule('low')).toMatchObject({
      leaderMayIngestEvidenceDirectly: true,
      humanQaReviewRequired: false,
      holdBehavior: 'none',
      canonicalStateAdvanceAllowed: true,
    });
    expect(getQaHitlPolicyRule('medium')).toMatchObject({
      humanQaReviewRequired: true,
      explicitHumanApprovalRequired: false,
      holdBehavior: 'review_required',
      canonicalStateAdvanceAllowed: false,
    });
    expect(getQaHitlPolicyRule('high')).toMatchObject({
      humanQaReviewRequired: true,
      explicitHumanApprovalRequired: true,
      holdBehavior: 'freeze_or_reconcile',
      canonicalStateAdvanceAllowed: false,
    });
  });
});
