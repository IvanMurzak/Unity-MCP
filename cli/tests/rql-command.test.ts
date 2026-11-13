// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { afterEach, describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as http from 'http';
import * as net from 'net';
import * as os from 'os';
import * as path from 'path';
import { runCliAsync } from './helpers/cli.js';

const dirs: string[] = [];
afterEach(() => {
  for (const dir of dirs.splice(0)) fs.rmSync(dir, { recursive: true, force: true });
});

function project(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'rql-command-'));
  dirs.push(dir);
  fs.mkdirSync(path.join(dir, 'Assets'));
  fs.mkdirSync(path.join(dir, 'ProjectSettings'));
  fs.writeFileSync(path.join(dir, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 1.0.0f1\n');
  return dir;
}

interface Captured { url: string; body: unknown }

/** A fake plugin HTTP API: records each request, runs `onRequest` (e.g. writes the files a save would) and answers. */
function fakeEditor(answer: object, onRequest?: () => void): Promise<{ port: number; requests: Captured[]; close: () => Promise<void> }> {
  const requests: Captured[] = [];
  const server = http.createServer((req, res) => {
    let data = '';
    req.on('data', (chunk: Buffer) => { data += chunk.toString(); });
    req.on('end', () => {
      requests.push({ url: req.url ?? '', body: data ? JSON.parse(data) : null });
      onRequest?.();
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

describe('rql with several statements', () => {
  it('sends a change of several statements to rql-mutate, tool names stripped', async () => {
    const dir = project();
    const editor = await fakeEditor({ status: 'success', content: [{ type: 'text', text: '[1/2] ...\n\n[2/2] ...' }] });
    try {
      const query = 'rql-mutate objects | where name == "A" | set active = false; rql-mutate editor | call CreateGameObject("B")';
      const { stdout, exitCode } = await runCliAsync(['rql', query, '--path', dir, '--url', `http://127.0.0.1:${editor.port}`]);
      expect(exitCode, stdout).toBe(0);
      const tools = editor.requests.filter(r => r.url.startsWith('/api/tools/'));   // after the readiness ping
      expect(tools.map(r => r.url)).toEqual(['/api/tools/rql-mutate']);
      expect(tools[0].body).toEqual({ query: 'objects | where name == "A" | set active = false; editor | call CreateGameObject("B")' });
    } finally {
      await editor.close();
    }
  });

  it('lists the files a failed change still wrote (the statements before the failure stay)', async () => {
    const dir = project();
    const scene = path.join(dir, 'Assets', 'Forest.unity');
    const editor = await fakeEditor(
      { status: 'error', content: [{ type: 'text', text: '[1/2] ...\n\n[2/2] ...\nerror unknown-member' }] },
      () => fs.writeFileSync(scene, 'saved'));
    try {
      const { stdout, exitCode } = await runCliAsync(['rql', 'objects | set active = false; objects | set nosuch = 1', '--path', dir, '--url', `http://127.0.0.1:${editor.port}`]);
      expect(exitCode, stdout).not.toBe(0);
      expect(stdout).toContain('Saved to disk: Assets/Forest.unity');
    } finally {
      await editor.close();
    }
  });
});
