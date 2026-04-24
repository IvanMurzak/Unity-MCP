import type {
  TeamRuntimeAdapter,
  TeamRuntimeLaunchRequest,
  TeamRuntimeLaunchResult,
  TeamRuntimeRoleInspection,
  TeamRuntimeSelection,
  TeamRuntimeSessionInspection,
} from './team-runtime.js';
import type { TmuxAdapter, TmuxPaneSnapshot } from './tmux.js';

function paneToRoleInspection(pane: TmuxPaneSnapshot): TeamRuntimeRoleInspection {
  return {
    roleName: pane.title || pane.paneId,
    runtimeHandle: pane.paneId,
    displayName: pane.title || pane.paneId,
    status: 'ready',
    workingDirectory: pane.currentPath,
    currentCommand: pane.currentCommand,
  };
}

export function createTmuxTeamRuntime(
  tmux: TmuxAdapter,
  selection: TeamRuntimeSelection,
): TeamRuntimeAdapter {
  return {
    kind: 'tmux',
    displayName: 'tmux',
    selection,

    ensureAvailable(): void {
      tmux.ensureAvailable();
    },

    capabilities() {
      return {
        runtimeKind: 'tmux' as const,
        paneTitles: true,
        roleHandles: true,
        splitLayout: true,
        sessionListing: false,
      };
    },

    hasSession(sessionHandle: string): boolean {
      return tmux.hasSession(sessionHandle);
    },

    launchSession(request: TeamRuntimeLaunchRequest): TeamRuntimeLaunchResult {
      const firstPaneId = tmux.createSession(request.sessionId, request.projectPath, request.windowName);
      const paneIds = [
        firstPaneId,
        ...request.roles.slice(1).map((_, index) =>
          tmux.splitWindow(firstPaneId, request.projectPath, index === 0 ? 'horizontal' : 'vertical')),
      ];

      tmux.selectLayout(request.sessionId, 'tiled');

      const roles = request.roles.map((role, index) => {
        const runtimeHandle = paneIds[index] ?? '';
        if (!runtimeHandle) {
          throw new Error(`Unable to assign tmux pane for role ${role.roleName}`);
        }
        tmux.setPaneTitle(runtimeHandle, role.paneTitle);
        return {
          roleName: role.roleName,
          runtimeHandle,
          displayName: role.paneTitle,
        };
      });

      return {
        sessionHandle: request.sessionId,
        roles,
        notes: selection.fallbackReason ? [selection.fallbackReason] : [],
      };
    },

    inspectSession(sessionHandle: string): TeamRuntimeSessionInspection {
      if (!tmux.hasSession(sessionHandle)) {
        return {
          sessionHandle,
          available: false,
          roles: [],
          issues: [`tmux session ${sessionHandle} is not available`],
        };
      }

      const panes = tmux.listPanes(sessionHandle);
      return {
        sessionHandle,
        available: true,
        roles: panes.map(paneToRoleInspection),
        issues: [],
      };
    },

    stopSession(sessionHandle: string): void {
      tmux.killSession(sessionHandle);
    },
  };
}
