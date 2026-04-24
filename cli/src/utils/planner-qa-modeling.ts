export const PLANNER_QA_MODELING_SCHEMA_VERSION = 1;

export const PLANNER_QA_ROLE_TYPES = ['planner', 'qa'] as const;
export const PLANNER_TASK_TYPES = [
  'prd_author',
  'plan_author',
  'task_graph_decompose',
  'replan_from_feedback',
] as const;
export const QA_TASK_TYPES = [
  'verify_change',
  'triage_bug',
  'regression_check',
  'release_readiness_review',
] as const;
export const PLANNER_QA_TASK_TYPES = [...PLANNER_TASK_TYPES, ...QA_TASK_TYPES] as const;

export const PLANNER_ARTIFACT_TYPES = ['prd_brief', 'plan_spec', 'task_graph'] as const;
export const QA_ARTIFACT_TYPES = ['qa_verdict', 'bug_triage', 'regression_report', 'readiness_assessment'] as const;
export const PLANNER_QA_ARTIFACT_TYPES = [...PLANNER_ARTIFACT_TYPES, ...QA_ARTIFACT_TYPES] as const;

export const PLANNER_QA_RISK_LEVELS = ['low', 'medium', 'high'] as const;
export const PLANNER_QA_REVIEW_POLICIES = ['none', 'qa-review', 'human-approval'] as const;
export const PLANNER_QA_LANE_BINDINGS = ['leader-coordinated', 'windows-execution-supported', 'review-only'] as const;
export const PLANNER_QA_ESCALATION_POLICIES = ['stale', 'blocked', 'risk_increase', 'scope_breach'] as const;
export const PLANNER_QA_TASK_STATUSES = ['pending', 'in_progress', 'completed', 'blocked', 'cancelled'] as const;
export const PLANNER_QA_ARTIFACT_REVIEW_STATES = ['draft', 'under_review', 'approved', 'rejected', 'superseded'] as const;
export const PLANNER_QA_CONFIDENCE_LEVELS = ['low', 'medium', 'high'] as const;

export type PlannerQaRoleType = typeof PLANNER_QA_ROLE_TYPES[number];
export type PlannerTaskType = typeof PLANNER_TASK_TYPES[number];
export type QaTaskType = typeof QA_TASK_TYPES[number];
export type PlannerQaTaskType = typeof PLANNER_QA_TASK_TYPES[number];
export type PlannerArtifactType = typeof PLANNER_ARTIFACT_TYPES[number];
export type QaArtifactType = typeof QA_ARTIFACT_TYPES[number];
export type PlannerQaArtifactType = typeof PLANNER_QA_ARTIFACT_TYPES[number];
export type PlannerQaRiskLevel = typeof PLANNER_QA_RISK_LEVELS[number];
export type PlannerQaReviewPolicy = typeof PLANNER_QA_REVIEW_POLICIES[number];
export type PlannerQaLaneBinding = typeof PLANNER_QA_LANE_BINDINGS[number];
export type PlannerQaEscalationPolicy = typeof PLANNER_QA_ESCALATION_POLICIES[number];
export type PlannerQaTaskStatus = typeof PLANNER_QA_TASK_STATUSES[number];
export type PlannerQaArtifactReviewState = typeof PLANNER_QA_ARTIFACT_REVIEW_STATES[number];
export type PlannerQaConfidenceLevel = typeof PLANNER_QA_CONFIDENCE_LEVELS[number];

export interface PlannerQaRoleRegistryEntry {
  schemaVersion: number;
  roleId: string;
  roleType: PlannerQaRoleType;
  version: number;
  status: 'active';
  responsibilities: readonly string[];
  allowedOutputs: readonly PlannerQaArtifactType[];
  constraints: readonly string[];
  handoffTargets: readonly string[];
  operatesWithinLeaderOwnedLanes: true;
  canMutateCanonicalState: false;
}

export interface PlannerQaTaskRecord {
  schemaVersion: number;
  taskId: string;
  roleType: PlannerQaRoleType;
  taskType: PlannerQaTaskType;
  status: PlannerQaTaskStatus;
  priority: number;
  riskLevel: PlannerQaRiskLevel;
  relatedHandoffId: string;
  relatedHandoffRecordVersion: number;
  laneBinding: PlannerQaLaneBinding;
  requestedBy: string;
  ownedBy: string;
  createdAt: string;
  dueAt?: string;
  inputs: string[];
  acceptanceCriteria: string[];
  dependsOn: string[];
  produces: PlannerQaArtifactType[];
  reviewPolicy: PlannerQaReviewPolicy;
  escalationPolicy: PlannerQaEscalationPolicy;
}

