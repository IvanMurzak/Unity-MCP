import * as vscode from 'vscode';
import { configureVscodeProject } from './cliAdapter';
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
    vscode.commands.registerCommand('unityMcp.configureProject', async () => {
      const workspaceFolder = await pickWorkspaceFolder();
      logger.debug('workspace:pick', {
        selected: workspaceFolder?.uri.fsPath ?? null,
      });

      if (!workspaceFolder) {
        void vscode.window.showWarningMessage(
          'Unity MCP needs an open workspace folder before it can write project configuration.',
        );
        logger.warn('configure:error', {
          reason: 'no-workspace-folder',
        });
        return;
      }

      if (!vscode.workspace.isTrusted) {
        logger.warn('configure:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'workspace-not-trusted',
        });

        const selection = await vscode.window.showWarningMessage(
          'Unity MCP only writes project configuration in a trusted workspace.',
          'Manage Trust',
        );

        if (selection === 'Manage Trust') {
          await vscode.commands.executeCommand('workbench.trust.manage');
        }
        return;
      }

      const initialStatus = await inspectWorkspaceStatus(
        workspaceFolder.uri.fsPath,
        workspaceFolder.name,
        'trusted',
      );

      if (!initialStatus.unityProjectDetected) {
        logger.warn('configure:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'not-unity-project',
        });

        void vscode.window.showErrorMessage(
          'Unity MCP can only configure a workspace that looks like a Unity project.',
        );
        return;
      }

      if (!initialStatus.pluginInstalled) {
        const pluginChoice = await vscode.window.showWarningMessage(
          'Unity MCP plugin was not detected in Packages/manifest.json. Continue writing .vscode/mcp.json anyway?',
          'Continue',
          'Cancel',
        );

        if (pluginChoice !== 'Continue') {
          logger.warn('configure:precheck', {
            workspace: workspaceFolder.uri.fsPath,
            reason: 'plugin-missing-cancelled',
          });
          return;
        }
      }

      const transportChoice = await vscode.window.showQuickPick(
        [
          {
            label: 'HTTP',
            description: 'Recommended',
            detail: 'Writes an HTTP MCP server entry into .vscode/mcp.json.',
            transport: 'http' as const,
          },
          {
            label: 'STDIO',
            detail: 'Writes a stdio MCP server entry that points to the local Unity MCP server binary.',
            transport: 'stdio' as const,
          },
        ],
        {
          placeHolder: 'Select which transport Unity MCP should configure for VS Code',
        },
      );

      if (!transportChoice) {
        logger.warn('configure:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'transport-not-selected',
        });
        return;
      }

      logger.info('configure:start', {
        workspace: workspaceFolder.uri.fsPath,
        transport: transportChoice.transport,
      });

      const result = await configureVscodeProject(logger, {
        workspacePath: workspaceFolder.uri.fsPath,
        transport: transportChoice.transport,
      });

      if (result.kind === 'failure') {
        void vscode.window.showErrorMessage(
          `Unity MCP could not configure the project: ${result.error.message}`,
          'Show Output',
        ).then((selection) => {
          if (selection === 'Show Output') {
            logger.show();
          }
        });
        return;
      }

      logger.info('configure:writeSuccess', {
        configPath: result.configPath,
        transport: result.transport,
      });
      if (result.warnings.length > 0) {
        logger.warn('configure:warnings', {
          warnings: result.warnings,
        });
      }
      if (result.nextSteps.length > 0) {
        logger.info('configure:nextSteps', {
          nextSteps: result.nextSteps,
        });
      }

      const updatedStatus = await inspectWorkspaceStatus(
        workspaceFolder.uri.fsPath,
        workspaceFolder.name,
        'trusted',
      );
      logger.appendReport(
        'Unity MCP Status',
        formatWorkspaceStatusReport(updatedStatus),
      );
      logger.show();

      void vscode.window.showInformationMessage(
        `Unity MCP configured ${workspaceFolder.name} for VS Code (${result.transport}).`,
        'Open Config',
        'Show Output',
      ).then(async (selection) => {
        if (selection === 'Open Config') {
          const document = await vscode.workspace.openTextDocument(result.configPath);
          await vscode.window.showTextDocument(document);
        }

        if (selection === 'Show Output') {
          logger.show();
        }
      });
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
    commands: [
      'unityMcp.checkStatus',
      'unityMcp.configureProject',
      'unityMcp.showOutput',
    ],
  });
}

export function deactivate(): void {
  // Nothing to dispose beyond the extension context subscriptions.
}
