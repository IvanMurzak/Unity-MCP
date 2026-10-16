import { randomBytes } from 'node:crypto';
import * as vscode from 'vscode';
import {
  formatWorkspaceStatusReport,
  type WorkspaceStatus,
} from './projectStatus';

export interface DashboardSnapshot {
  workspaceFolder?: vscode.WorkspaceFolder;
  status?: WorkspaceStatus;
}

export type DashboardCommandId =
  | 'unityMcp.checkStatus'
  | 'unityMcp.showOutput'
  | 'workbench.trust.manage'
  | 'unityMcp.installPlugin'
  | 'unityMcp.configureProject'
  | 'unityMcp.openUnityPlain'
  | 'unityMcp.openUnityConnected';

const ALLOWED_DASHBOARD_COMMANDS = new Set<DashboardCommandId>([
  'unityMcp.checkStatus',
  'unityMcp.showOutput',
  'workbench.trust.manage',
  'unityMcp.installPlugin',
  'unityMcp.configureProject',
  'unityMcp.openUnityPlain',
  'unityMcp.openUnityConnected',
]);

interface DashboardActionItem {
  commandId: DashboardCommandId;
  label: string;
  description: string;
  recommended: boolean;
}

interface DashboardRecommendation {
  title: string;
  detail: string;
}

export class UnityMcpDashboardProvider implements vscode.WebviewViewProvider {
  public static readonly viewId = 'unityMcp.dashboard';

  private view?: vscode.WebviewView;

  public constructor(
    private readonly extensionUri: vscode.Uri,
    private readonly getSnapshot: () => Promise<DashboardSnapshot>,
    private readonly onCommand: (commandId: DashboardCommandId) => Promise<void>,
    private readonly onEvent?: (event: string, properties?: Record<string, unknown>) => void,
  ) {}

  public resolveWebviewView(
    webviewView: vscode.WebviewView,
  ): void | Thenable<void> {
    this.view = webviewView;
    this.onEvent?.('dashboard:viewResolved', {
      viewId: UnityMcpDashboardProvider.viewId,
    });
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [vscode.Uri.joinPath(this.extensionUri, 'media')],
    };

    webviewView.webview.onDidReceiveMessage(async (message: unknown) => {
      if (
        typeof message === 'object' &&
        message !== null &&
        'type' in message &&
        'commandId' in message &&
        message.type === 'run-command' &&
        typeof message.commandId === 'string'
      ) {
        if (!isAllowedDashboardCommand(message.commandId)) {
          this.onEvent?.('dashboard:commandRejected', {
            commandId: message.commandId,
          });
          return;
        }

        this.onEvent?.('dashboard:command', {
          commandId: message.commandId,
        });
        await this.onCommand(message.commandId);
      }
    });

    return this.refresh();
  }

  public hasResolvedView(): boolean {
    return this.view !== undefined;
  }

  public async refresh(): Promise<void> {
    if (!this.view) {
      this.onEvent?.('dashboard:refreshSkipped', {
        reason: 'view-not-resolved',
      });
      return;
    }

    const snapshot = await this.getSnapshot();
    this.onEvent?.('dashboard:render', {
      hasWorkspace: Boolean(snapshot.workspaceFolder),
      workspaceName: snapshot.workspaceFolder?.name,
      trustState: snapshot.status?.trustState,
      unityProjectDetected: snapshot.status?.unityProjectDetected,
      pluginInstalled: snapshot.status?.pluginInstalled,
      unityConfigReady: snapshot.status?.unityMcpProjectConfigReady,
      mcpConfigured: snapshot.status?.mcpServerConfigured,
    });
    this.view.webview.html = renderDashboardHtml(
      this.view.webview,
      this.extensionUri,
      snapshot,
    );
  }
}

