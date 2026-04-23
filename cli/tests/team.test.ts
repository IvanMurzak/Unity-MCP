import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { createDefaultTeamLayout } from '../src/utils/team-templates.js';
import {
  canTransitionTeamSessionStatus,
  createTeamSessionState,
  getTeamStateFilePath,
  listTeamSessionStates,
  parseTeamSessionState,
  readTeamSessionState,
  transitionTeamSessionState,
  writeTeamSessionState,
} from '../src/utils/team-state.js';
import { createTmuxAdapter, parseListPanesOutput, TmuxCommandError, type TmuxAdapter, type TmuxPaneSnapshot } from '../src/utils/tmux.js';
import {
  getTeamSessionStatus,
  launchTeamSession,
  listTeamSessions,
  stopTeamSession,
  type TeamSessionListResult,
} from '../src/utils/team-orchestration.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(args: string[], options?: { cwd?: string }): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
      cwd: options?.cwd,
    });
    return { stdout, exitCode: 0 };
  } catch (err: unknown) {
    const error = err as { stdout?: string; stderr?: string; status?: number };
    return {
      stdout: (error.stdout ?? '') + (error.stderr ?? ''),
      exitCode: error.status ?? 1,
    };
  }
}

function makeFakeUnityProject(dir: string): void {
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  fs.mkdirSync(path.join(dir, 'ProjectSettings'), { recursive: true });
  fs.writeFileSync(path.join(dir, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 6000.0.0f1\n');
}

class FakeTmux implements TmuxAdapter {
  readonly sessions = new Map<string, TmuxPaneSnapshot[]>();
  readonly killedSessions: string[] = [];
  private nextPaneNumber = 1;

  ensureAvailable(): void {
    // no-op
  }

  hasSession(sessionName: string): boolean {
    return this.sessions.has(sessionName);
  }

  createSession(sessionName: string, workingDirectory: string): string {
    const paneId = `%${this.nextPaneNumber++}`;
    this.sessions.set(sessionName, [{
      paneId,
      title: '',
      currentCommand: 'shell',
      currentPath: workingDirectory,
      active: true,
    }]);
    return paneId;
  }

  splitWindow(targetPaneId: string, workingDirectory: string): string {
    const session = this.findSessionByPaneId(targetPaneId);
    if (!session) {
      throw new Error(`Unknown pane: ${targetPaneId}`);
    }

    const paneId = `%${this.nextPaneNumber++}`;
    const panes = this.sessions.get(session)!;
    panes.push({
      paneId,
      title: '',
      currentCommand: 'shell',
      currentPath: workingDirectory,
      active: false,
    });
    return paneId;
  }

  setPaneTitle(targetPaneId: string, title: string): void {
    const session = this.findSessionByPaneId(targetPaneId);
    if (!session) {
      throw new Error(`Unknown pane: ${targetPaneId}`);
    }
    const panes = this.sessions.get(session)!;
    const pane = panes.find(entry => entry.paneId === targetPaneId);
    if (pane) {
      pane.title = title;
    }
  }

  selectLayout(): void {
    // no-op for tests
  }

  listPanes(sessionName: string): TmuxPaneSnapshot[] {
    return [...(this.sessions.get(sessionName) ?? [])].map(pane => ({ ...pane }));
  }

  killSession(sessionName: string): void {
    this.killedSessions.push(sessionName);
    this.sessions.delete(sessionName);
  }

  private findSessionByPaneId(targetPaneId: string): string | undefined {
    for (const [sessionName, panes] of this.sessions.entries()) {
      if (panes.some(pane => pane.paneId === targetPaneId)) {
        return sessionName;
      }
    }
    return undefined;
  }
}

describe('team command registration', () => {
  it('lists the team command in global help', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('team');
  });

  it('shows team subcommands in help', () => {
    const { stdout, exitCode } = runCli(['team', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('launch');
    expect(stdout).toContain('list');
    expect(stdout).toContain('status');
    expect(stdout).toContain('stop');
  });
});

describe('team state helpers', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-team-state-'));
    makeFakeUnityProject(tmpDir);
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('serializes and deserializes a team session state', () => {
    const state = createTeamSessionState({
      sessionId: 'demo-session',
      projectPath: tmpDir,
      launcherVersion: '0.66.0',
      tmuxSessionName: 'demo-session',
      layoutPreset: 'default',
      verificationPolicy: 'ready when panes exist',
      templateVersion: '1',
      roles: createDefaultTeamLayout(tmpDir).roles,
    });

    writeTeamSessionState(tmpDir, state);
    const loaded = readTeamSessionState(tmpDir, 'demo-session');

    expect(loaded.sessionId).toBe('demo-session');
    expect(loaded.roles).toHaveLength(4);
    expect(loaded.roles.every(role => role.status === 'pending')).toBe(true);
  });

  it('rejects malformed session state payloads', () => {
    expect(() => parseTeamSessionState('{"schemaVersion":1,"status":"ready"}', 'broken.json'))
      .toThrowError(/roles/);
  });

  it('validates supported status transitions', () => {
    expect(canTransitionTeamSessionStatus('launching', 'ready')).toBe(true);
    expect(canTransitionTeamSessionStatus('ready', 'stopped')).toBe(true);
    expect(canTransitionTeamSessionStatus('stopped', 'ready')).toBe(false);

    const state = createTeamSessionState({
      sessionId: 'demo-session',
      projectPath: tmpDir,
      launcherVersion: '0.66.0',
      tmuxSessionName: 'demo-session',
      layoutPreset: 'default',
      verificationPolicy: 'ready when panes exist',
      templateVersion: '1',
      roles: createDefaultTeamLayout(tmpDir).roles,
    });
    const ready = transitionTeamSessionState(state, 'ready');
    expect(ready.status).toBe('ready');
  });
});

describe('tmux adapter', () => {
  it('builds the expected tmux arguments', () => {
    const calls: string[][] = [];
    const adapter = createTmuxAdapter((args) => {
      calls.push(args);
      if (args[0] === 'new-session') return '%1';
      if (args[0] === 'split-window') return '%2';
      if (args[0] === 'list-panes') return '%1\tleader\tshell\t/tmp\t1';
      return '';
    });

    adapter.ensureAvailable();
    adapter.createSession('demo', '/tmp/project', 'team');
    adapter.splitWindow('%1', '/tmp/project', 'horizontal');
    adapter.setPaneTitle('%1', 'leader');
    adapter.selectLayout('demo', 'tiled');
    adapter.listPanes('demo');
    adapter.killSession('demo');

    expect(calls).toEqual([
      ['-V'],
      ['new-session', '-d', '-P', '-F', '#{pane_id}', '-s', 'demo', '-n', 'team', '-c', '/tmp/project'],
      ['split-window', '-h', '-P', '-F', '#{pane_id}', '-t', '%1', '-c', '/tmp/project'],
      ['select-pane', '-t', '%1', '-T', 'leader'],
      ['select-layout', '-t', 'demo:0', 'tiled'],
      ['list-panes', '-t', 'demo:0', '-F', '#{pane_id}\t#{pane_title}\t#{pane_current_command}\t#{pane_current_path}\t#{pane_active}'],
      ['kill-session', '-t', 'demo'],
    ]);
  });

  it('parses list-panes output into typed pane records', () => {
    expect(parseListPanesOutput('%1\tleader\tshell\t/tmp\t1\n%2\tbuilder\tnode\t/tmp\t0')).toEqual([
      { paneId: '%1', title: 'leader', currentCommand: 'shell', currentPath: '/tmp', active: true },
      { paneId: '%2', title: 'builder', currentCommand: 'node', currentPath: '/tmp', active: false },
    ]);
  });

  it('surfaces a clear missing-tmux error', () => {
    const adapter = createTmuxAdapter(() => {
      throw new TmuxCommandError('tmux is required for team orchestration. Install tmux and ensure it is available on PATH.', 'missing');
    });

    expect(() => adapter.ensureAvailable()).toThrowError(/tmux is required/);
  });
});

describe('team templates', () => {
  it('expands the default layout into deterministic role panes', () => {
    const layout = createDefaultTeamLayout('/tmp/project');
    expect(layout.name).toBe('default');
    expect(layout.roles.map(role => role.roleName)).toEqual(['leader', 'builder', 'verifier', 'notes']);
    expect(layout.roles.map(role => role.paneTitle)).toEqual(['leader', 'builder', 'verifier', 'notes']);
    expect(layout.roles.every(role => role.command === 'shell')).toBe(true);
    expect(layout.roles.every(role => role.workingDirectory === '/tmp/project')).toBe(true);
  });
});

describe('team lifecycle with mocked tmux', () => {
  let tmpDir: string;
  let tmux: FakeTmux;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-team-lifecycle-'));
    makeFakeUnityProject(tmpDir);
    tmux = new FakeTmux();
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('launches a session, writes state, and marks it ready', () => {
    const state = launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    expect(state.status).toBe('ready');
    expect(state.roles).toHaveLength(4);
    expect(state.roles.every(role => role.status === 'ready')).toBe(true);
    expect(fs.existsSync(getTeamStateFilePath(tmpDir, 'alpha-team'))).toBe(true);

    const loaded = readTeamSessionState(tmpDir, 'alpha-team');
    expect(loaded.tmuxSessionName).toBe('alpha-team');
    expect(loaded.roles.map(role => role.paneTitle)).toEqual(['leader', 'builder', 'verifier', 'notes']);
  });

  it('reconciles status to degraded when tmux panes disappear', () => {
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    tmux.sessions.set('alpha-team', tmux.listPanes('alpha-team').slice(0, 2));

    const inspection = getTeamSessionStatus(tmpDir, tmux, 'alpha-team');

    expect(inspection.state.status).toBe('degraded');
    expect(inspection.issues.some(issue => issue.includes('missing pane for role verifier'))).toBe(true);
  });

  it('lists sessions and skips corrupted state files without crashing', () => {
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    const corruptedPath = path.join(tmpDir, '.unity-mcp', 'team-state', 'broken.json');
    fs.writeFileSync(corruptedPath, '{not-json}\n');

    const result: TeamSessionListResult = listTeamSessions(tmpDir, tmux);

    expect(result.inspections).toHaveLength(1);
    expect(result.inspections[0].state.sessionId).toBe('alpha-team');
    expect(result.invalid).toHaveLength(1);
    expect(result.invalid[0].filePath).toBe(corruptedPath);
  });

  it('stops an active session and marks saved state as stopped', () => {
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    const stopped = stopTeamSession(tmpDir, tmux, 'alpha-team');

    expect(stopped.status).toBe('stopped');
    expect(stopped.roles.every(role => role.status === 'stopped')).toBe(true);
    expect(tmux.killedSessions).toEqual(['alpha-team']);
  });

  it('records degraded launch state when pane creation fails mid-launch', () => {
    const failingTmux = new FakeTmux();
    const originalSplitWindow = failingTmux.splitWindow.bind(failingTmux);
    let splitCalls = 0;
    failingTmux.splitWindow = (targetPaneId, workingDirectory, orientation) => {
      splitCalls += 1;
      if (splitCalls === 2) {
        throw new Error('split failed');
      }
      return originalSplitWindow(targetPaneId, workingDirectory, orientation);
    };

    expect(() => launchTeamSession(tmpDir, '0.66.0', failingTmux, { sessionName: 'broken-team' }, new Date('2026-04-23T05:00:00.000Z')))
      .toThrowError(/split failed/);

    const loaded = readTeamSessionState(tmpDir, 'broken-team');
    expect(loaded.status).toBe('degraded');
    expect(loaded.notes.some(note => note.includes('Launch failed: split failed'))).toBe(true);
  });

  it('returns saved sessions when tmux is unavailable during list', () => {
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    const unavailableTmux: TmuxAdapter = {
      ensureAvailable: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      hasSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      createSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      splitWindow: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      setPaneTitle: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      selectLayout: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      listPanes: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      killSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
    };

    const result = listTeamSessions(tmpDir, unavailableTmux);

    expect(result.inspections).toHaveLength(1);
    expect(result.tmuxUnavailableMessage).toContain('tmux is required');
  });

  it('lists saved team state files newest-first', () => {
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    launchTeamSession(tmpDir, '0.66.0', tmux, { sessionName: 'beta-team' }, new Date('2026-04-23T06:00:00.000Z'));

    const listed = listTeamSessionStates(tmpDir);
    expect(listed.sessions.map(session => session.sessionId)).toEqual(['beta-team', 'alpha-team']);
  });
});
