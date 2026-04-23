import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { createTmuxAdapter } from '../../utils/tmux.js';
import { listTeamSessions } from '../../utils/team-orchestration.js';
import { resolveTeamProjectPath } from './helpers.js';

interface TeamListCommandOptions {
  path?: string;
}

export function createTeamListCommand(): Command {
  return new Command('list')
    .description('List saved team sessions for a Unity project')
    .argument('[path]', 'Unity project path (defaults to current directory)')
    .option('--path <path>', 'Unity project path (defaults to current directory)')
    .action((positionalPath: string | undefined, options: TeamListCommandOptions) => {
      try {
        const projectPath = resolveTeamProjectPath(positionalPath, options);
        const result = listTeamSessions(projectPath, createTmuxAdapter());

        ui.heading('Unity-MCP Team Sessions');
        ui.label('Project', projectPath);
        if (result.tmuxUnavailableMessage) {
          ui.warn(result.tmuxUnavailableMessage);
        }
        ui.divider();

        if (result.inspections.length === 0) {
          ui.info('No saved team sessions found.');
        } else {
          for (const inspection of result.inspections) {
            const paneSummary = inspection.state.roles.map(role => `${role.roleName}:${role.status}`).join(', ');
            ui.label(inspection.state.sessionId, `${inspection.state.status} — ${paneSummary}`);
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