export function buildStatusBarPresentation(snapshot: DashboardSnapshot): {
  text: string;
  tooltip: string;
} {
  const status = snapshot.status;

  if (!snapshot.workspaceFolder || !status) {
    return {
      text: '$(circle-slash) Unity MCP',
      tooltip: 'Open a workspace folder to use Unity MCP.',
    };
  }

  if (status.trustState !== 'trusted') {
    return {
      text: '$(shield) Unity MCP: Trust',
      tooltip: 'Trust this workspace before using Unity MCP write or launch actions.',
    };
  }

  if (!status.unityProjectDetected) {
    return {
      text: '$(circle-slash) Unity MCP',
      tooltip: `${status.workspaceName} does not look like a Unity project.`,
    };
  }

  if (!status.pluginInstalled) {
    return {
      text: '$(package) Unity MCP: Install',
      tooltip: 'Unity MCP plugin is missing. Install it into this Unity project.',
    };
  }

  if (!status.unityMcpProjectConfigExists) {
    return {
      text: '$(sync~spin) Unity MCP: Init',
      tooltip: 'Open Unity once without MCP so the Unity MCP package can initialize.',
    };
  }

  if (!status.unityMcpProjectConfigReady) {
    return {
      text: '$(warning) Unity MCP: Fix Config',
      tooltip: 'The Unity MCP project config exists but is invalid or incomplete. Open Unity without MCP and review the diagnostics.',
    };
  }

  if (!status.mcpServerConfigured) {
    return {
      text: '$(settings-gear) Unity MCP: Configure',
      tooltip: 'VS Code MCP config is missing or incomplete. Configure this project.',
    };
  }

  return {
    text: '$(check) Unity MCP: Ready',
    tooltip: `Unity MCP is ready for ${status.workspaceName}.`,
  };
}

export function isAllowedDashboardCommand(
  commandId: string,
): commandId is DashboardCommandId {
  return ALLOWED_DASHBOARD_COMMANDS.has(commandId as DashboardCommandId);
}

export function buildDashboardActions(snapshot: DashboardSnapshot): DashboardActionItem[] {
  const status = snapshot.status;

  if (!snapshot.workspaceFolder || !status) {
    return [
      {
        commandId: 'unityMcp.checkStatus',
        label: 'Check Status',
        description: 'Inspect the currently open workspace.',
        recommended: true,
      },
      {
        commandId: 'unityMcp.showOutput',
        label: 'Show Output',
        description: 'Open the Unity MCP output channel.',
        recommended: false,
      },
    ];
  }

  if (status.trustState !== 'trusted') {
    return [
      {
        commandId: 'workbench.trust.manage',
        label: 'Manage Trust',
        description: 'Trust this workspace before running write or launch actions.',
        recommended: true,
      },
      {
        commandId: 'unityMcp.checkStatus',
        label: 'Check Status',
        description: 'Refresh workspace diagnostics and next-step guidance.',
        recommended: false,
      },
      {
        commandId: 'unityMcp.showOutput',
        label: 'Show Output',
        description: 'Open the Unity MCP output channel for logs and diagnostics.',
        recommended: false,
      },
    ];
  }

  if (!status.unityProjectDetected) {
    return [
      {
        commandId: 'unityMcp.checkStatus',
        label: 'Check Status',
        description: 'Refresh workspace diagnostics and confirm Unity project detection.',
        recommended: true,
      },
      {
        commandId: 'unityMcp.showOutput',
        label: 'Show Output',
        description: 'Open the Unity MCP output channel for logs and diagnostics.',
        recommended: false,
      },
    ];
  }

  const items: DashboardActionItem[] = [];

  if (!status.pluginInstalled) {
    items.push({
      commandId: 'unityMcp.installPlugin',
      label: 'Install Plugin',
      description: 'Add the Unity MCP package to Packages/manifest.json.',
      recommended: true,
    });
    items.push({
      commandId: 'unityMcp.configureProject',
      label: 'Configure Project',
      description: 'Write .vscode/mcp.json now if you want VS Code ready before the plugin install finishes.',
      recommended: false,
    });
  } else if (!status.unityMcpProjectConfigExists) {
    items.push({
      commandId: 'unityMcp.openUnityPlain',
      label: 'Open Unity',
      description: 'Launch Unity once so the plugin can import and create its project config.',
      recommended: true,
    });
    items.push({
      commandId: 'unityMcp.installPlugin',
      label: 'Install Plugin',
      description: 'Re-run package installation to reconcile Packages/manifest.json if needed.',
      recommended: false,
    });
  } else if (!status.unityMcpProjectConfigReady) {
    items.push({
      commandId: 'unityMcp.openUnityPlain',
      label: 'Open Unity',
      description: 'Launch Unity without MCP and repair or regenerate the AI-Game-Developer project config.',
      recommended: true,
    });
  } else if (!status.mcpServerConfigured) {
    items.push({
      commandId: 'unityMcp.configureProject',
      label: 'Configure Project',
      description: 'Write or update .vscode/mcp.json for this Unity project.',
      recommended: true,
    });
    items.push({
      commandId: 'unityMcp.openUnityConnected',
      label: 'Open Unity With MCP',
      description: 'Launch Unity using the current AI-Game-Developer connection config.',
      recommended: false,
    });
  } else {
    items.push({
      commandId: 'unityMcp.openUnityConnected',
      label: 'Open Unity With MCP',
      description: 'Launch Unity using the current AI-Game-Developer connection config.',
      recommended: true,
    });
    items.push({
      commandId: 'unityMcp.openUnityPlain',
      label: 'Open Unity',
      description: 'Launch Unity without overriding MCP connection settings.',
      recommended: false,
    });
  }

  if (status.pluginInstalled) {
    items.push({
      commandId: 'unityMcp.configureProject',
      label: 'Configure Project',
      description: 'Write or update .vscode/mcp.json for this Unity project.',
      recommended: false,
    });
  }

  items.push({
    commandId: 'unityMcp.checkStatus',
    label: 'Check Status',
    description: 'Refresh workspace diagnostics and next-step guidance.',
    recommended: false,
  });
  items.push({
    commandId: 'unityMcp.showOutput',
    label: 'Show Output',
    description: 'Open the Unity MCP output channel for logs and diagnostics.',
    recommended: false,
  });

  return dedupeActions(items);
}

