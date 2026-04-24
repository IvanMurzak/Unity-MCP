import { describe, expect, it } from 'vitest';
import {
  assertBotDispatchProvenance,
  assertLeaderLifecycleMutation,
  assertWindowsEvidenceEnvelope,
  canActorMutateHandoffLifecycleState,
  listForbiddenLedgerMutationFields,
  type BotDispatchProvenance,
  type WindowsEvidenceEnvelope,
} from '../src/utils/dev-env-handoff-boundaries.js';

describe('dev environment handoff boundaries', () => {
  it('allows only the leader to mutate handoff lifecycle state', () => {
    expect(canActorMutateHandoffLifecycleState('leader')).toBe(true);
    expect(canActorMutateHandoffLifecycleState('windows_lane')).toBe(false);
    expect(canActorMutateHandoffLifecycleState('discord_adapter')).toBe(false);
    expect(canActorMutateHandoffLifecycleState('bot_bridge')).toBe(false);

    expect(assertLeaderLifecycleMutation({
      actorRole: 'leader',
      handoffId: 'handoff-1',
      handoffVersion: 1,
      fromState: 'awaiting_approval',
      toState: 'approved_not_dispatched',
      reason: 'approval intent validated by leader',
    }).toState).toBe('approved_not_dispatched');

    expect(() => assertLeaderLifecycleMutation({
      actorRole: 'windows_lane',
      handoffId: 'handoff-1',
      handoffVersion: 1,
      fromState: 'awaiting_approval',
      toState: 'approved_not_dispatched',
      reason: 'attempted lane-side promotion',
    })).toThrowError(/Only the leader may mutate/);
  });

  it('accepts Windows evidence envelopes without lifecycle mutation fields', () => {
    const envelope: WindowsEvidenceEnvelope = {
      schemaVersion: 1,
      kind: 'windows_lane_evidence_envelope',
      handoffId: 'handoff-2',
      handoffVersion: 3,
      sourceLane: {
        kind: 'windows_codex',
        laneId: 'win-codex-01',
      },
      submittedAt: '2026-04-24T05:00:00.000Z',
      outcome: 'passed',
      summary: 'Windows validation completed.',
      evidenceRefs: [
        {
          type: 'test_report',
          uri: 'file:///artifacts/windows-test-report.json',
          sha256: 'f'.repeat(64),
        },
      ],
    };

    expect(assertWindowsEvidenceEnvelope(envelope)).toEqual(envelope);
  });

  it('rejects Windows evidence envelopes that try to mutate lifecycle state', () => {
    expect(() => assertWindowsEvidenceEnvelope({
      schemaVersion: 1,
      kind: 'windows_lane_evidence_envelope',
      handoffId: 'handoff-2',
      handoffVersion: 3,
      sourceLane: {
        kind: 'windows_codex',
        laneId: 'win-codex-01',
      },
      submittedAt: '2026-04-24T05:00:00.000Z',
      outcome: 'passed',
      summary: 'Windows validation completed.',
      evidenceRefs: [{ type: 'log', uri: 'file:///artifacts/log.txt' }],
      lifecycleState: 'completed',
    })).toThrowError(/may not contain ledger lifecycle mutation fields: lifecycleState/);
  });

  it('accepts bot dispatch provenance without writing ledger state directly', () => {
    const provenance: BotDispatchProvenance = {
      schemaVersion: 1,
      kind: 'bot_dispatch_provenance',
      handoffId: 'handoff-3',
      handoffVersion: 2,
      dispatchId: 'dispatch-1',
      emittedAt: '2026-04-24T05:01:00.000Z',
      botId: 'discord-ci-bridge',
      target: {
        provider: 'github_actions',
        repository: 'IvanMurzak/Unity-MCP',
        eventType: 'unity_mcp_handoff_approved',
        workflowRef: 'test_pull_request_manual.yml',
      },
      result: 'accepted',
      externalRunId: '123456789',
    };

    expect(assertBotDispatchProvenance(provenance)).toEqual(provenance);
  });

  it('rejects bot dispatch provenance that includes direct ledger mutation fields', () => {
    expect(() => assertBotDispatchProvenance({
      schemaVersion: 1,
      kind: 'bot_dispatch_provenance',
      handoffId: 'handoff-3',
      handoffVersion: 2,
      dispatchId: 'dispatch-1',
      emittedAt: '2026-04-24T05:01:00.000Z',
      botId: 'discord-ci-bridge',
      target: {
        provider: 'github_actions',
        repository: 'IvanMurzak/Unity-MCP',
        eventType: 'unity_mcp_handoff_approved',
      },
      result: 'accepted',
      ledgerPatch: { state: 'dispatched' },
    })).toThrowError(/may not contain ledger lifecycle mutation fields: ledgerPatch/);
  });

  it('identifies lifecycle mutation fields before accepting lane-submitted records', () => {
    expect(listForbiddenLedgerMutationFields({
      state: 'dispatched',
      transition: 'approved_not_dispatched->dispatched',
      evidenceRefs: [],
    })).toEqual(['state', 'transition']);
  });
});