export interface PlannerQaArtifactRecord {
  schemaVersion: number;
  artifactId: string;
  artifactType: PlannerQaArtifactType;
  producerRole: PlannerQaRoleType;
  taskId: string;
  version: number;
  relatedHandoffId: string;
  relatedHandoffRecordVersion: number;
  laneBinding: PlannerQaLaneBinding;
  summary: string;
  inlineBody?: string;
  bodyRef?: string;
  createdAt: string;
  inputRefs: string[];
  evidenceRefs: string[];
  decisionMetadata: {
    confidence: PlannerQaConfidenceLevel;
    riskLevel: PlannerQaRiskLevel;
    recommendedNextAction: string;
  };
  reviewState: PlannerQaArtifactReviewState;
}

export interface QaHitlPolicyRule {
  riskLevel: PlannerQaRiskLevel;
  leaderMayIngestEvidenceDirectly: boolean;
  humanQaReviewRequired: boolean;
  explicitHumanApprovalRequired: boolean;
  blocksPromotionUntilHumanReview: boolean;
  holdBehavior: 'none' | 'review_required' | 'freeze_or_reconcile';
  canonicalStateAdvanceAllowed: boolean;
  summary: string;
}

export class PlannerQaModelingError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'PlannerQaModelingError';
  }
}

export const PLANNER_QA_ROLE_REGISTRY: readonly PlannerQaRoleRegistryEntry[] = [
  {
    schemaVersion: PLANNER_QA_MODELING_SCHEMA_VERSION,
    roleId: 'planner',
    roleType: 'planner',
    version: 1,
    status: 'active',
    responsibilities: [
      'Author PRD and plan artifacts for leader review',
      'Decompose scoped work into a bounded task graph',
      'Frame dependency and risk concerns for existing approval gates',
    ],
    allowedOutputs: ['prd_brief', 'plan_spec', 'task_graph'],
    constraints: [
      'Must not mutate canonical lifecycle state',
      'Must not dispatch CI/CD or control Discord directly',
      'Must reference relatedHandoffRecordVersion when an artifact can affect promotion readiness',
    ],
    handoffTargets: ['leader', 'qa'],
    operatesWithinLeaderOwnedLanes: true,
    canMutateCanonicalState: false,
  },
  {
    schemaVersion: PLANNER_QA_MODELING_SCHEMA_VERSION,
    roleId: 'qa',
    roleType: 'qa',
    version: 1,
    status: 'active',
    responsibilities: [
      'Publish verification verdicts and regression findings',
      'Triage bugs and frame release readiness risk',
      'Escalate medium/high-risk outcomes through the leader-owned gate model',
    ],
    allowedOutputs: ['qa_verdict', 'bug_triage', 'regression_report', 'readiness_assessment'],
    constraints: [
      'Must not mutate canonical lifecycle state',
      'Must not approve or dispatch directly',
      'Must use risk-based HITL semantics for medium/high-risk findings',
    ],
    handoffTargets: ['leader', 'planner', 'approval-surface-via-leader'],
    operatesWithinLeaderOwnedLanes: true,
    canMutateCanonicalState: false,
  },
] as const;

export const QA_HITL_POLICY: Record<PlannerQaRiskLevel, QaHitlPolicyRule> = {
  low: {
    riskLevel: 'low',
    leaderMayIngestEvidenceDirectly: true,
    humanQaReviewRequired: false,
    explicitHumanApprovalRequired: false,
    blocksPromotionUntilHumanReview: false,
    holdBehavior: 'none',
    canonicalStateAdvanceAllowed: true,
    summary: 'Routine verification only; no new approval gate is created.',
  },
  medium: {
    riskLevel: 'medium',
    leaderMayIngestEvidenceDirectly: false,
    humanQaReviewRequired: true,
    explicitHumanApprovalRequired: false,
    blocksPromotionUntilHumanReview: true,
    holdBehavior: 'review_required',
    canonicalStateAdvanceAllowed: false,
    summary: 'Human QA review is required before promotion readiness may be asserted.',
  },
  high: {
    riskLevel: 'high',
    leaderMayIngestEvidenceDirectly: false,
    humanQaReviewRequired: true,
    explicitHumanApprovalRequired: true,
    blocksPromotionUntilHumanReview: true,
    holdBehavior: 'freeze_or_reconcile',
    canonicalStateAdvanceAllowed: false,
    summary: 'Leader must freeze or reconcile and require explicit human QA approval before unfreeze/promotion.',
  },
};