function renderDashboardHtml(
  webview: vscode.Webview,
  extensionUri: vscode.Uri,
  snapshot: DashboardSnapshot,
): string {
  const nonce = createNonce();
  const iconUri = webview.asWebviewUri(
    vscode.Uri.joinPath(extensionUri, 'media', 'unity-mcp-activity.svg'),
  );
  const actions = buildDashboardActions(snapshot);
  const status = snapshot.status;
  const nextStep = buildDashboardRecommendation(status);
  const report = status
    ? formatWorkspaceStatusReport(status)
    : 'Open a workspace folder to begin using Unity MCP.';

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${webview.cspSource} data:; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Unity MCP</title>
  <style>
    :root {
      color-scheme: light dark;
    }
    body {
      padding: 0 14px 16px;
      color: var(--vscode-foreground);
      font-family: var(--vscode-font-family);
      font-size: var(--vscode-font-size);
      line-height: 1.5;
    }
    .hero {
      display: grid;
      grid-template-columns: 48px 1fr;
      gap: 12px;
      align-items: center;
      margin: 14px 0 18px;
    }
    .hero img {
      width: 40px;
      height: 40px;
    }
    .hero h1 {
      margin: 0;
      font-size: 1.15rem;
      font-weight: 700;
    }
    .hero p {
      margin: 2px 0 0;
      color: var(--vscode-descriptionForeground);
    }
    .card {
      background: color-mix(in srgb, var(--vscode-editorWidget-background) 82%, transparent);
      border: 1px solid var(--vscode-panel-border);
      border-radius: 10px;
      padding: 12px;
      margin-bottom: 12px;
    }
    .card h2 {
      margin: 0 0 8px;
      font-size: 0.95rem;
    }
    .next-step-title {
      margin: 0 0 4px;
      font-size: 1rem;
      font-weight: 700;
    }
    .next-step-detail {
      margin: 0;
      color: var(--vscode-descriptionForeground);
    }
    .state-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
    }
    .state-item {
      border: 1px solid var(--vscode-panel-border);
      border-radius: 8px;
      padding: 8px;
      background: color-mix(in srgb, var(--vscode-sideBar-background) 70%, transparent);
    }
    .label {
      display: block;
      color: var(--vscode-descriptionForeground);
      font-size: 0.84rem;
    }
    .value {
      display: block;
      font-weight: 600;
      margin-top: 2px;
    }
    .actions {
      display: grid;
      gap: 8px;
    }
    button {
      width: 100%;
      text-align: left;
      border: 1px solid var(--vscode-button-border, transparent);
      border-radius: 8px;
      padding: 10px 12px;
      background: var(--vscode-button-secondaryBackground);
      color: var(--vscode-button-secondaryForeground);
      cursor: pointer;
    }
    button.primary {
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
    }
    .button-title {
      display: block;
      font-weight: 600;
    }
    .button-description {
      display: block;
      color: inherit;
      opacity: 0.8;
      font-size: 0.9em;
      margin-top: 2px;
    }
    pre {
      margin: 0;
      white-space: pre-wrap;
      font-family: var(--vscode-editor-font-family);
      font-size: 12px;
    }
    .muted {
      color: var(--vscode-descriptionForeground);
    }
  </style>
