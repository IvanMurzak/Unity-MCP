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
import {
  createTeamRuntime,
  resolveTeamRuntimeSelection,
  type TeamRuntimeAdapter,
  type TeamRuntimeRoleInspection,
} from '../src/utils/team-runtime.js';
import { createProcessTeamRuntime } from '../src/utils/team-runtime-process.js';
import { createTmuxTeamRuntime } from '../src/utils/team-runtime-tmux.js';
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

class FakeProcessRuntime implements TeamRuntimeAdapter {
  readonly kind = 'process' as const;
  readonly displayName = 'process';
  readonly selection = {
    requestedKind: 'process' as const,
    preferredKind: 'process' as const,
    resolvedKind: 'tmux' as const,
    fallbackReason: 'test double only',
  };

  readonly sessions = new Map<string, TeamRuntimeRoleInspection[]>();
  readonly stoppedSessions: string[] = [];

  ensureAvailable(): void {
    // no-op
  }

  capabilities() {
    return {
      runtimeKind: 'process' as const,
      paneTitles: false,
      roleHandles: false,
      splitLayout: false,
      sessionListing: false,
    };
  }

  hasSession(sessionHandle: string): boolean {
    return this.sessions.has(sessionHandle);
  }

  launchSession(request: { sessionId: string; roles: { roleName: string; workingDirectory: string; command: string }[] }): { sessionHandle: string; roles: { roleName: string; runtimeHandle: string; displayName: string }[] } {
    const roles = request.roles.map(role => ({
      roleName: role.roleName,
      runtimeHandle: '',
      displayName: role.roleName,
      status: 'ready' as const,
      workingDirectory: role.workingDirectory,
      currentCommand: role.command,
    }));
    this.sessions.set(request.sessionId, roles);

    return {
      sessionHandle: request.sessionId,
      roles: request.roles.map(role => ({
        roleName: role.roleName,
        runtimeHandle: '',
        displayName: role.roleName,
      })),
    };
  }

  inspectSession(sessionHandle: string) {
    return {
      sessionHandle,
      available: this.sessions.has(sessionHandle),
      roles: [...(this.sessions.get(sessionHandle) ?? [])],
      issues: this.sessions.has(sessionHandle) ? [] : [`process session ${sessionHandle} is not available`],
    };
  }

  stopSession(sessionHandle: string): void {
    this.stoppedSessions.push(sessionHandle);
    this.sessions.delete(sessionHandle);
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

  it('serializes and deserializes a backend-neutral team session state', () => {
    const state = createTeamSessionState({
      sessionId: 'demo-session',
      projectPath: tmpDir,
      launcherVersion: '0.66.0',
      runtime: {
        kind: 'tmux',
        sessionHandle: 'demo-session',
      },
      layoutPreset: 'default',
      verificationPolicy: 'ready when roles exist',
      templateVersion: '1',
      roles: createDefaultTeamLayout(tmpDir).roles,
    });

    writeTeamSessionState(tmpDir, state);
    const loaded = readTeamSessionState(tmpDir, 'demo-session');

    expect(loaded.sessionId).toBe('demo-session');
    expect(loaded.runtime.kind).toBe('tmux');
    expect(loaded.runtime.sessionHandle).toBe('demo-session');
    expect(loaded.roles).toHaveLength(4);
    expect(loaded.roles.every(role => role.status === 'pending')).toBe(true);
  });

  it('migrates schema-v1 tmux state into the runtime-neutral shape', () => {
    const legacy = {
      schemaVersion: 1,
      sessionId: 'legacy-session',
      projectPath: tmpDir,
      createdAt: '2026-04-24T00:00:00.000Z',
      updatedAt: '2026-04-24T00:00:00.000Z',
      launcherVersion: '0.66.0',
      tmuxSessionName: 'legacy-session',
      status: 'ready',
      layoutPreset: 'default',
      roles: createDefaultTeamLayout(tmpDir).roles.map((role, index) => ({
        ...role,
        paneId: `%${index + 1}`,
        status: 'ready',
      })),
      verificationPolicy: 'ready when panes exist',
      notes: [],
      templateVersion: '1',
    };

    const loaded = parseTeamSessionState(JSON.stringify(legacy), 'legacy.json');

    expect(loaded.schemaVersion).toBe(2);
    expect(loaded.runtime.kind).toBe('tmux');
    expect(loaded.runtime.sessionHandle).toBe('legacy-session');
    expect(loaded.roles[0].runtimeHandle).toBe('%1');
  });

  it('rejects malformed session state payloads', () => {
    expect(() => parseTeamSessionState('{"schemaVersion":2,"status":"ready"}', 'broken.json'))
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
      runtime: {
        kind: 'tmux',
        sessionHandle: 'demo-session',
      },
      layoutPreset: 'default',
      verificationPolicy: 'ready when roles exist',
      templateVersion: '1',
      roles: createDefaultTeamLayout(tmpDir).roles,
    });
    const ready = transitionTeamSessionState(state, 'ready');
    expect(ready.status).toBe('ready');
  });
});

