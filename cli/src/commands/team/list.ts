import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { createTeamRuntime } from '../../utils/team-runtime.js';
import { listTeamSessions } from '../../utils/team-orchestration.js';
import { resolveTeamProjectPath } from './helpers.js';

interface TeamListCommandOptions {
  path?: string;
}

export function createTeamListCommand(): Command {
  return new Command('list')
    .description('List saved Unity project sessions')
    .argument('[path]', 'Unity project path (defaults to current directory)')
    .option('--path <path>', 'Unity project path (defaults to current directory)')
    .action((positionalPath: string | undefined, options: TeamListCommandOptions) => {
      try {
        const projectPath = resolveTeamProjectPath(positionalPath, options);
        const result = listTeamSessions(projectPath, createTeamRuntime());

        ui.heading('Unity-MCP Project Sessions');
        ui.label('Project', projectPath);
        if (result.runtimeUnavailableMessage) {
          ui.warn(result.runtimeUnavailableMessage);
        }
        ui.divider();

        if (result.inspections.length === 0) {
          ui.info('No saved project sessions found.');
        } else {
          for (const inspection of result.inspections) {
            const roleSummary = inspection.state.roles.map(role => `${role.roleName}:${role.status}`).join(', ');
            ui.label(
              inspection.state.sessionId,
              `${inspection.state.status} — ${inspection.state.runtime.kind}:${inspection.state.runtime.sessionHandle} — ${roleSummary}`,
            );
          }
        }

        if (result.invalid.length > 0) {
          ui.divider();
          for (const failure of result.invalid) {
            ui.warn(`Skipped invalid state file ${failure.filePath}: ${failure.error}`);
          }
        }
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
