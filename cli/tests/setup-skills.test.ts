// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as http from 'http';
import * as net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { runCliAsync } from './helpers/cli.js';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

interface CapturedRequest {
  url: string;
  method: string;
  body: unknown;
}

interface CaptureServer {
  port: number;
  waitForRequest: (timeoutMs?: number) => Promise<CapturedRequest>;
  close: () => Promise<void>;
}

/** Start a one-shot local HTTP server that captures a single POST request and responds 200. */
function startCaptureServer(): Promise<CaptureServer> {
  return new Promise((resolve) => {
    let requestResolve!: (v: CapturedRequest) => void;
    const requestPromise = new Promise<CapturedRequest>((res) => {
      requestResolve = res;
    });

    const server = http.createServer((req, res) => {
      let data = '';
      req.on('data', (chunk: Buffer) => { data += chunk.toString(); });
      req.on('end', () => {
        let body: unknown = null;
        try { body = JSON.parse(data); } catch { body = data; }
        requestResolve({ url: req.url ?? '', method: req.method ?? '', body });
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end('{}');
      });
    });

    server.listen(0, '127.0.0.1', () => {
      const addr = server.address() as net.AddressInfo;
      resolve({
        port: addr.port,
        waitForRequest: (timeoutMs = 10000) => {
          let handle: ReturnType<typeof setTimeout>;
          return Promise.race([
            requestPromise.then((v) => { clearTimeout(handle); return v; }),
            new Promise<never>((_, reject) => {
              handle = setTimeout(() => reject(new Error(`waitForRequest timed out after ${timeoutMs}ms`)), timeoutMs);
            }),
          ]);
        },
        close: () => new Promise<void>((res) => server.close(() => res())),
      });
    });
  });
}

// ---------------------------------------------------------------------------
// Tests — help / list / error cases (no server needed)
// ---------------------------------------------------------------------------

describe('setup-skills command', () => {
  it('shows help with --help', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('setup-skills');
    expect(stdout).toContain('--url');
    expect(stdout).toContain('--list');
    expect(stdout).toContain('--timeout');
  });

  it('--list prints agent table and exits 0', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills', '--list']);
    expect(exitCode).toBe(0);
    // Table should include at least one agent with skills support
    expect(stdout).toContain('claude-code');
  });

  it('exits 1 with error when agent-id is missing', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Missing required argument');
  });

  it('exits 1 with error for unknown agent-id', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills', 'not-a-real-agent']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Unknown agent');
  });

  it('exits 1 when agent does not support skills (claude-desktop)', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills', 'claude-desktop']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('does not support skills');
  });

  it('exits 1 for invalid timeout value', async () => {
    // Path is a positional argument for setup-skills, not --path
    const { stdout, exitCode } = await runCliAsync([
      'setup-skills', 'claude-code',
      '--url', 'http://127.0.0.1:1',
      '--timeout', 'not-a-number',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Invalid timeout value');
  });

  // ---------------------------------------------------------------------------
  // Tests — verifying relative path is sent in the POST body
  // ---------------------------------------------------------------------------

  describe('sends relative skillsPath in POST body', () => {
    let tmpDir: string;
    let captureServer: CaptureServer;

    beforeEach(async () => {
      tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-skills-test-'));
      captureServer = await startCaptureServer();
    });

    afterEach(async () => {
      await captureServer.close();
      fs.rmSync(tmpDir, { recursive: true, force: true });
    });

    it.each([
      ['claude-code', '.claude/skills'],
      ['cursor', '.cursor/skills'],
      ['vscode-copilot', '.github/skills'],
    ])('sends relative path for %s (%s)', async (agentId, expectedPath) => {
      const serverUrl = `http://127.0.0.1:${captureServer.port}`;
      const [{ body }, { exitCode }] = await Promise.all([
        captureServer.waitForRequest(),
        runCliAsync(['setup-skills', agentId, tmpDir, '--url', serverUrl]),
      ]);

      expect(exitCode).toBe(0);
      const sentPath = (body as Record<string, string>).path;
      expect(sentPath).toBe(expectedPath);
      expect(path.isAbsolute(sentPath)).toBe(false);
      expect(sentPath).not.toContain(tmpDir);
    }, 15000);

    it('POSTs to the correct endpoint', async () => {
      const serverUrl = `http://127.0.0.1:${captureServer.port}`;
      const [{ url, method }] = await Promise.all([
        captureServer.waitForRequest(),
        runCliAsync(['setup-skills', 'claude-code', tmpDir, '--url', serverUrl]),
      ]);

      expect(method).toBe('POST');
      expect(url).toBe('/api/system-tools/unity-skill-generate');
    }, 15000);
  });
});
