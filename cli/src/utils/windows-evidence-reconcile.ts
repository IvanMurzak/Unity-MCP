import {
  applyEvidenceEnvelope,
  createEvidenceEnvelope,
  createLeaderWriter,
  readHandoffRecord,
  writeHandoffRecord,
} from './handoff-ledger.js';
import {
  formatWindowsEvidenceRefsForLedger,
  listQueuedWindowsEvidenceSpoolRecords,
  markQueuedWindowsEvidenceApplied,
  markQueuedWindowsEvidencePending,
  queueWindowsEvidenceEnvelope,
  type QueuedWindowsEvidenceSpoolRecord,
} from './windows-evidence-spool.js';

export interface ReconcileQueuedWindowsEvidenceOptions {
  projectPath: string;
  leaderActor: string;
  handoffId?: string;
}

export interface ReconcileQueuedWindowsEvidenceResult {
  totalQueued: number;
  applied: QueuedWindowsEvidenceSpoolRecord[];
  pending: Array<{
    record: QueuedWindowsEvidenceSpoolRecord;
    reason: string;
  }>;
}

export function submitQueuedWindowsEvidence(projectPath: string, input: unknown): {
  record: QueuedWindowsEvidenceSpoolRecord;
  filePath: string;
  duplicate: boolean;
} {
  return queueWindowsEvidenceEnvelope(projectPath, input);
}

export function reconcileQueuedWindowsEvidence(
  options: ReconcileQueuedWindowsEvidenceOptions,
): ReconcileQueuedWindowsEvidenceResult {
  const writer = createLeaderWriter(options.leaderActor);
  const queuedRecords = listQueuedWindowsEvidenceSpoolRecords(options.projectPath)
    .filter(record => record.consumedAt === null)
    .filter(record => !options.handoffId || record.handoffId === options.handoffId);

  const applied: QueuedWindowsEvidenceSpoolRecord[] = [];
  const pending: Array<{ record: QueuedWindowsEvidenceSpoolRecord; reason: string }> = [];

  for (const queuedRecord of queuedRecords) {
    try {
      const handoff = readHandoffRecord(options.projectPath, queuedRecord.handoffId);
      if (handoff.recordVersion < queuedRecord.handoffVersion) {
        throw new Error(
          `Handoff ${handoff.handoffId} is behind the queued evidence version (${handoff.recordVersion} < ${queuedRecord.handoffVersion}).`,
        );
      }

      const nextRecord = applyEvidenceEnvelope(handoff, writer, createEvidenceEnvelope({
        handoffId: queuedRecord.handoffId,
        submittedBy: `windows:${queuedRecord.sourceLaneId}`,
        sourceLane: 'windows-codex',
        evidenceRefs: formatWindowsEvidenceRefsForLedger(queuedRecord.envelope),
        createdAt: queuedRecord.submittedAt,
      }));
      writeHandoffRecord(options.projectPath, writer, nextRecord);

      applied.push(markQueuedWindowsEvidenceApplied({
        projectPath: options.projectPath,
        recordId: queuedRecord.recordId,
        appliedRecordVersion: nextRecord.recordVersion,
      }));
    } catch (err) {
      const reason = err instanceof Error ? err.message : String(err);
      pending.push({
        record: markQueuedWindowsEvidencePending({
          projectPath: options.projectPath,
          recordId: queuedRecord.recordId,
          lastError: reason,
        }),
        reason,
      });
    }
  }

  return {
    totalQueued: queuedRecords.length,
    applied,
    pending,
  };
}
