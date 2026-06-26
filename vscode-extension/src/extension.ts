import * as vscode from 'vscode';
import { configureVscodeProject, installUnityMcpPlugin, openUnityProject } from './cliAdapter';
import {
  buildStatusBarPresentation,
  type DashboardSnapshot,
  UnityMcpDashboardProvider,
} from './dashboard';
import { ExtensionLogger } from './logging';
import {
  formatWorkspaceStatusReport,
  inspectWorkspaceStatus,
  type WorkspaceAction,
} from './projectStatus';
import { readUnityMcpProjectConfig } from './unityConfig';
import { normalizeFsPath } from './utils';
import { getPreferredWorkspaceFolder, pickWorkspaceFolder } from './workspace';

const UI_REFRESH_DEBOUNCE_MS = 150;
const STATUS_RELEVANT_FILES = new Set([
  'Packages/manifest.json',
  '.vscode/mcp.json',
  'UserSettings/AI-Game-Developer-Config.json',
]);

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const logger = new ExtensionLogger();
  context.subscriptions.push(logger);
  const statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
  statusBarItem.command = 'unityMcp.showDashboard';
  context.subscriptions.push(statusBarItem);

  const dashboardProvider = new UnityMcpDashboardProvider(
    context.extensionUri,
    getDashboardSnapshot,
    async (commandId) => {
      await vscode.commands.executeCommand(commandId);
      await refreshUi();
    },
    (event, properties) => {
      logger.info(event, properties ?? {});
    },
  );
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider(
      UnityMcpDashboardProvider.viewId,
      dashboardProvider,
    ),
  );

  logger.info('activate:start', {
    workspaceCount: vscode.workspace.workspaceFolders?.length ?? 0,
    trusted: vscode.workspace.isTrusted,
  });

  let scheduledRefreshHandle: NodeJS.Timeout | undefined;

  async function getDashboardSnapshot(): Promise<DashboardSnapshot> {
    const workspaceFolder = getPreferredWorkspaceFolder();
    if (!workspaceFolder) {
      return {};
    }

    return {
      workspaceFolder,
      status: await inspectWorkspaceStatus(
        workspaceFolder.uri.fsPath,
        workspaceFolder.name,
        vscode.workspace.isTrusted ? 'trusted' : 'restricted',
      ),
    };
  }

  async function refreshUi(): Promise<void> {
    logger.debug('dashboard:refreshUiStart', {
      viewResolved: dashboardProvider.hasResolvedView(),
    });
    const snapshot = await getDashboardSnapshot();
    logger.debug('dashboard:snapshot', {
      hasWorkspace: Boolean(snapshot.workspaceFolder),
      workspaceName: snapshot.workspaceFolder?.name,
      trustState: snapshot.status?.trustState,
      unityProjectDetected: snapshot.status?.unityProjectDetected,
      pluginInstalled: snapshot.status?.pluginInstalled,
      unityConfigReady: snapshot.status?.unityMcpProjectConfigReady,
      mcpConfigured: snapshot.status?.mcpServerConfigured,
    });
    const statusBar = buildStatusBarPresentation(snapshot);
    statusBarItem.text = statusBar.text;
    statusBarItem.tooltip = statusBar.tooltip;
    statusBarItem.show();
    await dashboardProvider.refresh();
  }

  function scheduleRefreshUi(reason: string): void {
    if (scheduledRefreshHandle) {
      clearTimeout(scheduledRefreshHandle);
    }

    logger.debug('dashboard:refreshScheduled', {
      reason,
      delayMs: UI_REFRESH_DEBOUNCE_MS,
    });

    scheduledRefreshHandle = setTimeout(() => {
      scheduledRefreshHandle = undefined;
      void refreshUi();
    }, UI_REFRESH_DEBOUNCE_MS);
  }

  function isRelevantStatusDocument(document: vscode.TextDocument): boolean {
    if (document.uri.scheme !== 'file') {
      return false;
    }

    const workspaceFolder = vscode.workspace.getWorkspaceFolder(document.uri);
    if (!workspaceFolder) {
      return false;
    }

    const relativePath = pathRelativeToWorkspace(workspaceFolder, document.uri);
    return STATUS_RELEVANT_FILES.has(relativePath);
  }

  async function runOpenUnity(
    mode: 'prompt' | 'plain' | 'connected',
  ): Promise<void> {
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

    let effectiveMode = mode;
    if (effectiveMode === 'prompt') {
      const openMode = await vscode.window.showQuickPick(
        [
          {
            label: 'Open Unity',
            detail: 'Launch the Unity project without overriding MCP connection settings.',
            mode: 'plain' as const,
          },
          {
            label: 'Open Unity With MCP Connection',
            detail: describeConnectedLaunchAvailability(projectConfig),
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

      effectiveMode = openMode.mode;
    }

    if (effectiveMode === 'connected' && !projectConfig.ready) {
      logger.warn('openUnity:precheck', {
        workspace: workspaceFolder.uri.fsPath,
        reason: projectConfig.exists ? 'project-config-invalid' : 'project-config-missing',
      });

      const selection = await vscode.window.showWarningMessage(
        projectConfig.exists
          ? 'Unity MCP found AI-Game-Developer-Config.json, but it is invalid or incomplete. Open Unity once without MCP and fix or regenerate the project config before retrying connected launch.'
          : 'Unity MCP is installed, but the project has not finished first-time initialization yet. Open Unity once without MCP so the package can import and create its project config, then retry connected launch.',
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
  }

  context.subscriptions.push(
    vscode.workspace.onDidGrantWorkspaceTrust(() => {
      logger.info('trust:granted', {});
      scheduleRefreshUi('workspace-trusted');
    }),
  );

  context.subscriptions.push(
    vscode.workspace.onDidChangeWorkspaceFolders(() => {
      scheduleRefreshUi('workspace-folders-changed');
    }),
  );

  context.subscriptions.push(
    vscode.window.onDidChangeActiveTextEditor(() => {
      scheduleRefreshUi('active-editor-changed');
    }),
  );

  context.subscriptions.push(
    vscode.workspace.onDidSaveTextDocument((document) => {
      if (!isRelevantStatusDocument(document)) {
        return;
      }

      scheduleRefreshUi(`saved:${document.uri.fsPath}`);
    }),
  );

  context.subscriptions.push({
    dispose: () => {
      if (scheduledRefreshHandle) {
        clearTimeout(scheduledRefreshHandle);
      }
    },
  });

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.showOutput', () => {
      logger.show();
      logger.debug('output:show', {});
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.openUnity', async () => runOpenUnity('prompt')),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.openUnityPlain', async () => runOpenUnity('plain')),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.openUnityConnected', async () => runOpenUnity('connected')),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.showDashboard', async () => {
      await vscode.commands.executeCommand('workbench.view.extension.unityMcpSidebar');
      await refreshUi();
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('unityMcp.refreshDashboard', async () => {
      await refreshUi();
      logger.info('dashboard:refreshed', {});
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

      await refreshUi();
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

      await refreshUi();
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

        const actions = buildStatusMessageActions(status);
        const summary = status.unityProjectDetected
          ? `Unity MCP status collected for ${workspaceFolder.name}.`
          : `${workspaceFolder.name} does not look like a Unity project.`;

        void vscode.window.showInformationMessage(summary, ...actions).then(async (selection) => {
          if (!selection) {
            return;
          }

          if (selection === 'Show Output') {
            logger.show();
            return;
          }

          const command = statusActionToCommand(selection);
          if (command) {
            await vscode.commands.executeCommand(command);
            await refreshUi();
          }
        });
        await refreshUi();
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
      'unityMcp.showDashboard',
      'unityMcp.refreshDashboard',
      'unityMcp.showOutput',
    ],
  });

  await refreshUi();
}

function buildStatusMessageActions(status: {
  recommendedActions: WorkspaceAction[];
}): string[] {
  const actions = status.recommendedActions.map((action) => actionToLabel(action));
  actions.push('Show Output');
  return actions.slice(0, 3);
}

function actionToLabel(action: WorkspaceAction): string {
  switch (action) {
    case 'trust-workspace':
      return 'Manage Trust';
    case 'install-plugin':
      return 'Install Plugin';
    case 'open-unity-without-mcp':
      return 'Open Unity';
    case 'configure-vscode-mcp':
      return 'Configure Project';
    case 'open-unity-with-mcp':
      return 'Open Unity With MCP';
  }
}

function statusActionToCommand(actionLabel: string): string | undefined {
  switch (actionLabel) {
    case 'Manage Trust':
      return 'workbench.trust.manage';
    case 'Install Plugin':
      return 'unityMcp.installPlugin';
    case 'Open Unity':
      return 'unityMcp.openUnityPlain';
    case 'Configure Project':
      return 'unityMcp.configureProject';
    case 'Open Unity With MCP':
      return 'unityMcp.openUnityConnected';
    default:
      return undefined;
  }
}

export function deactivate(): void {
  // Nothing to dispose beyond the extension context subscriptions.
}

function describeConnectedLaunchAvailability(
  projectConfig: Awaited<ReturnType<typeof readUnityMcpProjectConfig>>,
): string {
  if (projectConfig.ready) {
    return 'Use the current AI-Game-Developer project config and request server startup.';
  }

  if (projectConfig.exists) {
    return 'Blocked until UserSettings/AI-Game-Developer-Config.json is valid again.';
  }

  return 'Requires UserSettings/AI-Game-Developer-Config.json to be present.';
}

function pathRelativeToWorkspace(
  workspaceFolder: vscode.WorkspaceFolder,
  uri: vscode.Uri,
): string {
  const normalizedWorkspace = normalizeFsPath(workspaceFolder.uri.fsPath);
  const normalizedDocument = normalizeFsPath(uri.fsPath);

  if (!normalizedDocument.startsWith(`${normalizedWorkspace}/`)) {
    return normalizedDocument;
  }

  return normalizedDocument.slice(normalizedWorkspace.length + 1);
}
