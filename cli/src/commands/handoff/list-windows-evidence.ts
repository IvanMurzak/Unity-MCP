import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { listQueuedWindowsEvidenceSpoolRecords } from '../../utils/windows-evidence-spool.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffListWindowsEvidenceOptions {
  path?: string;
  handoffId?: string;
}

export function createHandoffListWindowsEvidenceCommand(): Command {
  return new Command('list-windows-evidence')
    .description('List queued Windows evidence spool records and their reconcile status')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when not passed positionally')
    .option('--handoff-id <handoffId>', 'Only show records for one handoff id')
    .action((positionalPath: string | undefined, options: HandoffListWindowsEvidenceOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const records = listQueuedWindowsEvidenceSpoolRecords(projectPath)
          .filter(record => !options.handoffId || record.handoffId === options.handoffId);

        ui.heading('Unity-MCP Windows Evidence Queue');
        ui.label('Project', projectPath);
        ui.label('Records', String(records.length));
        if (options.handoffId) {
          ui.label('Handoff filter', options.handoffId);
        }
        ui.divider();

        if (records.length === 0) {
          ui.info('No queued Windows evidence records found.');
          return;
        }

        for (const record of records) {
          const status = record.consumedAt
            ? `applied@${record.appliedRecordVersion ?? '?'}`
            : record.lastError
              ? `pending-error:${record.lastError}`
              : 'pending';
          ui.info(`${record.handoffId}@${record.handoffVersion} — ${record.sourceLaneId} — ${record.outcome} — ${status}`);
        }
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
