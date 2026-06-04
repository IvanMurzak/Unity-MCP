import * as vscode from 'vscode';
import { ExtensionLogger } from './logging';
import {
  formatWorkspaceStatusReport,
  inspectWorkspaceStatus,
} from './projectStatus';
import { pickWorkspaceFolder } from './workspace';

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const logger = new ExtensionLogger();
  context.subscriptions.push(logger);

  logger.info('activate:start', {
    workspaceCount: vscode.workspace.workspaceFolders?.length ?? 0,
    trusted: vscode.workspace.isTrusted,
  });

  context.subscriptions.push(
    vscode.workspace.onDidGrantWorkspaceTrust(() => {
      logger.info('trust:granted', {});
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.showOutput', () => {
      logger.show();
      logger.debug('output:show', {});
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.checkStatus', async () => {
      const workspaceFolder = await pickWorkspaceFolder();
      logger.debug('workspace:pick', {
        selected: workspaceFolder?.uri.fsPath ?? null,
      });

      if (!workspaceFolder) {
        void vscode.window.showWarningMessage(
          'Unity MCP needs an open workspace folder to inspect project status.',
        );
        logger.warn('status:error', {
          reason: 'no-workspace-folder',
        });
        return;
      }

      const trustState = vscode.workspace.isTrusted ? 'trusted' : 'restricted';

      try {
        logger.info('status:computeStart', {
          workspace: workspaceFolder.uri.fsPath,
          trustState,
        });

        const status = await inspectWorkspaceStatus(
          workspaceFolder.uri.fsPath,
          workspaceFolder.name,
          trustState,
        );

        logger.info('status:computeResult', {
          workspace: workspaceFolder.uri.fsPath,
          unityProjectDetected: status.unityProjectDetected,
          pluginInstalled: status.pluginInstalled,
          mcpConfigExists: status.mcpConfigExists,
          mcpServerConfigured: status.mcpServerConfigured,
        });

        logger.appendReport(
          'Unity MCP Status',
          formatWorkspaceStatusReport(status),
        );
        logger.show();

        const summary = status.unityProjectDetected
          ? `Unity MCP status collected for ${workspaceFolder.name}.`
          : `${workspaceFolder.name} does not look like a Unity project.`;

        void vscode.window.showInformationMessage(summary, 'Show Output').then((selection) => {
          if (selection === 'Show Output') {
            logger.show();
          }
        });
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        logger.error('status:error', {
          workspace: workspaceFolder.uri.fsPath,
          message,
        });

        void vscode.window.showErrorMessage(
          `Unity MCP failed to inspect the workspace: ${message}`,
          'Show Output',
        ).then((selection) => {
          if (selection === 'Show Output') {
            logger.show();
          }
        });
      }
    }),
  );

  logger.info('activate:complete', {
    commands: ['unityMcp.checkStatus', 'unityMcp.showOutput'],
  });
}

export function deactivate(): void {
  // Nothing to dispose beyond the extension context subscriptions.
}
