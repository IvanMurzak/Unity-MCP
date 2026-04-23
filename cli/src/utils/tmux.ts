import { execFileSync } from 'child_process';

const LIST_PANES_FORMAT = '#{pane_id}\t#{pane_title}\t#{pane_current_command}\t#{pane_current_path}\t#{pane_active}';

export interface TmuxPaneSnapshot {
  paneId: string;
  title: string;
  currentCommand: string;
  currentPath: string;
  active: boolean;
}

export interface TmuxAdapter {
  ensureAvailable(): void;
  hasSession(sessionName: string): boolean;
  createSession(sessionName: string, workingDirectory: string, windowName: string): string;
  splitWindow(targetPaneId: string, workingDirectory: string, orientation: 'horizontal' | 'vertical'): string;
  setPaneTitle(targetPaneId: string, title: string): void;
  selectLayout(sessionName: string, layoutName: string): void;
  listPanes(sessionName: string): TmuxPaneSnapshot[];
  killSession(sessionName: string): void;
}

export type TmuxExec = (args: string[]) => string;

export class TmuxCommandError extends Error {
  readonly kind: 'missing' | 'failed';

  constructor(message: string, kind: 'missing' | 'failed') {
    super(message);
    this.name = 'TmuxCommandError';
    this.kind = kind;
  }
}

function normalizeTmuxError(err: unknown, command: string): TmuxCommandError {
  const error = err as NodeJS.ErrnoException & { status?: number; stderr?: string | Buffer };
  if (error?.code === 'ENOENT') {
    return new TmuxCommandError('tmux is required for team orchestration. Install tmux and ensure it is available on PATH.', 'missing');
  }

  const stderr = typeof error?.stderr === 'string'
    ? error.stderr.trim()
    : Buffer.isBuffer(error?.stderr)
      ? error.stderr.toString('utf-8').trim()
      : '';

  const details = stderr.length > 0 ? `: ${stderr}` : '';
  return new TmuxCommandError(`tmux ${command} failed${details}`, 'failed');
}

function defaultExec(args: string[]): string {
  try {
    return execFileSync('tmux', args, {
      encoding: 'utf-8',
      stdio: ['ignore', 'pipe', 'pipe'],
    }).trim();
  } catch (err) {
    throw normalizeTmuxError(err, args[0] ?? 'command');
  }
}

export function parseListPanesOutput(output: string): TmuxPaneSnapshot[] {
  return output
    .split('\n')
    .map(line => line.trim())
    .filter(Boolean)
    .map(line => {
      const [paneId = '', title = '', currentCommand = '', currentPath = '', active = '0'] = line.split('\t');
      if (!paneId) {
        throw new TmuxCommandError('tmux list-panes returned malformed pane data', 'failed');
      }

      return {
        paneId,
        title,
        currentCommand,
        currentPath,
        active: active === '1',
      };
    });
}

export function createTmuxAdapter(exec: TmuxExec = defaultExec): TmuxAdapter {
  return {
    ensureAvailable(): void {
      exec(['-V']);
    },

    hasSession(sessionName: string): boolean {
      try {
        exec(['has-session', '-t', sessionName]);
        return true;
      } catch (err) {
        if (err instanceof TmuxCommandError && err.kind === 'missing') {
          throw err;
        }
        return false;
      }
    },

    createSession(sessionName: string, workingDirectory: string, windowName: string): string {
      return exec([
        'new-session',
        '-d',
        '-P',
        '-F',
        '#{pane_id}',
        '-s',
        sessionName,
        '-n',
        windowName,
        '-c',
        workingDirectory,
      ]);
    },

    splitWindow(targetPaneId: string, workingDirectory: string, orientation: 'horizontal' | 'vertical'): string {
      return exec([
        'split-window',
        orientation === 'horizontal' ? '-h' : '-v',
        '-P',
        '-F',
        '#{pane_id}',
        '-t',
        targetPaneId,
        '-c',
        workingDirectory,
      ]);
    },

    setPaneTitle(targetPaneId: string, title: string): void {
      exec(['select-pane', '-t', targetPaneId, '-T', title]);
    },

    selectLayout(sessionName: string, layoutName: string): void {
      exec(['select-layout', '-t', `${sessionName}:0`, layoutName]);
    },

    listPanes(sessionName: string): TmuxPaneSnapshot[] {
      return parseListPanesOutput(exec(['list-panes', '-t', `${sessionName}:0`, '-F', LIST_PANES_FORMAT]));
    },

    killSession(sessionName: string): void {
      exec(['kill-session', '-t', sessionName]);
    },
  };
}