</head>
<body>
  <section class="hero">
    <img src="${iconUri}" alt="">
    <div>
      <h1>Unity MCP</h1>
      <p>${snapshot.workspaceFolder ? escapeHtml(snapshot.workspaceFolder.name) : 'No workspace selected'}</p>
    </div>
  </section>

  ${
    nextStep
      ? `<section class="card">
          <h2>Next Step</h2>
          <p class="next-step-title">${escapeHtml(nextStep.title)}</p>
          <p class="next-step-detail">${escapeHtml(nextStep.detail)}</p>
        </section>`
      : ''
  }

  <section class="card">
    <h2>Workspace Status</h2>
    ${
      status
        ? `<div class="state-grid">
            <div class="state-item">
              <span class="label">Workspace Trust</span>
              <span class="value">${escapeHtml(status.trustState)}</span>
            </div>
            <div class="state-item">
              <span class="label">Unity Project</span>
              <span class="value">${status.unityProjectDetected ? 'Detected' : 'Not detected'}</span>
            </div>
            <div class="state-item">
              <span class="label">Plugin</span>
              <span class="value">${status.pluginInstalled ? `Installed (${escapeHtml(status.pluginVersion ?? 'unknown')})` : 'Missing'}</span>
            </div>
            <div class="state-item">
              <span class="label">Unity Config</span>
              <span class="value">${!status.unityMcpProjectConfigExists ? 'Missing' : status.unityMcpProjectConfigReady ? 'Ready' : 'Needs attention'}</span>
            </div>
            <div class="state-item">
              <span class="label">VS Code MCP</span>
              <span class="value">${status.mcpServerConfigured ? `Configured (${escapeHtml(status.mcpServerTransport ?? 'unknown')})` : 'Not configured'}</span>
            </div>
          </div>`
        : `<p class="muted">Open a Unity project folder to see setup status and actions.</p>`
    }
  </section>

  <section class="card">
    <h2>Actions</h2>
    <div class="actions">
      ${actions.map((action) => `
        <button class="${action.recommended ? 'primary' : ''}" data-command="${escapeHtml(action.commandId)}">
          <span class="button-title">${escapeHtml(action.label)}</span>
          <span class="button-description">${escapeHtml(action.description)}</span>
        </button>
      `).join('')}
    </div>
  </section>

  <section class="card">
    <h2>Diagnostics</h2>
    <pre>${escapeHtml(report)}</pre>
  </section>

  <script nonce="${nonce}">
    const vscode = acquireVsCodeApi();
    for (const button of document.querySelectorAll('button[data-command]')) {
      button.addEventListener('click', () => {
        vscode.postMessage({
          type: 'run-command',
          commandId: button.dataset.command,
        });
      });
    }
  </script>
</body>
</html>`;
}

function buildDashboardRecommendation(
  status: WorkspaceStatus | undefined,
): DashboardRecommendation | undefined {
  if (!status) {
    return {
      title: 'Open a Unity project',
      detail: 'Select a workspace folder so Unity MCP can inspect the project and suggest setup steps.',
    };
  }

  const [nextAction] = status.recommendedActions;
  if (!nextAction) {
    return {
      title: 'Unity MCP is ready',
      detail: 'Use the dashboard or status bar to open Unity, reconfigure MCP transport, or inspect logs.',
    };
  }

  switch (nextAction) {
    case 'trust-workspace':
      return {
        title: 'Trust this workspace',
        detail: 'Unity MCP only enables write and launch actions after VS Code trusts the current workspace.',
      };
    case 'install-plugin':
      return {
        title: 'Install the Unity MCP plugin',
        detail: 'Add the Unity package to Packages/manifest.json before trying to launch or connect with MCP.',
      };
    case 'open-unity-without-mcp':
      if (status.unityMcpProjectConfigExists) {
        return {
          title: 'Repair the Unity project config',
          detail: 'Open Unity without MCP and fix or regenerate AI-Game-Developer-Config.json before retrying connected launch.',
        };
      }

      return {
        title: 'Initialize the Unity project',
        detail: 'Open Unity once without MCP so the newly installed package can import and create its project config.',
      };
    case 'configure-vscode-mcp':
      return {
        title: 'Configure VS Code MCP',
        detail: 'Write or update .vscode/mcp.json so VS Code can connect to the Unity MCP server for this project.',
      };
    case 'open-unity-with-mcp':
      return {
        title: 'Open Unity with MCP',
        detail: 'The project looks ready. Launch Unity with the saved MCP connection settings.',
      };
  }
}

function dedupeActions(items: DashboardActionItem[]): DashboardActionItem[] {
  const seen = new Set<string>();
  const deduped: DashboardActionItem[] = [];

  for (const item of items) {
    if (seen.has(item.commandId)) {
      continue;
    }

    seen.add(item.commandId);
    deduped.push(item);
  }

  return deduped;
}

function createNonce(): string {
  return randomBytes(18).toString('base64url');
}

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
