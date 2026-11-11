// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { afterEach, describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as http from 'http';
import * as net from 'net';
import * as os from 'os';
import * as path from 'path';
import { runCliAsync } from './helpers/cli.js';
import { OLD_PLUGIN_ACCOUNT_HINT, UNITY_ACCOUNT_QUERY, UNITY_SIGN_IN_QUERY, rqlErrorHint, unityAccountToolCall } from '../src/lib/rql.js';

const dirs: string[] = [];
afterEach(() => {
  for (const dir of dirs.splice(0)) fs.rmSync(dir, { recursive: true, force: true });
});

/** A Unity project that names its editor version, so a command that may start an editor would try to. */
function project(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-account-'));
  dirs.push(dir);
  fs.mkdirSync(path.join(dir, 'Assets'));
  fs.mkdirSync(path.join(dir, 'ProjectSettings'));
  fs.writeFileSync(path.join(dir, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 1.0.0f1\n');
  return dir;
}

interface Captured { url: string; body: unknown }

/** A fake plugin HTTP API: records each request and answers with `answer`. */
function fakeEditor(answer: object): Promise<{ port: number; requests: Captured[]; close: () => Promise<void> }> {
  const requests: Captured[] = [];
  const server = http.createServer((req, res) => {
    let data = '';
    req.on('data', (chunk: Buffer) => { data += chunk.toString(); });
    req.on('end', () => {
      requests.push({ url: req.url ?? '', body: data ? JSON.parse(data) : null });
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(answer));
    });
  });
  return new Promise(resolve => server.listen(0, '127.0.0.1', () => resolve({
    port: (server.address() as net.AddressInfo).port,
    requests,
    close: () => new Promise<void>(done => server.close(() => done())),
  })));
}

describe('unity-account', () => {
  it('reads the account with rql-query and opens the sign-in with rql-mutate', () => {
    expect(unityAccountToolCall(false)).toEqual({ toolName: 'rql-query', input: { query: UNITY_ACCOUNT_QUERY } });
    expect(unityAccountToolCall(true)).toEqual({ toolName: 'rql-mutate', input: { query: UNITY_SIGN_IN_QUERY } });
    expect(UNITY_ACCOUNT_QUERY).toBe('assetStore | select signedIn userName');
    expect(UNITY_SIGN_IN_QUERY).toBe('assetStore | call SignIn()');
  });

  it('tells a plugin without the assetStore source to update', () => {
    expect(rqlErrorHint('rql-query', "error unknown-source: unknown source 'assetStore'")).toBe(OLD_PLUGIN_ACCOUNT_HINT);
    expect(rqlErrorHint('rql-mutate', "error unknown-member: unknown method 'SignIn' on AssetStoreEditorApi")).toBe(OLD_PLUGIN_ACCOUNT_HINT);
    expect(rqlErrorHint('rql-query', "error unknown-source: unknown source 'object'")).toBeUndefined();
  });

  it('asks the running editor and prints its answer', async () => {
    const editor = await fakeEditor({ status: 'success', content: [{ type: 'text', text: 'signedIn|userName\nfalse|""' }] });
    try {
      const { stdout, exitCode } = await runCliAsync(['unity-account', project(), '--url', `http://127.0.0.1:${editor.port}`]);
      expect(exitCode, stdout).toBe(0);
      expect(stdout).toContain('signedIn|userName');
      expect(editor.requests.map(r => r.url)).toEqual(['/api/tools/rql-query']);
      expect(editor.requests[0].body).toEqual({ query: UNITY_ACCOUNT_QUERY });
    } finally {
      await editor.close();
    }
  });

  it('--sign-in calls SignIn() in the running editor', async () => {
    const editor = await fakeEditor({ status: 'success', content: [{ type: 'text', text: "Unity's sign-in was opened for the user" }] });
    try {
      const { stdout, exitCode } = await runCliAsync(['unity-account', project(), '--sign-in', '--url', `http://127.0.0.1:${editor.port}`]);
      expect(exitCode, stdout).toBe(0);
      expect(stdout).toContain('sign-in was opened');
      expect(editor.requests.map(r => r.url)).toEqual(['/api/tools/rql-mutate']);
      expect(editor.requests[0].body).toEqual({ query: UNITY_SIGN_IN_QUERY });
    } finally {
      await editor.close();
    }
  });

  it('never starts an editor: no running one is an unreachable error', async () => {
    const closed = await fakeEditor({});
    const port = closed.port;
    await closed.close();
    const { stdout, exitCode } = await runCliAsync(['unity-account', project(), '--format', 'json', '--url', `http://127.0.0.1:${port}`]);
    expect(exitCode, stdout).toBe(7);
    expect(stdout).not.toMatch(/Starting|Launching|Unity Hub/i);
  });

  it('is listed apart from login', async () => {
    const { stdout } = await runCliAsync(['unity-account', '--help']);
    expect(stdout).toContain('--sign-in');
    expect(stdout).toContain('Not the ai-game.dev account');
  });
});
