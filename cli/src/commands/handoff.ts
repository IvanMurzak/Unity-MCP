import { Command } from 'commander';
import { createHandoffDispatchApprovedCommand } from './handoff/dispatch-approved.js';
import { createHandoffNotifyDiscordCommand } from './handoff/notify-discord.js';
import { createHandoffServeCommand } from './handoff/serve.js';

export function createHandoffCommand(): Command {
  const command = new Command('handoff')
    .description('Leader-owned approval handoff bridge commands');

  command.addCommand(createHandoffServeCommand());
  command.addCommand(createHandoffNotifyDiscordCommand());
  command.addCommand(createHandoffDispatchApprovedCommand());

  return command;
}
