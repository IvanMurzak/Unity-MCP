import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { parseInput } from '../../utils/input.js';
import { submitQueuedWindowsEvidence } from '../../utils/windows-evidence-reconcile.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffSubmitWindowsEvidenceOptions {
  path?: string;
  input?: string;
  inputFile?: string;
}

export function createHandoffSubmitWindowsEvidenceCommand(): Command {
  return new Command('submit-windows-evidence')
    .description('Queue a bounded Windows Codex evidence envelope for later leader reconcile')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when not passed positionally')
    .option('--input <json>', 'Inline JSON Windows evidence envelope')
    .option('--input-file <path>', 'Path to a JSON Windows evidence envelope file')
    .action((positionalPath: string | undefined, options: HandoffSubmitWindowsEvidenceOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const raw = parseInput(options);
        const parsed = JSON.parse(raw) as unknown;
        const queued = submitQueuedWindowsEvidence(projectPath, parsed);

        ui.heading('Unity-MCP Windows Evidence Submit');
        ui.label('Project', projectPath);
        ui.label('Handoff', queued.record.handoffId);
        ui.label('Version', String(queued.record.handoffVersion));
        ui.label('Lane', queued.record.sourceLaneId);
        ui.label('Outcome', queued.record.outcome);
        ui.label('Spool file', queued.filePath);
        ui.divider();
        if (queued.duplicate) {
          ui.success(`Windows evidence was already queued for handoff ${queued.record.handoffId}.`);
        } else {
          ui.success(`Queued Windows evidence for handoff ${queued.record.handoffId}.`);
        }
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
