import * as vscode from 'vscode';
import { configureVscodeProject, installUnityMcpPlugin, openUnityProject } from './cliAdapter';
import { ExtensionLogger } from './logging';
import {
  formatWorkspaceStatusReport,
  inspectWorkspaceStatus,
} from './projectStatus';
import { readUnityMcpProjectConfig } from './unityConfig';
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
    vscode.commands.registerCommand('unityMcp.openUnity', async () => {
      const workspaceFolder = await pickWorkspaceFolder();
      logger.debug('workspace:pick', {
        selected: workspaceFolder?.uri.fsPath ?? null,
      });

      if (!workspaceFolder) {
        void vscode.window.showWarningMessage(
          'Unity MCP needs an open workspace folder before it can launch Unity.',
        );
        logger.warn('openUnity:error', {
          reason: 'no-workspace-folder',
        });
        return;
      }

      if (!vscode.workspace.isTrusted) {
        logger.warn('openUnity:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'workspace-not-trusted',
        });

        const selection = await vscode.window.showWarningMessage(
          'Unity MCP only launches Unity from a trusted workspace.',
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
        logger.warn('openUnity:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'not-unity-project',
        });

        void vscode.window.showErrorMessage(
          'Unity MCP can only open a workspace that looks like a Unity project.',
        );
        return;
      }

      const projectConfig = await readUnityMcpProjectConfig(workspaceFolder.uri.fsPath);
      if (projectConfig.warnings.length > 0) {
        logger.warn('openUnity:configWarnings', {
          warnings: projectConfig.warnings,
        });
      }

      const openMode = await vscode.window.showQuickPick(
        [
          {
            label: 'Open Unity',
            detail: 'Launch the Unity project without overriding MCP connection settings.',
            mode: 'plain' as const,
          },
          {
            label: 'Open Unity With MCP Connection',
            detail: projectConfig.exists
              ? 'Use the current AI-Game-Developer project config and request server startup.'
              : 'Requires UserSettings/AI-Game-Developer-Config.json to be present.',
            mode: 'connected' as const,
          },
        ],
        {
          placeHolder: 'Choose how Unity MCP should launch this Unity project',
        },
      );

      if (!openMode) {
        logger.warn('openUnity:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'mode-not-selected',
        });
        return;
      }

      let effectiveMode = openMode.mode;
      if (effectiveMode === 'connected' && !projectConfig.exists) {
        logger.warn('openUnity:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'project-config-missing',
        });

        const selection = await vscode.window.showWarningMessage(
          'Unity MCP is installed, but the project has not finished first-time initialization yet. Open Unity once without MCP so the package can import and create its project config, then retry connected launch.',
          'Open Without MCP',
          'Show Output',
          'Cancel',
        );

        if (selection === 'Show Output') {
          logger.show();
        }

        if (selection !== 'Open Without MCP') {
          return;
        }

        effectiveMode = 'plain';
      }

      logger.info('openUnity:start', {
        workspace: workspaceFolder.uri.fsPath,
        mode: effectiveMode,
      });

      const result = await openUnityProject(logger, {
        workspacePath: workspaceFolder.uri.fsPath,
        noConnect: effectiveMode === 'plain',
        url: effectiveMode === 'connected' ? projectConfig.host : undefined,
        token: effectiveMode === 'connected' ? projectConfig.token : undefined,
        auth: effectiveMode === 'connected' ? projectConfig.authOption : undefined,
        keepConnected: effectiveMode === 'connected' ? projectConfig.keepConnected : undefined,
        transport: effectiveMode === 'connected' ? projectConfig.transport : undefined,
        startServer: effectiveMode === 'connected' ? true : undefined,
      });

      if (result.kind === 'failure') {
        void vscode.window.showErrorMessage(
          `Unity MCP could not open Unity: ${result.errorMessage}`,
          'Show Output',
        ).then((selection) => {
          if (selection === 'Show Output') {
            logger.show();
          }
        });
        return;
      }

      if (result.warnings.length > 0) {
        logger.warn('openUnity:warnings', {
          warnings: result.warnings,
        });
      }

      logger.show();
      const summary = result.alreadyRunning
        ? `Unity is already running for ${workspaceFolder.name}.`
        : `Unity launch requested for ${workspaceFolder.name}.`;

      void vscode.window.showInformationMessage(
        summary,
        'Show Output',
      ).then((selection) => {
        if (selection === 'Show Output') {
          logger.show();
        }
      });
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.installPlugin', async () => {
      const workspaceFolder = await pickWorkspaceFolder();
      logger.debug('workspace:pick', {
        selected: workspaceFolder?.uri.fsPath ?? null,
      });

      if (!workspaceFolder) {
        void vscode.window.showWarningMessage(
          'Unity MCP needs an open workspace folder before it can install the Unity package.',
        );
        logger.warn('pluginInstall:error', {
          reason: 'no-workspace-folder',
        });
        return;
      }

      if (!vscode.workspace.isTrusted) {
        logger.warn('pluginInstall:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'workspace-not-trusted',
        });

        const selection = await vscode.window.showWarningMessage(
          'Unity MCP only installs the Unity package in a trusted workspace.',
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
        logger.warn('pluginInstall:precheck', {
          workspace: workspaceFolder.uri.fsPath,
          reason: 'not-unity-project',
        });

        void vscode.window.showErrorMessage(
          'Unity MCP can only install the Unity package into a workspace that looks like a Unity project.',
        );
        return;
      }

      if (initialStatus.pluginInstalled) {
        const alreadyInstalledChoice = await vscode.window.showInformationMessage(
          `Unity MCP plugin already appears in ${workspaceFolder.name}. Install again anyway to let the shared library reconcile the manifest?`,
          'Re-run Install',
          'Cancel',
        );

        if (alreadyInstalledChoice !== 'Re-run Install') {
          logger.warn('pluginInstall:precheck', {
            workspace: workspaceFolder.uri.fsPath,
            reason: 'already-installed-cancelled',
          });
          return;
        }
      } else {
        const confirmInstall = await vscode.window.showWarningMessage(
          'Unity MCP will update Packages/manifest.json in this Unity project. Continue?',
          'Install Plugin',
          'Cancel',
        );

        if (confirmInstall !== 'Install Plugin') {
          logger.warn('pluginInstall:precheck', {
            workspace: workspaceFolder.uri.fsPath,
            reason: 'install-cancelled',
          });
          return;
        }
      }

      logger.info('pluginInstall:start', {
        workspace: workspaceFolder.uri.fsPath,
      });

      const result = await installUnityMcpPlugin(logger, {
        workspacePath: workspaceFolder.uri.fsPath,
      });

      if (result.kind === 'failure') {
        void vscode.window.showErrorMessage(
          `Unity MCP could not install the plugin: ${result.error.message}`,
          'Show Output',
        ).then((selection) => {
          if (selection === 'Show Output') {
            logger.show();
          }
        });
        return;
      }

      if (result.warnings.length > 0) {
        logger.warn('pluginInstall:warnings', {
          warnings: result.warnings,
        });
      }
      if (result.nextSteps.length > 0) {
        logger.info('pluginInstall:nextSteps', {
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
        `Unity MCP plugin installed for ${workspaceFolder.name} (version ${result.installedVersion}).`,
        'Open Manifest',
        'Show Output',
      ).then(async (selection) => {
        if (selection === 'Open Manifest') {
          const document = await vscode.workspace.openTextDocument(result.manifestPath);
          await vscode.window.showTextDocument(document);
        }

        if (selection === 'Show Output') {
          logger.show();
        }
      });
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
      'unityMcp.installPlugin',
      'unityMcp.openUnity',
      'unityMcp.showOutput',
    ],
  });
}

export function deactivate(): void {
  // Nothing to dispose beyond the extension context subscriptions.
}