export function getPlannerQaRoleRegistry(): readonly PlannerQaRoleRegistryEntry[] {
  return PLANNER_QA_ROLE_REGISTRY;
}

export function getQaHitlPolicyRule(riskLevel: PlannerQaRiskLevel): QaHitlPolicyRule {
  return QA_HITL_POLICY[riskLevel];
}

export function assertPlannerQaTaskRecord(input: unknown): PlannerQaTaskRecord {
  const record = assertRecord(input, 'Planner/QA task');
  const roleType = assertEnum(record.roleType, PLANNER_QA_ROLE_TYPES, 'roleType');
  const taskType = assertEnum(record.taskType, PLANNER_QA_TASK_TYPES, 'taskType');
  assertRoleTypeMatchesTaskType(roleType, taskType);

  return {
    schemaVersion: assertSchemaVersion(record.schemaVersion, 'task.schemaVersion'),
    taskId: assertNonEmptyString(record.taskId, 'taskId'),
    roleType,
    taskType,
    status: assertEnum(record.status, PLANNER_QA_TASK_STATUSES, 'status'),
    priority: assertPositiveInteger(record.priority, 'priority'),
    riskLevel: assertEnum(record.riskLevel, PLANNER_QA_RISK_LEVELS, 'riskLevel'),
    relatedHandoffId: assertNonEmptyString(record.relatedHandoffId, 'relatedHandoffId'),
    relatedHandoffRecordVersion: assertPositiveInteger(record.relatedHandoffRecordVersion, 'relatedHandoffRecordVersion'),
    laneBinding: assertEnum(record.laneBinding, PLANNER_QA_LANE_BINDINGS, 'laneBinding'),
    requestedBy: assertNonEmptyString(record.requestedBy, 'requestedBy'),
    ownedBy: assertNonEmptyString(record.ownedBy, 'ownedBy'),
    createdAt: assertNonEmptyString(record.createdAt, 'createdAt'),
    ...(record.dueAt !== undefined ? { dueAt: assertNonEmptyString(record.dueAt, 'dueAt') } : {}),
    inputs: assertStringArray(record.inputs, 'inputs'),
    acceptanceCriteria: assertStringArray(record.acceptanceCriteria, 'acceptanceCriteria'),
    dependsOn: assertStringArray(record.dependsOn, 'dependsOn'),
    produces: assertArtifactTypeArray(record.produces, 'produces', roleType),
    reviewPolicy: assertEnum(record.reviewPolicy, PLANNER_QA_REVIEW_POLICIES, 'reviewPolicy'),
    escalationPolicy: assertEnum(record.escalationPolicy, PLANNER_QA_ESCALATION_POLICIES, 'escalationPolicy'),
  };
}

