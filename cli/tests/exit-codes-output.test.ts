import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { spawnSync } from 'child_process';
import { CLI_PATH } from './helpers/cli.js';
import { formatEnvelope } from '../src/utils/output.js';
import { EXIT_CODE_DESCRIPTIONS, ExitCode } from '../src/utils/exit-codes.js';
import { classifyToolFailure } from '../src/commands/run-tool-builder.js';
import { listEditorDialogs, parseDialogList, pressDialogScript, pressEditorDialogButton } from '../src/utils/editor-dialogs.js';
import type { RunToolFailure } from '../src/lib/types.js';

/** Run the built CLI; stdout and stderr apart (the envelope must be alone on stdout). */
function runCli(args: string[]): { stdout: string; stderr: string; exitCode: number } {
  const r = spawnSync('node', [CLI_PATH, ...args], { encoding: 'utf-8', timeout: 20_000 });
  return { stdout: r.stdout, stderr: r.stderr, exitCode: r.status ?? 1 };
}

function failure(extra: Partial<RunToolFailure>): RunToolFailure {
  return { kind: 'failure', success: false, endpoint: 'http://x', reason: 'unknown', message: 'm', ...extra };
}

describe('exit codes', () => {
  it('match the table shared with unreal-mcp-cli and godot-cli', () => {
    expect(ExitCode).toEqual({ Success: 0, Error: 1, Usage: 2, CommandFailed: 6, Unreachable: 7, TestsFailed: 8, Timeout: 14, Busy: 17, AwaitingInput: 18 });
    expect(EXIT_CODE_DESCRIPTIONS.map(e => e.code)).toEqual([0, 1, 2, 6, 7, 8, 14, 17, 18]);
  });

  it('classify tool failures', () => {
    expect(classifyToolFailure(failure({ reason: 'timeout' }))).toEqual({ exitCode: 14, code: 'TIMEOUT' });
    expect(classifyToolFailure(failure({ reason: 'connection-refused' }))).toEqual({ exitCode: 7, code: 'UNREACHABLE' });
    expect(classifyToolFailure(failure({ reason: 'http-error', httpStatus: 503 }))).toEqual({ exitCode: 17, code: 'BUSY' });
    expect(classifyToolFailure(failure({ reason: 'http-error', httpStatus: 404 }))).toEqual({ exitCode: 2, code: 'NOT_FOUND' });
    expect(classifyToolFailure(failure({ reason: 'http-error', httpStatus: 500 }))).toEqual({ exitCode: 6, code: 'TOOL_FAILED' });
    expect(classifyToolFailure(failure({ reason: 'invalid-input' }))).toEqual({ exitCode: 2, code: 'USAGE' });
  });
});

describe('envelope', () => {
  it('json is pretty, ndjson is one line', () => {
    const env = { success: true, command: 'x', data: { a: 1 }, errors: [], warnings: [] };
    expect(formatEnvelope(env, 'json')).toBe(JSON.stringify(env, null, 2) + '\n');
    expect(formatEnvelope(env, 'ndjson')).toBe(JSON.stringify(env) + '\n');
  });
});

describe('editor dialogs', () => {
  it('is Windows-only', async () => {
    expect(await listEditorDialogs([1], 'linux')).toEqual({ kind: 'unsupported', platform: 'linux' });
    expect(await pressEditorDialogButton([1], 'OK', undefined, 'darwin')).toEqual({ kind: 'unsupported', platform: 'darwin' });
  });

  it('parses the helper output', () => {
    const json = '[{"id":"123","pid":5,"title":"Hold On","message":"Save?","buttons":["Save","Don\'t Save"]}]';
    expect(parseDialogList(json)).toEqual({ kind: 'ok', dialogs: [{ id: '123', pid: 5, title: 'Hold On', message: 'Save?', buttons: ['Save', "Don't Save"] }] });
    expect(parseDialogList('error:boom')).toEqual({ kind: 'error', message: 'boom' });
    expect(parseDialogList('garbage').kind).toBe('error');
  });

  it('never runs the helper without a valid pid', async () => {
    let ran = false;
    expect(await listEditorDialogs([0, -1], 'win32', async () => { ran = true; return '[]'; })).toEqual({ kind: 'ok', dialogs: [] });
    expect(ran).toBe(false);
  });

  it('quotes the button label as a PowerShell literal', () => {
    expect(pressDialogScript([5], 0n, "Don't $(evil)")).toContain("'Don''t $(evil)'");
  });

  it('rejects a non-numeric dialog id', async () => {
    expect(await pressEditorDialogButton([5], 'OK', '12; rm', 'win32', async () => 'pressed')).toMatchObject({ kind: 'error' });
  });
});

describe('CLI exit codes and formats', () => {
  it('an unknown option is a usage error (2), --help stays 0', () => {
    expect(runCli(['status', '--no-such-flag']).exitCode).toBe(2);
    expect(runCli(['status', '--help']).exitCode).toBe(0);
  });

  it('an unknown --format is a usage error', () => {
    const { exitCode, stderr } = runCli(['commands', '--format', 'yaml']);
    expect(exitCode).toBe(2);
    expect(stderr).toContain('--format must be one of');
  });

  it('commands --format json lists every command from the commander tree', () => {
    const { exitCode, stdout } = runCli(['commands', '--format', 'json']);
    expect(exitCode).toBe(0);
    const envelope = JSON.parse(stdout);
    expect(envelope).toMatchObject({ success: true, command: 'commands', errors: [] });
    const names = envelope.data.commands.map((c: { name: string }) => c.name);
    expect(names).toEqual(expect.arrayContaining(['test', 'compile', 'dialog', 'run-tool', 'exec', 'status', 'commands']));
    const test = envelope.data.commands.find((c: { name: string }) => c.name === 'test');
    expect(test.options.map((o: { flags: string }) => o.flags)).toEqual(expect.arrayContaining(['--mode <mode>', '--junit-output <file>', '--format <format>']));
    expect(envelope.data.exitCodes.map((e: { code: number }) => e.code)).toEqual([0, 1, 2, 6, 7, 8, 14, 17, 18]);
  });

  it('status --format json on an unreachable project: one envelope on stdout, exit 7', () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-status-json-'));
    try {
      fs.mkdirSync(path.join(dir, 'Assets'));
      const { exitCode, stdout } = runCli(['status', dir, '--format', 'json', '--timeout', '300']);
      expect(exitCode).toBe(7);
      const envelope = JSON.parse(stdout);
      expect(envelope).toMatchObject({ success: false, command: 'status', errors: [{ code: 'UNREACHABLE' }] });
      expect(envelope.data.editor).toEqual({ running: false });
      expect(envelope.data.ready).toBe(false);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('test rejects a bad --mode and a reserved editor argument as usage errors', () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-test-cli-'));
    try {
      fs.mkdirSync(path.join(dir, 'Assets'));
      expect(runCli(['test', dir, '--mode', 'Sideways']).exitCode).toBe(2);
      const { exitCode, stdout } = runCli(['test', dir, '--format', 'json', '--', '-batchmode']);
      expect(exitCode).toBe(2);
      expect(JSON.parse(stdout).errors[0].code).toBe('USAGE');
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});
