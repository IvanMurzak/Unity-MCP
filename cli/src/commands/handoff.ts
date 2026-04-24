import { Command } from 'commander';
import { createHandoffDispatchApprovedCommand } from './handoff/dispatch-approved.js';
import { createHandoffListWindowsEvidenceCommand } from './handoff/list-windows-evidence.js';
import { createHandoffNotifyDiscordCommand } from './handoff/notify-discord.js';
import { createHandoffReconcileWindowsEvidenceCommand } from './handoff/reconcile-windows-evidence.js';
import { createHandoffServeCommand } from './handoff/serve.js';
import { createHandoffSubmitWindowsEvidenceCommand } from './handoff/submit-windows-evidence.js';

export function createHandoffCommand(): Command {
  const command = new Command('handoff')
    .description('Leader-owned approval handoff bridge commands');

  command.addCommand(createHandoffServeCommand());
  command.addCommand(createHandoffNotifyDiscordCommand());
  command.addCommand(createHandoffDispatchApprovedCommand());
  command.addCommand(createHandoffSubmitWindowsEvidenceCommand());
  command.addCommand(createHandoffListWindowsEvidenceCommand());
  command.addCommand(createHandoffReconcileWindowsEvidenceCommand());

  return command;
}