describe('runtime selection', () => {
  it('makes Windows preference explicit instead of silently assuming tmux', () => {
    expect(resolveTeamRuntimeSelection('auto', 'win32')).toEqual({
      requestedKind: 'auto',
      preferredKind: 'process',
      resolvedKind: 'process',
    });
  });

  it('creates the currently implemented tmux runtime on supported platforms', () => {
    const runtime = createTeamRuntime('tmux');
    expect(runtime.kind).toBe('tmux');
    expect(runtime.capabilities().paneTitles).toBe(true);
  });

  it('creates the process runtime when requested explicitly', () => {
    const runtime = createTeamRuntime('process');
    expect(runtime.kind).toBe('process');
    expect(runtime.capabilities().paneTitles).toBe(false);
  });
});

describe('process runtime adapter', () => {
  it('persists launched role handles and inspects/stops them through metadata', () => {
    let nextPid = 1000;
    const alive = new Set<number>();
    const tmpRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-process-runtime-'));
    const runtime = createProcessTeamRuntime(resolveTeamRuntimeSelection('process', 'win32'), {
      metadataRoot: tmpRoot,
      launchRoleProcess: () => {
        const pid = ++nextPid;
        alive.add(pid);
        return { pid };
      },
      isProcessAlive: (pid) => alive.has(pid),
      terminateProcess: (pid) => { alive.delete(pid); },
    });

    const launched = runtime.launchSession({
      sessionId: 'process-session',
      projectPath: '/tmp/project',
      windowName: 'team',
      roles: createDefaultTeamLayout('/tmp/project').roles,
    });

    expect(launched.sessionHandle).toBe('process-session');
    expect(launched.roles).toHaveLength(4);
    expect(runtime.hasSession('process-session')).toBe(true);

    const inspection = runtime.inspectSession('process-session');
    expect(inspection.available).toBe(true);
    expect(inspection.roles.every(role => role.status === 'ready')).toBe(true);

    runtime.stopSession('process-session');

    expect(runtime.hasSession('process-session')).toBe(false);
    const stopped = runtime.inspectSession('process-session');
    expect(stopped.available).toBe(false);
    expect(stopped.issues[0]).toContain('process session process-session is not available');

    fs.rmSync(tmpRoot, { recursive: true, force: true });
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

describe('team lifecycle with mocked runtimes', () => {
  let tmpDir: string;
  let tmux: FakeTmux;
  let tmuxRuntime: TeamRuntimeAdapter;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-team-lifecycle-'));
    makeFakeUnityProject(tmpDir);
    tmux = new FakeTmux();
    tmuxRuntime = createTmuxTeamRuntime(tmux, resolveTeamRuntimeSelection('tmux'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('launches a session, writes backend-neutral state, and marks it ready', () => {
    const state = launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    expect(state.status).toBe('ready');
    expect(state.runtime.kind).toBe('tmux');
    expect(state.roles).toHaveLength(4);
    expect(state.roles.every(role => role.status === 'ready')).toBe(true);
    expect(fs.existsSync(getTeamStateFilePath(tmpDir, 'alpha-team'))).toBe(true);

    const loaded = readTeamSessionState(tmpDir, 'alpha-team');
    expect(loaded.runtime.sessionHandle).toBe('alpha-team');
    expect(loaded.roles.map(role => role.paneTitle)).toEqual(['leader', 'builder', 'verifier', 'notes']);
  });

  it('reconciles status to degraded when runtime roles disappear', () => {
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    tmux.sessions.set('alpha-team', tmux.listPanes('alpha-team').slice(0, 2));

    const inspection = getTeamSessionStatus(tmpDir, tmuxRuntime, 'alpha-team');

    expect(inspection.state.status).toBe('degraded');
    expect(inspection.issues.some(issue => issue.includes('missing pane for role verifier'))).toBe(true);
  });

  it('lists sessions and skips corrupted state files without crashing', () => {
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    const corruptedPath = path.join(tmpDir, '.unity-mcp', 'team-state', 'broken.json');
    fs.writeFileSync(corruptedPath, '{not-json}\n');

    const result: TeamSessionListResult = listTeamSessions(tmpDir, tmuxRuntime);

    expect(result.inspections).toHaveLength(1);
    expect(result.inspections[0].state.sessionId).toBe('alpha-team');
    expect(result.invalid).toHaveLength(1);
    expect(result.invalid[0].filePath).toBe(corruptedPath);
  });

  it('stops an active session and marks saved state as stopped', () => {
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    const stopped = stopTeamSession(tmpDir, tmuxRuntime, 'alpha-team');

    expect(stopped.status).toBe('stopped');
    expect(stopped.roles.every(role => role.status === 'stopped')).toBe(true);
    expect(tmux.killedSessions).toEqual(['alpha-team']);
  });

  it('records degraded launch state when runtime launch fails mid-launch', () => {
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

    expect(() => launchTeamSession(
      tmpDir,
      '0.66.0',
      createTmuxTeamRuntime(failingTmux, resolveTeamRuntimeSelection('tmux')),
      { sessionName: 'broken-team' },
      new Date('2026-04-23T05:00:00.000Z'),
    )).toThrowError(/split failed/);

    const loaded = readTeamSessionState(tmpDir, 'broken-team');
    expect(loaded.status).toBe('degraded');
    expect(loaded.notes.some(note => note.includes('Launch failed: split failed'))).toBe(true);
  });

  it('returns saved sessions when the runtime is unavailable during list', () => {
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));

    const unavailableRuntime: TeamRuntimeAdapter = {
      kind: 'tmux',
      displayName: 'tmux',
      selection: resolveTeamRuntimeSelection('tmux'),
      ensureAvailable: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      capabilities: () => ({ runtimeKind: 'tmux', paneTitles: true, roleHandles: true, splitLayout: true, sessionListing: false }),
      hasSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      launchSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      inspectSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
      stopSession: () => { throw new TmuxCommandError('tmux is required', 'missing'); },
    };

    const result = listTeamSessions(tmpDir, unavailableRuntime);

    expect(result.inspections).toHaveLength(1);
    expect(result.runtimeUnavailableMessage).toContain('tmux is required');
  });

  it('supports a runtime without pane handles by matching roles through the shared contract', () => {
    const processRuntime = new FakeProcessRuntime();

    const state = launchTeamSession(
      tmpDir,
      '0.66.0',
      processRuntime,
      { sessionName: 'process-team' },
      new Date('2026-04-24T00:00:00.000Z'),
    );
    expect(state.runtime.kind).toBe('process');
    expect(state.roles.every(role => role.runtimeHandle === '')).toBe(true);

    const inspection = getTeamSessionStatus(tmpDir, processRuntime, 'process-team');
    expect(inspection.state.status).toBe('ready');
    expect(inspection.state.roles.every(role => role.status === 'ready')).toBe(true);
  });

  it('lists saved team state files newest-first', () => {
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'alpha-team' }, new Date('2026-04-23T05:00:00.000Z'));
    launchTeamSession(tmpDir, '0.66.0', tmuxRuntime, { sessionName: 'beta-team' }, new Date('2026-04-23T06:00:00.000Z'));

    const listed = listTeamSessionStates(tmpDir);
    expect(listed.sessions.map(session => session.sessionId)).toEqual(['beta-team', 'alpha-team']);
  });
});
