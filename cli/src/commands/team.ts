import { Command } from 'commander';
import { createTeamLaunchCommand } from './team/launch.js';
import { createTeamListCommand } from './team/list.js';
import { createTeamStatusCommand } from './team/status.js';
import { createTeamStopCommand } from './team/stop.js';

export function createTeamCommand(launcherVersion: string): Command {
  const command = new Command('team')
    .description('Local tmux-based team orchestration for Unity projects');

  command.addCommand(createTeamLaunchCommand(launcherVersion));
  command.addCommand(createTeamListCommand());
  command.addCommand(createTeamStatusCommand());
  command.addCommand(createTeamStopCommand());

  return command;
}
