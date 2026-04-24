import { describe, expect, it } from 'vitest';
import {
  DEV_ENV_CONTROL_PLANE_LANE_ID,
  DEV_ENV_LANE_DEFINITIONS,
  assertDevEnvLifecycleMutationAllowed,
  getDevEnvLifecycleMutator,
  isDevEnvControlPlane,
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
});