export function assertPlannerQaArtifactRecord(input: unknown): PlannerQaArtifactRecord {
  const record = assertRecord(input, 'Planner/QA artifact');
  const producerRole = assertEnum(record.producerRole, PLANNER_QA_ROLE_TYPES, 'producerRole');
  const artifactType = assertEnum(record.artifactType, PLANNER_QA_ARTIFACT_TYPES, 'artifactType');
  assertRoleTypeMatchesArtifactType(producerRole, artifactType);

  const inlineBody = record.inlineBody === undefined ? undefined : assertNonEmptyString(record.inlineBody, 'inlineBody');
  const bodyRef = record.bodyRef === undefined ? undefined : assertNonEmptyString(record.bodyRef, 'bodyRef');
  if (!inlineBody && !bodyRef) {
    throw new PlannerQaModelingError('Planner/QA artifact must include either inlineBody or bodyRef.');
  }

  const decisionMetadataRecord = assertRecord(record.decisionMetadata, 'decisionMetadata');

  return {
    schemaVersion: assertSchemaVersion(record.schemaVersion, 'artifact.schemaVersion'),
    artifactId: assertNonEmptyString(record.artifactId, 'artifactId'),
    artifactType,
    producerRole,
    taskId: assertNonEmptyString(record.taskId, 'taskId'),
    version: assertPositiveInteger(record.version, 'version'),
    relatedHandoffId: assertNonEmptyString(record.relatedHandoffId, 'relatedHandoffId'),
    relatedHandoffRecordVersion: assertPositiveInteger(record.relatedHandoffRecordVersion, 'relatedHandoffRecordVersion'),
    laneBinding: assertEnum(record.laneBinding, PLANNER_QA_LANE_BINDINGS, 'laneBinding'),
    summary: assertNonEmptyString(record.summary, 'summary'),
    ...(inlineBody ? { inlineBody } : {}),
    ...(bodyRef ? { bodyRef } : {}),
    createdAt: assertNonEmptyString(record.createdAt, 'createdAt'),
    inputRefs: assertStringArray(record.inputRefs, 'inputRefs'),
    evidenceRefs: assertStringArray(record.evidenceRefs, 'evidenceRefs'),
    decisionMetadata: {
      confidence: assertEnum(decisionMetadataRecord.confidence, PLANNER_QA_CONFIDENCE_LEVELS, 'decisionMetadata.confidence'),
      riskLevel: assertEnum(decisionMetadataRecord.riskLevel, PLANNER_QA_RISK_LEVELS, 'decisionMetadata.riskLevel'),
      recommendedNextAction: assertNonEmptyString(decisionMetadataRecord.recommendedNextAction, 'decisionMetadata.recommendedNextAction'),
    },
    reviewState: assertEnum(record.reviewState, PLANNER_QA_ARTIFACT_REVIEW_STATES, 'reviewState'),
  };
}

function assertSchemaVersion(value: unknown, field: string): number {
  if (value !== PLANNER_QA_MODELING_SCHEMA_VERSION) {
    throw new PlannerQaModelingError(`Unsupported ${field}`);
  }
  return PLANNER_QA_MODELING_SCHEMA_VERSION;
}

function assertRecord(value: unknown, field: string): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    throw new PlannerQaModelingError(`Invalid ${field}; expected object`);
  }
  return value as Record<string, unknown>;
}

function assertNonEmptyString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new PlannerQaModelingError(`Invalid or missing ${field}`);
  }
  return value;
}

function assertPositiveInteger(value: unknown, field: string): number {
  if (!Number.isInteger(value) || Number(value) < 1) {
    throw new PlannerQaModelingError(`Invalid ${field}`);
  }
  return Number(value);
}

function assertStringArray(value: unknown, field: string): string[] {
  if (!Array.isArray(value) || value.some(item => typeof item !== 'string' || item.trim().length === 0)) {
    throw new PlannerQaModelingError(`Invalid ${field}; expected string[]`);
  }
  return [...value] as string[];
}

function assertEnum<T extends readonly string[]>(value: unknown, allowed: T, field: string): T[number] {
  if (typeof value !== 'string' || !(allowed as readonly string[]).includes(value)) {
    throw new PlannerQaModelingError(`Invalid ${field}`);
  }
  return value as T[number];
}

function assertArtifactTypeArray(value: unknown, field: string, roleType: PlannerQaRoleType): PlannerQaArtifactType[] {
  if (!Array.isArray(value) || value.length === 0) {
    throw new PlannerQaModelingError(`Invalid ${field}; expected non-empty array`);
  }

  return value.map((entry, index) => {
    const artifactType = assertEnum(entry, PLANNER_QA_ARTIFACT_TYPES, `${field}[${index}]`);
    assertRoleTypeMatchesArtifactType(roleType, artifactType);
    return artifactType;
  });
}

function assertRoleTypeMatchesTaskType(roleType: PlannerQaRoleType, taskType: PlannerQaTaskType): void {
  const plannerTask = (PLANNER_TASK_TYPES as readonly string[]).includes(taskType);
  if ((roleType === 'planner') !== plannerTask) {
    throw new PlannerQaModelingError(`Task type ${taskType} does not match roleType ${roleType}.`);
  }
}

function assertRoleTypeMatchesArtifactType(roleType: PlannerQaRoleType, artifactType: PlannerQaArtifactType): void {
  const plannerArtifact = (PLANNER_ARTIFACT_TYPES as readonly string[]).includes(artifactType);
  if ((roleType === 'planner') !== plannerArtifact) {
    throw new PlannerQaModelingError(`Artifact type ${artifactType} does not match roleType ${roleType}.`);
  }
}
