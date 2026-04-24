import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { reconcileQueuedWindowsEvidence } from '../../utils/windows-evidence-reconcile.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffReconcileWindowsEvidenceOptions {
  path?: string;
  handoffId?: string;
  leaderActor?: string;
}

export function createHandoffReconcileWindowsEvidenceCommand(): Command {
  return new Command('reconcile-windows-evidence')
    .description('Apply queued Windows Codex evidence envelopes to leader-owned handoff records')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when not passed positionally')
    .option('--handoff-id <handoffId>', 'Reconcile only one handoff id')
    .option('--leader-actor <actor>', 'Leader actor label to write into the audit trail', process.env.UNITY_MCP_HANDOFF_LEADER_ACTOR ?? 'mac-omx-leader')
    .action((positionalPath: string | undefined, options: HandoffReconcileWindowsEvidenceOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const result = reconcileQueuedWindowsEvidence({
          projectPath,
          leaderActor: options.leaderActor ?? process.env.UNITY_MCP_HANDOFF_LEADER_ACTOR ?? 'mac-omx-leader',
          ...(options.handoffId ? { handoffId: options.handoffId } : {}),
        });

        ui.heading('Unity-MCP Windows Evidence Reconcile');
        ui.label('Project', projectPath);
        ui.label('Queued records', String(result.totalQueued));
        ui.label('Applied', String(result.applied.length));
        ui.label('Pending', String(result.pending.length));
        if (options.handoffId) {
          ui.label('Handoff filter', options.handoffId);
        }
        ui.divider();

        for (const applied of result.applied) {
          ui.info(`APPLIED ${applied.handoffId}@${applied.handoffVersion} -> recordVersion ${applied.appliedRecordVersion}`);
        }
        for (const pending of result.pending) {
          ui.warn(`PENDING ${pending.record.handoffId}@${pending.record.handoffVersion}: ${pending.reason}`);
        }

        if (result.pending.length > 0) {
          ui.warn('Some queued Windows evidence remains pending for a later reconcile pass.');
        }
        ui.success(`Reconcile processed ${result.totalQueued} queued Windows evidence record(s).`);
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
