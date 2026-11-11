import { createServer, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { classifyToolFailure } from '@baizor/gamedev-cli-core';
import { runTool, runSystemTool } from '../src/lib/run-tool.js';

// A server that takes the call and then drops the connection: before the headers, in the middle of the body (of a 200
// and of an error answer), or with a TCP reset. Every one is `connection-reset` (exit 7 UNREACHABLE), as in the other
// two engine CLIs, never `unknown` (exit 1) and never an empty answer read as a success.
describe('runTool on a dropped connection', () => {
  let server: Server;
  let base: string;

  beforeAll(async () => {
    server = createServer((req, res) => {
      req.resume();
      req.on('end', () => {
        const mode = req.url!.split('/').pop();
        if (mode === 'before-headers') return void req.socket.destroy();
        if (mode === 'rst') return void req.socket.resetAndDestroy();
        res.writeHead(mode === 'mid-error-body' ? 500 : 200, { 'content-type': 'application/json', 'content-length': '200' });
        res.write('{"status":"succ');
        setTimeout(() => req.socket.destroy(), 20);
      });
    });
    await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
    base = `http://127.0.0.1:${(server.address() as AddressInfo).port}`;
  });
  afterAll(() => new Promise<void>((resolve) => server.close(() => resolve())));

  for (const mode of ['before-headers', 'mid-body', 'mid-error-body', 'rst']) {
    it(`${mode}: connection-reset, exit 7 UNREACHABLE`, async () => {
      const result = await runTool({ toolName: mode, url: base, input: {}, timeoutMs: 5_000 });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') return;
      expect(result.reason).toBe('connection-reset');
      expect(classifyToolFailure(result)).toEqual({ exitCode: 7, code: 'UNREACHABLE' });
    });
  }

  it('run-system-tool too', async () => {
    const result = await runSystemTool({ toolName: 'mid-body', url: base, input: {}, timeoutMs: 5_000 });
    expect(result.kind === 'failure' && result.reason).toBe('connection-reset');
  });
});
