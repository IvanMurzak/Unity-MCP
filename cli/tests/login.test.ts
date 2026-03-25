// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as http from 'http';
import * as net from 'net';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { spawn } from 'child_process';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function runCliAsync(args: string[]): Promise<{ stdout: string; exitCode: number }> {
  return new Promise((resolve) => {
    const child = spawn('node', [CLI_PATH, ...args], { stdio: 'pipe' });
    let stdout = '';
    let settled = false;
    const timeoutMs = 30000;

    const timeout = setTimeout(() => {
      if (settled) return;
      settled = true;
      try { child.kill(); } catch { /* noop */ }
      resolve({ stdout, exitCode: 1 });
    }, timeoutMs);

    const finish = (exitCode: number) => {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      resolve({ stdout, exitCode });
    };

    child.stdout?.on('data', (d: Buffer) => { stdout += d.toString(); });
    child.stderr?.on('data', (d: Buffer) => { stdout += d.toString(); });
    child.on('close', (code) => { finish(code ?? 0); });
    child.on('error', (err) => {
      stdout += `\n[runCliAsync] Error: ${String(err)}\n`;
      finish(1);
    });
  });
}

interface MockAuthServer {
  port: number;
  authorizeRequests: unknown[];
  tokenRequests: unknown[];
  close: () => Promise<void>;
}

/**
 * Start a mock auth server that handles device auth endpoints.
 * - POST /api/auth/device/authorize → returns device code
 * - POST /api/auth/device/token → returns access_token on first poll
 */
function startMockAuthServer(options?: {
  tokenResponse?: 'success' | 'denied' | 'expired';
}): Promise<MockAuthServer> {
  const tokenResponse = options?.tokenResponse ?? 'success';

  return new Promise((resolve) => {
    const authorizeRequests: unknown[] = [];
    const tokenRequests: unknown[] = [];

    const server = http.createServer((req, res) => {
      let data = '';
      req.on('data', (chunk: Buffer) => { data += chunk.toString(); });
      req.on('end', () => {
        let body: unknown = null;
        try { body = JSON.parse(data); } catch { body = data; }

        if (req.url === '/api/auth/device/authorize' && req.method === 'POST') {
          authorizeRequests.push(body);
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({
            device_code: 'test-device-code',
            user_code: 'TEST-CODE',
            verification_uri: `http://127.0.0.1/device`,
            verification_uri_complete: `http://127.0.0.1/device?code=TEST-CODE`,
            expires_in: 300,
            interval: 1,
          }));
        } else if (req.url === '/api/auth/device/token' && req.method === 'POST') {
          tokenRequests.push(body);
          if (tokenResponse === 'success') {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ access_token: 'test-cloud-token', token_type: 'bearer' }));
          } else if (tokenResponse === 'denied') {
            res.writeHead(400, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'access_denied', error_description: 'User denied the request' }));
          } else {
            res.writeHead(400, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'expired_token', error_description: 'Device code expired' }));
          }
        } else {
          res.writeHead(404);
          res.end('Not found');
        }
      });
    });

    server.listen(0, '127.0.0.1', () => {
      const addr = server.address() as net.AddressInfo;
      resolve({
        port: addr.port,
        authorizeRequests,
        tokenRequests,
        close: () => new Promise<void>((res) => server.close(() => res())),
      });
    });
  });
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('login command', () => {
  it('shows help with --help', async () => {
    const { stdout, exitCode } = await runCliAsync(['login', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('login');
    expect(stdout).toContain('--force');
  });

  describe('with mock server', () => {
    let tmpDir: string;
    let mockServer: MockAuthServer;

    beforeEach(async () => {
      tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-login-test-'));
      // Create Assets dir so it looks like a Unity project (not needed for login, but safe)
      fs.mkdirSync(path.join(tmpDir, 'Assets'), { recursive: true });
    });

    afterEach(async () => {
      if (mockServer) await mockServer.close();
      fs.rmSync(tmpDir, { recursive: true, force: true });
    });

    it('shows already authenticated when cloudToken exists', async () => {
      // Pre-write a config with cloudToken
      const configDir = path.join(tmpDir, 'UserSettings');
      fs.mkdirSync(configDir, { recursive: true });
      fs.writeFileSync(
        path.join(configDir, 'AI-Game-Developer-Config.json'),
        JSON.stringify({ connectionMode: 'Cloud', cloudToken: 'existing-token' }),
      );

      const { stdout, exitCode } = await runCliAsync(['login', tmpDir]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('Already authenticated');
    });
  });
});
