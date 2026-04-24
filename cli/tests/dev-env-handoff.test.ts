import { describe, expect, it } from 'vitest';
import {
  DEV_ENV_CONTROL_PLANE_LANE_ID,
  DEV_ENV_LANE_DEFINITIONS,
  assertDevEnvExecutionLaneEvidenceOnly,
  assertDevEnvHandoffTransition,
  assertDevEnvLifecycleMutationAllowed,
  canTransitionDevEnvHandoff,
  getDevEnvLifecycleMutator,
  getDevEnvOutageState,
  isDevEnvControlPlane,
  isDevEnvExecutionValidationLane,
  reconcileDevEnvQueuedEvidence,
  type DevEnvEvidenceEnvelope,
  type DevEnvHandoffRecord,
} from '../src/utils/dev-env-handoff.js';

describe('dev-env v1 control plane contract', () => {
  it('treats mac + OMX as the only lane allowed to mutate lifecycle state', () => {
    const mutator = getDevEnvLifecycleMutator();

    expect(mutator.id).toBe(DEV_ENV_CONTROL_PLANE_LANE_ID);
    expect(mutator.displayName).toBe('mac + OMX leader');
    expect(mutator.role).toBe('control-plane');
    expect(mutator.canMutateLifecycleState).toBe(true);
  });

  it('rejects lifecycle mutation attempts from non-leader lanes', () => {
    expect(() => assertDevEnvLifecycleMutationAllowed('windows-codex')).toThrowError(/may submit evidence or intents only/);
    expect(() => assertDevEnvLifecycleMutationAllowed('chat-approval-hub')).toThrowError(/may submit evidence or intents only/);
    expect(() => assertDevEnvLifecycleMutationAllowed('bot-ci-bridge')).toThrowError(/may submit evidence or intents only/);
    expect(() => assertDevEnvLifecycleMutationAllowed(DEV_ENV_CONTROL_PLANE_LANE_ID)).not.toThrow();
  });

  it('keeps every non-leader lane out of the control plane', () => {
    const nonLeaderLanes = DEV_ENV_LANE_DEFINITIONS.filter(lane => lane.id !== DEV_ENV_CONTROL_PLANE_LANE_ID);

    expect(nonLeaderLanes.length).toBeGreaterThan(0);
    expect(nonLeaderLanes.every(lane => !lane.canMutateLifecycleState)).toBe(true);
    expect(nonLeaderLanes.every(lane => !isDevEnvControlPlane(lane.id))).toBe(true);
  });

  it('treats Windows Codex as evidence-only execution and validation, not leadership', () => {
    expect(isDevEnvExecutionValidationLane('windows-codex')).toBe(true);
    expect(isDevEnvControlPlane('windows-codex')).toBe(false);
    expect(() => assertDevEnvExecutionLaneEvidenceOnly('windows-codex')).not.toThrow();
    expect(() => assertDevEnvExecutionLaneEvidenceOnly(DEV_ENV_CONTROL_PLANE_LANE_ID)).toThrowError(/not a v1 execution\/validation lane/);
  });
});

describe('dev-env v1 ledger and outage contract', () => {
  it('requires the leader for every valid state transition', () => {
    expect(canTransitionDevEnvHandoff('draft', 'awaiting_approval')).toBe(true);
    expect(canTransitionDevEnvHandoff('approved_not_dispatched', 'dispatched')).toBe(true);
    expect(canTransitionDevEnvHandoff('completed', 'dispatched')).toBe(false);

    expect(() => assertDevEnvHandoffTransition({
      actorLane: DEV_ENV_CONTROL_PLANE_LANE_ID,
      from: 'draft',
      to: 'awaiting_approval',
    })).not.toThrow();

    expect(() => assertDevEnvHandoffTransition({
      actorLane: 'windows-codex',
      from: 'draft',
      to: 'awaiting_approval',
    })).toThrowError(/Only mac \+ OMX leader/);
  });

  it('freezes approval and approved-not-dispatched handoffs during leader outage', () => {
    expect(getDevEnvOutageState('awaiting_approval')).toBe('frozen');
    expect(getDevEnvOutageState('approved_not_dispatched')).toBe('frozen');
    expect(getDevEnvOutageState('dispatched')).toBe('reconcile_needed');
    expect(getDevEnvOutageState('completed')).toBe('completed');
  });

  it('reconciles queued Windows evidence once and rejects stale replays', () => {
    const record: DevEnvHandoffRecord = {
      id: 'handoff-1',
      type: 'execution_to_verification',
      state: 'frozen',
      recordVersion: 3,
      sourceLane: 'windows-codex',
      targetLane: DEV_ENV_CONTROL_PLANE_LANE_ID,
      evidenceRefs: ['log://existing'],
      appliedEvidenceIds: ['evidence-0'],
    };
    const envelopes: DevEnvEvidenceEnvelope[] = [
      {
        id: 'evidence-1',
        laneId: 'windows-codex',
        kind: 'execution_result',
        handoffId: 'handoff-1',
        recordVersion: 3,
        createdAt: '2026-04-24T00:00:00.000Z',
        refs: ['log://windows-run'],
      },
      {
        id: 'evidence-1',
        laneId: 'windows-codex',
        kind: 'execution_result',
        handoffId: 'handoff-1',
        recordVersion: 3,
        createdAt: '2026-04-24T00:00:01.000Z',
        refs: ['log://windows-run-duplicate'],
      },
      {
        id: 'evidence-stale-version',
        laneId: 'windows-codex',
        kind: 'execution_result',
        handoffId: 'handoff-1',
        recordVersion: 2,
        createdAt: '2026-04-24T00:00:02.000Z',
        refs: ['log://stale'],
      },
    ];

    const result = reconcileDevEnvQueuedEvidence(record, envelopes);

    expect(result.state).toBe('reconcile_needed');
    expect(result.evidenceRefs).toEqual(['log://existing', 'log://windows-run']);
    expect(result.appliedEvidenceIds).toEqual(['evidence-0', 'evidence-1']);
    expect(result.ignoredEvidenceIds).toEqual(['evidence-1', 'evidence-stale-version']);
  });
});
