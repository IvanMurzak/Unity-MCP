import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { loadHandoffBridgeConfig, sendDiscordApprovalNotification } from '../../utils/discord-approval.js';
import { readHandoffRecord } from '../../utils/handoff-ledger.js';
import { createDiscordNotificationSpoolSnapshot } from '../../utils/handoff-server.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffNotifyDiscordOptions {
  path?: string;
  envFile?: string;
}

export function createHandoffNotifyDiscordCommand(): Command {
  return new Command('notify-discord')
    .description('Send a Discord approval notification for a leader-owned handoff record')
    .argument('<handoff-id>', 'Leader-owned handoff id to notify for')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when handoff id is passed as the first positional argument')
    .option('--env-file <path>', 'Optional env file containing UNITY_MCP_HANDOFF_* secrets')
    .action(async (handoffId: string, positionalPath: string | undefined, options: HandoffNotifyDiscordOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const record = readHandoffRecord(projectPath, handoffId);
        if (record.state !== 'awaiting_approval') {
          throw new Error(`Handoff ${handoffId} must be awaiting_approval before Discord notify; current state is ${record.state}.`);
        }

        const config = loadHandoffBridgeConfig({ envFilePath: options.envFile });
        const notification = await sendDiscordApprovalNotification(config, record);
        const spoolFilePath = createDiscordNotificationSpoolSnapshot({
          projectPath,
          messageId: notification.messageId,
          channelId: notification.channelId,
          handoffId: record.handoffId,
          recordVersion: record.recordVersion,
          requestedAction: record.requestedAction,
          sourceLane: record.sourceLane,
          targetLane: record.targetLane,
        });

        ui.heading('Unity-MCP Handoff Discord Notification');
        ui.label('Project', projectPath);
        ui.label('Handoff', record.handoffId);
        ui.label('Record version', String(record.recordVersion));
        ui.label('Gate', record.requestedAction);
        ui.label('Discord message', notification.messageId);
        ui.label('Spool file', spoolFilePath);
        ui.divider();
        ui.success(`Discord approval notification queued for handoff ${record.handoffId}.`);
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}
