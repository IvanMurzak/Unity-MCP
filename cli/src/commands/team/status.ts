import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { createTmuxAdapter } from '../../utils/tmux.js';
import { getTeamSessionStatus } from '../../utils/team-orchestration.js';
import { resolveTeamProjectAndSession } from './helpers.js';

interface TeamStatusCommandOptions {
  path?: string;
}

export function createTeamStatusCommand(): Command {
  return new Command('status')
    .description('Show saved and live tmux status for a local team session')
    .argument('[session-or-path]', 'Session id/name or Unity project path (defaults to latest session in cwd project)')
    .option('--path <path>', 'Unity project path when looking up a specific session id/name')
    .action((target: string | undefined, options: TeamStatusCommandOptions) => {
      try {
        const { projectPath, sessionRef } = resolveTeamProjectAndSession(target, options);
        const inspection = getTeamSessionStatus(projectPath, createTmuxAdapter(), sessionRef);

        ui.heading('Unity-MCP Team Status');
        ui.label('Project', inspection.state.projectPath);
        ui.label('Session', inspection.state.sessionId);
        ui.label('tmux', inspection.state.tmuxSessionName);
        ui.label('Status', inspection.state.status);
        ui.divider();
        for (const role of inspection.state.roles) {
          ui.label(role.roleName, `${role.status} — ${role.paneId || 'unassigned'} (${role.paneTitle})`);
        }
        if (inspection.issues.length > 0) {
          ui.divider();
          for (const issue of inspection.issues) {
            ui.warn(issue);
          }
          process.exit(1);
        }
        ui.divider();
        ui.success('Persisted state matches the live tmux session.');
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
