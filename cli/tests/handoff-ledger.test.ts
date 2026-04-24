import { describe, expect, it } from 'vitest';
import {
  applyApprovalIntent,
  applyEvidenceEnvelope,
  canTransitionHandoffState,
  createApprovalIntent,
  createEvidenceEnvelope,
  createHandoffRecord,
  createLeaderWriter,
  parseHandoffRecord,
  transitionHandoffState,
  type HandoffLifecycleWriter,
} from '../src/utils/handoff-ledger.js';

describe('handoff ledger single-writer lifecycle', () => {
  const leader = createLeaderWriter('mac-omx-leader');

  it('creates versioned leader-owned records with append-only audit history', () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'mac-omx',
      targetLane: 'windows-codex',
      requestedAction: 'plan-to-execution',
      createdBy: leader,
      createdAt: '2026-04-24T00:00:00.000Z',
    });

    const awaiting = transitionHandoffState(record, leader, 'awaiting_approval', {
      updatedAt: '2026-04-24T00:01:00.000Z',
    });

    expect(record.state).toBe('draft');
    expect(record.audit).toHaveLength(1);
    expect(awaiting.state).toBe('awaiting_approval');
    expect(awaiting.recordVersion).toBe(2);
    expect(awaiting.audit.map(entry => entry.sequence)).toEqual([1, 2]);
    expect(awaiting.audit[0]).toEqual(record.audit[0]);
  });

  it('rejects lifecycle mutation from non-leader writers', () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-2',
      sourceLane: 'mac-omx',
      targetLane: 'windows-codex',
      requestedAction: 'plan-to-execution',
      createdBy: leader,
    });
    const notLeader = { kind: 'windows-lane', actor: 'windows-codex' } as unknown as HandoffLifecycleWriter;

    expect(() => transitionHandoffState(record, notLeader, 'awaiting_approval'))
      .toThrowError(/Only the leader writer/);
  });

  it('treats execution evidence as an envelope until the leader applies it', () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-3',
      sourceLane: 'windows-codex',
      targetLane: 'mac-omx',
      requestedAction: 'execution-to-verification',
      createdBy: leader,
    });
    const envelope = createEvidenceEnvelope({
      handoffId: 'handoff-3',
      submittedBy: 'worker-win-1',
      sourceLane: 'windows-codex',
      evidenceRefs: ['logs/windows-validation.txt'],
      createdAt: '2026-04-24T00:02:00.000Z',
    });

    expect(record.evidenceRefs).toEqual([]);

    const applied = applyEvidenceEnvelope(record, leader, envelope, '2026-04-24T00:03:00.000Z');

    expect(applied.evidenceRefs).toEqual(['logs/windows-validation.txt']);
    expect(applied.recordVersion).toBe(2);
    expect(applied.audit.at(-1)?.action).toBe('evidence_applied');
  });

  it('applies approval intents only against the current leader-owned record version', () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-4',
      sourceLane: 'mac-omx',
      targetLane: 'github-actions',
      requestedAction: 'verification-to-cicd',
      createdBy: leader,
    });
    const awaiting = transitionHandoffState(record, leader, 'awaiting_approval');
    const staleIntent = createApprovalIntent({
      handoffId: 'handoff-4',
      recordVersion: 1,
      decision: 'approve',
      approverIdentity: 'U123',
      provider: 'slack',
    });

    expect(() => applyApprovalIntent(awaiting, leader, staleIntent)).toThrowError(/Stale approval intent/);

    const currentIntent = createApprovalIntent({
      handoffId: 'handoff-4',
      recordVersion: awaiting.recordVersion,
      decision: 'approve',
      approverIdentity: 'U123',
      provider: 'slack',
    });
    const approved = applyApprovalIntent(awaiting, leader, currentIntent, '2026-04-24T00:04:00.000Z');

    expect(approved.state).toBe('approved_not_dispatched');
    expect(approved.approverIdentity).toBe('U123');
    expect(approved.approvalVersion).toBe(1);
  });

  it('validates canonical transitions and persisted record shape', () => {
    expect(canTransitionHandoffState('draft', 'awaiting_approval')).toBe(true);
    expect(canTransitionHandoffState('completed', 'dispatched')).toBe(false);

    const record = createHandoffRecord({
      handoffId: 'handoff-5',
      sourceLane: 'mac-omx',
      targetLane: 'github-actions',
      requestedAction: 'verification-to-cicd',
      createdBy: leader,
    });
    const parsed = parseHandoffRecord(JSON.stringify(record), 'handoff-5.json');

    expect(parsed.handoffId).toBe('handoff-5');
    expect(parsed.state).toBe('draft');
    expect(parsed.audit).toHaveLength(1);
    expect(() => parseHandoffRecord('{"schemaVersion":1,"state":"draft"}', 'broken.json')).toThrowError(/recordVersion|handoffId|audit/);
  });
});
