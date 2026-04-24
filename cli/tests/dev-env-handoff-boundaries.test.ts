import { describe, expect, it } from 'vitest';
import {
  assertBotDispatchProvenance,
  assertLeaderLifecycleMutation,
  assertWindowsEvidenceEnvelope,
  assertWindowsHandoffSnapshot,
  canActorMutateHandoffLifecycleState,
  listForbiddenLedgerMutationFields,
  listForbiddenWindowsSnapshotFields,
  type BotDispatchProvenance,
  type WindowsEvidenceEnvelope,
  type WindowsHandoffSnapshot,
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

  it('accepts passive Windows handoff snapshots with descriptive metadata only', () => {
    const snapshot: WindowsHandoffSnapshot = {
      snapshotId: 'snapshot-1',
      handoffId: 'verification-handoff-1',
      handoffRecordVersion: 3,
      requestedAction: 'Run Windows validation and produce bounded evidence for leader reconcile.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:00:00.000Z',
      evidenceExpectations: [
        {
          evidenceType: 'log',
          description: 'Unity/worker log for the run.',
          required: true,
          exampleUri: 'file:///C:/unity-mcp-agent/logs/worker-1.log',
        },
        {
          evidenceType: 'test_report',
          description: 'Validation test report when available.',
        },
      ],
      projectHints: {
        projectPathHint: 'D:\\workSpace\\Unity-MCP\\Unity-MCP-Plugin',
        unityProjectPathHint: 'D:\\workSpace\\Unity-MCP\\Unity-MCP-Plugin',
        unityEditorVersionHint: '6000.3.6f1',
      },
      workspaceHints: {
        workingDirectoryHint: 'D:\\workSpace\\Unity-MCP',
        artifactDirectoryHint: 'C:\\unity-mcp-agent\\outbox',
        logDirectoryHint: 'C:\\unity-mcp-agent\\logs',
        branchHint: 'codex/tmux-team-orchestration',
      },
      notes: [
        'Reference-only snapshot; Unity-MCP does not ingest it directly.',
      ],
    };

    expect(assertWindowsHandoffSnapshot(snapshot)).toEqual(snapshot);
  });

  it('accepts passive Windows handoff snapshots without optional fields', () => {
    expect(assertWindowsHandoffSnapshot({
      snapshotId: 'snapshot-2',
      handoffId: 'verification-handoff-2',
      handoffRecordVersion: 1,
      requestedAction: 'Collect bounded Windows evidence only.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:05:00.000Z',
    })).toEqual({
      snapshotId: 'snapshot-2',
      handoffId: 'verification-handoff-2',
      handoffRecordVersion: 1,
      requestedAction: 'Collect bounded Windows evidence only.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:05:00.000Z',
      evidenceExpectations: undefined,
      projectHints: undefined,
      workspaceHints: undefined,
      notes: undefined,
    });
  });

  it('rejects passive Windows handoff snapshots with unsupported scheduler fields', () => {
    expect(() => assertWindowsHandoffSnapshot({
      snapshotId: 'snapshot-3',
      handoffId: 'verification-handoff-3',
      handoffRecordVersion: 2,
      requestedAction: 'Collect bounded Windows evidence only.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:10:00.000Z',
      pollingIntervalMs: 5000,
    })).toThrowError(/unsupported fields: pollingIntervalMs/);
  });

  it('rejects passive Windows handoff snapshots with forbidden nested process-control fields', () => {
    expect(() => assertWindowsHandoffSnapshot({
      snapshotId: 'snapshot-4',
      handoffId: 'verification-handoff-4',
      handoffRecordVersion: 2,
      requestedAction: 'Collect bounded Windows evidence only.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:12:00.000Z',
      workspaceHints: {
        workingDirectoryHint: 'D:\\workSpace\\Unity-MCP',
        dispatchTarget: 'worker-7',
      },
    })).toThrowError(/workspaceHints\.dispatchTarget/);
  });

  it('rejects passive Windows handoff snapshots with invalid evidence expectation metadata', () => {
    expect(() => assertWindowsHandoffSnapshot({
      snapshotId: 'snapshot-5',
      handoffId: 'verification-handoff-5',
      handoffRecordVersion: 2,
      requestedAction: 'Collect bounded Windows evidence only.',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      createdAt: '2026-04-24T06:15:00.000Z',
      evidenceExpectations: [
        {
          evidenceType: 'test_report',
          description: 'Validation report',
          required: 'yes',
        },
      ],
    })).toThrowError(/expected boolean/);
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

  it('identifies forbidden snapshot and lifecycle mutation fields before accepting lane-submitted records', () => {
    expect(listForbiddenLedgerMutationFields({
      state: 'dispatched',
      transition: 'approved_not_dispatched->dispatched',
      evidenceRefs: [],
    })).toEqual(['state', 'transition']);

    expect(listForbiddenWindowsSnapshotFields({
      workspaceHints: {
        branchHint: 'main',
        retryOwner: 'worker-1',
      },
      polling: true,
    })).toEqual(['workspaceHints.retryOwner', 'polling']);
  });
});
