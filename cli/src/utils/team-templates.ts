import * as path from 'path';

export interface TeamRoleTemplate {
  roleName: string;
  paneTitle: string;
  command: string;
  workingDirectory: string;
  readinessHint: string;
}

export interface TeamLayoutTemplate {
  name: string;
  templateVersion: string;
  windowName: string;
  verificationPolicy: string;
  roles: TeamRoleTemplate[];
}

const DEFAULT_TEMPLATE_VERSION = '1';
const DEFAULT_LAYOUT_NAME = 'default';

export function createDefaultTeamLayout(projectPath: string): TeamLayoutTemplate {
  const workingDirectory = path.resolve(projectPath);

  return {
    name: DEFAULT_LAYOUT_NAME,
    templateVersion: DEFAULT_TEMPLATE_VERSION,
    windowName: 'team',
    verificationPolicy: 'Session is ready when tmux panes exist and persisted state matches the live layout.',
    roles: [
      {
        roleName: 'leader',
        paneTitle: 'leader',
        command: 'shell',
        workingDirectory,
        readinessHint: 'Leader pane created and ready for operator commands.',
      },
      {
        roleName: 'builder',
        paneTitle: 'builder',
        command: 'shell',
        workingDirectory,
        readinessHint: 'Builder pane created and ready for implementation work.',
      },
      {
        roleName: 'verifier',
        paneTitle: 'verifier',
        command: 'shell',
        workingDirectory,
        readinessHint: 'Verifier pane created and ready for test and validation work.',
      },
      {
        roleName: 'notes',
        paneTitle: 'notes',
        command: 'shell',
        workingDirectory,
        readinessHint: 'Notes pane created and ready for logs or scratch notes.',
      },
    ],
  };
}

export function getTeamLayoutTemplate(layoutName: string | undefined, projectPath: string): TeamLayoutTemplate {
  const normalized = (layoutName ?? DEFAULT_LAYOUT_NAME).trim().toLowerCase();

  if (normalized !== DEFAULT_LAYOUT_NAME) {
    throw new Error(`Unknown team layout preset: ${layoutName}. Only "${DEFAULT_LAYOUT_NAME}" is available in milestone 1.`);
  }

  return createDefaultTeamLayout(projectPath);
}
