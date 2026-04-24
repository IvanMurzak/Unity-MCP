import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { createTeamRuntime } from '../../utils/team-runtime.js';
import { launchTeamSession } from '../../utils/team-orchestration.js';
import { getTeamStateFilePath } from '../../utils/team-state.js';
import { resolveTeamProjectPath } from './helpers.js';

interface TeamLaunchCommandOptions {
  path?: string;
  layout?: string;
  sessionName?: string;
}

export function createTeamLaunchCommand(launcherVersion: string): Command {
  return new Command('launch')
    .description('Launch a local team session for a Unity project (tmux backend currently shipped)')
    .argument('[path]', 'Unity project path (defaults to current directory)')
    .option('--path <path>', 'Unity project path (defaults to current directory)')
    .option('--layout <name>', 'Layout preset name (milestone 1 supports only: default)', 'default')
    .option('--session-name <name>', 'Override the generated team session name')
    .action((positionalPath: string | undefined, options: TeamLaunchCommandOptions) => {
      try {
        const projectPath = resolveTeamProjectPath(positionalPath, options);
        const runtime = createTeamRuntime();
        const state = launchTeamSession(projectPath, launcherVersion, runtime, options);

        ui.heading('Unity-MCP Team Launch');
        ui.label('Project', state.projectPath);
        ui.label('Session', state.sessionId);
        ui.label('Runtime', `${state.runtime.kind} (${state.runtime.sessionHandle})`);
        ui.label('Layout', state.layoutPreset);
        ui.label('State file', getTeamStateFilePath(state.projectPath, state.sessionId));
        ui.divider();
        for (const role of state.roles) {
          ui.label(role.roleName, `${role.runtimeHandle || 'unassigned'} (${role.paneTitle})`);
        }
        ui.divider();
        ui.success(`Team session ${state.sessionId} is ready.`);
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
