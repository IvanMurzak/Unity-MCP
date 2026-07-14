// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  redeemEnrollmentCode,
  normalizeRedeemResponse,
  resolveEnrollCode,
  pinUrl,
  upsertProjectPinIntoConfigs,
  runEnroll,
  projectRootForIdentity,
  EnrollmentError,
} from '../src/utils/enroll.js';
import { deriveProjectPin } from '../src/utils/port.js';
import { MachineCredentialStore } from '../src/utils/machine-credentials.js';

function tmpProject(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'enroll-'));
}

interface CapturedRequest {
  url: string;
  body: unknown;
  method?: string;
}

/** A fetch double for the AS enroll/redeem endpoint that captures the request. */
function makeRedeemFetch(
  responseBody: Record<string, unknown>,
  status: number,
  captured: CapturedRequest[],
): typeof fetch {
  return (async (input: RequestInfo | URL, init?: RequestInit) => {
    captured.push({
      url: String(input),
      method: init?.method,
      body: init?.body ? JSON.parse(String(init.body)) : undefined,
    });
    if (status >= 400) return new Response('nope', { status });
    return new Response(JSON.stringify(responseBody), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  }) as unknown as typeof fetch;
}

describe('normalizeRedeemResponse', () => {
  it('accepts snake_case fields', () => {
    const c = normalizeRedeemResponse({
      access_token: 'at',
      refresh_token: 'rt',
      expires_at: '2030-01-01T00:00:00Z',
      server_target: 'https://ai-game.dev',
      subject: 'acct-1',
    });
    expect(c).toEqual({
      accessToken: 'at',
      refreshToken: 'rt',
      expiresAt: '2030-01-01T00:00:00Z',
      serverTarget: 'https://ai-game.dev',
      subject: 'acct-1',
    });
  });

  it('accepts camelCase fields', () => {
    const c = normalizeRedeemResponse({
      accessToken: 'at',
      refreshToken: 'rt',
      serverTarget: 'http://localhost:24000',
    });
    expect(c.accessToken).toBe('at');
    expect(c.refreshToken).toBe('rt');
    expect(c.serverTarget).toBe('http://localhost:24000');
  });

  it('converts expires_in seconds to an absolute expiresAt', () => {
    const before = Date.now();
    const c = normalizeRedeemResponse({ access_token: 'at', expires_in: 3600 });
    const at = new Date(c.expiresAt!).getTime();
    expect(at).toBeGreaterThanOrEqual(before + 3600 * 1000 - 5000);
    expect(at).toBeLessThanOrEqual(Date.now() + 3600 * 1000 + 5000);
  });
});

describe('redeemEnrollmentCode', () => {
  it('POSTs {enroll_code} in the body (never the URL) and returns the credential', async () => {
    const captured: CapturedRequest[] = [];
    const fetchImpl = makeRedeemFetch(
      { access_token: 'plugin-jwt', refresh_token: 'rt', server_target: 'https://ai-game.dev' },
      200,
      captured,
    );

    const cred = await redeemEnrollmentCode('SECRET-CODE-123', {
      baseUrl: 'https://ai-game.dev',
      fetchImpl,
    });

    expect(cred.accessToken).toBe('plugin-jwt');
    expect(captured).toHaveLength(1);
    expect(captured[0].url).toBe('https://ai-game.dev/api/auth/enroll/redeem');
    expect(captured[0].method).toBe('POST');
    expect(captured[0].body).toEqual({ enroll_code: 'SECRET-CODE-123' });
    // The code must NOT appear in the URL / query string.
    expect(captured[0].url).not.toContain('SECRET-CODE-123');
  });

  it('surfaces a spent/invalid code as an actionable EnrollmentError with the HTTP status', async () => {
    const captured: CapturedRequest[] = [];
    const fetchImpl = makeRedeemFetch({}, 400, captured);

    await expect(
      redeemEnrollmentCode('BURNED', { baseUrl: 'https://ai-game.dev', fetchImpl }),
    ).rejects.toMatchObject({ name: 'EnrollmentError', status: 400 });
  });

  it('rejects a response with no access token', async () => {
    const captured: CapturedRequest[] = [];
    const fetchImpl = makeRedeemFetch({ server_target: 'https://ai-game.dev' }, 200, captured);
    await expect(
      redeemEnrollmentCode('X', { baseUrl: 'https://ai-game.dev', fetchImpl }),
    ).rejects.toThrow(EnrollmentError);
  });
});

describe('resolveEnrollCode (--enroll vs --enroll-stdin)', () => {
  it('reads from stdin (not argv) in --enroll-stdin mode', () => {
    let stdinRead = false;
    const code = resolveEnrollCode({ enrollStdin: true }, () => {
      stdinRead = true;
      return '  code-from-stdin\n';
    });
    expect(code).toBe('code-from-stdin');
    expect(stdinRead).toBe(true);
  });

  it('reads from argv in --enroll mode and never touches stdin', () => {
    let stdinRead = false;
    const code = resolveEnrollCode({ enroll: 'code-from-argv' }, () => {
      stdinRead = true;
      return 'unused';
    });
    expect(code).toBe('code-from-argv');
    expect(stdinRead).toBe(false);
  });

  it('rejects supplying both', () => {
    expect(() => resolveEnrollCode({ enroll: 'a', enrollStdin: true }, () => 'b')).toThrow(/not both/);
  });

  it('rejects supplying neither', () => {
    expect(() => resolveEnrollCode({}, () => '')).toThrow(/required/);
  });

  it('rejects empty stdin', () => {
    expect(() => resolveEnrollCode({ enrollStdin: true }, () => '   \n')).toThrow(/No enrollment code/);
  });
});

describe('projectRootForIdentity', () => {
  it('produces a forward-slash absolute root (matching the plugin ProjectRootPath)', () => {
    const root = projectRootForIdentity(process.cwd());
    expect(root).not.toContain('\\'); // never a backslash — plugin uses Application.dataPath form
    expect(path.isAbsolute(root) || /^[A-Za-z]:\//.test(root)).toBe(true);
  });

  it('derives the same pin regardless of input separator style for the same path', () => {
    // A path expressed with backslashes vs forward slashes must normalize to the same pin,
    // because path.resolve + the /\\/->/ conversion canonicalizes the separators.
    const a = projectRootForIdentity('C:/tmp/some-proj');
    const b = projectRootForIdentity('C:/tmp/some-proj/');
    expect(deriveProjectPin(a)).toBe(deriveProjectPin(b));
  });
});

describe('pinUrl', () => {
  it('appends /p/<pin> to a hosted URL', () => {
    expect(pinUrl('https://ai-game.dev/mcp', '34ea75f2')).toBe('https://ai-game.dev/mcp/p/34ea75f2');
  });
  it('appends /p/<pin> to a localhost URL', () => {
    expect(pinUrl('http://localhost:24000', '8ef72cf7')).toBe('http://localhost:24000/p/8ef72cf7');
  });
  it('replaces an existing pin rather than stacking', () => {
    expect(pinUrl('https://ai-game.dev/mcp/p/00000000', '34ea75f2')).toBe(
      'https://ai-game.dev/mcp/p/34ea75f2',
    );
  });
});

describe('upsertProjectPinIntoConfigs', () => {
  it('pins the ai-game-developer URL in an existing project-local .mcp.json', () => {
    const project = tmpProject();
    try {
      const mcpJson = path.join(project, '.mcp.json');
      fs.writeFileSync(
        mcpJson,
        JSON.stringify({ mcpServers: { 'ai-game-developer': { type: 'http', url: 'https://ai-game.dev/mcp' } } }, null, 2),
      );

      const { updatedFiles } = upsertProjectPinIntoConfigs(project, '34ea75f2');
      expect(updatedFiles).toContain(mcpJson);

      const written = JSON.parse(fs.readFileSync(mcpJson, 'utf-8'));
      expect(written.mcpServers['ai-game-developer'].url).toBe('https://ai-game.dev/mcp/p/34ea75f2');
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('leaves configs without an ai-game-developer entry untouched', () => {
    const project = tmpProject();
    try {
      const mcpJson = path.join(project, '.mcp.json');
      const original = JSON.stringify({ mcpServers: { other: { url: 'https://example.com' } } }, null, 2);
      fs.writeFileSync(mcpJson, original);

      const { updatedFiles } = upsertProjectPinIntoConfigs(project, '34ea75f2');
      expect(updatedFiles).toHaveLength(0);
      expect(fs.readFileSync(mcpJson, 'utf-8')).toBe(original);
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('is a no-op for a project with no agent configs', () => {
    const project = tmpProject();
    try {
      const { updatedFiles } = upsertProjectPinIntoConfigs(project, '34ea75f2');
      expect(updatedFiles).toHaveLength(0);
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });
});

describe('runEnroll (full side effect)', () => {
  it('writes the credential to the machine store + marker + pin, never a project token file', async () => {
    const project = tmpProject();
    const storeDir = path.join(project, '.ai-game-dev');
    try {
      // Pre-existing project-local agent config that should get pinned.
      const mcpJson = path.join(project, '.mcp.json');
      fs.writeFileSync(
        mcpJson,
        JSON.stringify({ mcpServers: { 'ai-game-developer': { type: 'http', url: 'https://ai-game.dev/mcp' } } }, null, 2),
      );

      const store = new MachineCredentialStore(storeDir);
      const captured: CapturedRequest[] = [];
      const fetchImpl = makeRedeemFetch(
        {
          access_token: 'plugin-jwt',
          refresh_token: 'plugin-refresh',
          server_target: 'https://ai-game.dev',
          expires_in: 3600,
        },
        200,
        captured,
      );

      const result = await runEnroll({ code: 'CODE', projectPath: project, store, fetchImpl });

      // 1. Credential in the machine store (never a project cloudToken config).
      const creds = store.read();
      expect(creds?.accessToken).toBe('plugin-jwt');
      expect(creds?.refreshToken).toBe('plugin-refresh');
      expect(creds?.serverTarget).toBe('https://ai-game.dev');
      expect(fs.existsSync(path.join(project, 'UserSettings', 'AI-Game-Developer-Config.json'))).toBe(false);

      // 2. Project marker records the server target.
      const marker = JSON.parse(fs.readFileSync(result.markerPath, 'utf-8'));
      expect(marker.serverTarget).toBe('https://ai-game.dev');

      // 3. Pin upserted into the existing config (forward-slash root, matching the plugin).
      const expectedPin = deriveProjectPin(projectRootForIdentity(project));
      expect(result.pin).toBe(expectedPin);
      const written = JSON.parse(fs.readFileSync(mcpJson, 'utf-8'));
      expect(written.mcpServers['ai-game-developer'].url).toBe(`https://ai-game.dev/mcp/p/${expectedPin}`);
      expect(result.pinnedConfigs).toContain(mcpJson);
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });
});
