import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { createTmuxAdapter } from '../../utils/tmux.js';
import { stopTeamSession } from '../../utils/team-orchestration.js';
import { resolveTeamProjectAndSession } from './helpers.js';

interface TeamStopCommandOptions {
  path?: string;
}

export function createTeamStopCommand(): Command {
  return new Command('stop')
    .description('Stop a local tmux team session and mark saved state as stopped')
    .argument('[session]', 'Session id/name to stop (defaults to latest active session in cwd project)')
    .option('--path <path>', 'Unity project path when stopping a specific session id/name')
    .action((sessionRef: string | undefined, options: TeamStopCommandOptions) => {
      try {
        const { projectPath, sessionRef: resolvedSessionRef } = resolveTeamProjectAndSession(sessionRef, options);
        const state = stopTeamSession(projectPath, createTmuxAdapter(), resolvedSessionRef);
        ui.success(`Stopped team session ${state.sessionId}.`);
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
