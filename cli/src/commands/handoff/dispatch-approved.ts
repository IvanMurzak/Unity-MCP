import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { loadHandoffBridgeConfig } from '../../utils/discord-approval.js';
import { dispatchApprovedVerificationHandoff } from '../../utils/github-dispatch.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffDispatchApprovedOptions {
  path?: string;
  envFile?: string;
}

export function createHandoffDispatchApprovedCommand(): Command {
  return new Command('dispatch-approved')
    .description('Dispatch an approved verification handoff to GitHub Actions via repository_dispatch')
    .argument('<handoff-id>', 'Leader-owned handoff id to dispatch')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when handoff id is passed as the first positional argument')
    .option('--env-file <path>', 'Optional env file containing UNITY_MCP_HANDOFF_* secrets')
    .action(async (handoffId: string, positionalPath: string | undefined, options: HandoffDispatchApprovedOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const bridgeConfig = loadHandoffBridgeConfig({ envFilePath: options.envFile });
        const dispatch = await dispatchApprovedVerificationHandoff({
          projectPath,
          handoffId,
          bridgeConfig,
        });

        ui.heading('Unity-MCP Handoff GitHub Dispatch');
        ui.label('Project', projectPath);
        ui.label('Handoff', handoffId);
        ui.label('Event', dispatch.eventType);
        ui.label('Target', dispatch.target);
        ui.label('Dispatch id', dispatch.dispatchId);
        ui.divider();
        ui.success(`repository_dispatch emitted for handoff ${handoffId}.`);
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
