// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  unityAdapter,
  derivePin,
  derivePinV2,
  deviceLogin,
  DEFAULT_POLL_INTERVAL_MS,
  type DeviceAuthTransport,
  type DeviceAuthorizeResponse,
  type DeviceTokenResponse,
} from '@baizor/gamedev-cli-core';
import { setupMcp } from '../src/lib/setup-mcp.js';
import { MCP_SERVER_NAME } from '../src/utils/agents.js';

// ---------------------------------------------------------------------------
// e1: unity-mcp-cli is a thin adapter over @baizor/gamedev-cli-core. These parity checks gate the
// integration seams the auth-fixes design pins (T1/T3/T4/T7): the Unity engine adapter's identity,
// the v2 project-identity golden vectors the pinned config routes on, and the RFC 8628 client-MUSTs
// the login flow runs through cli-core.
// ---------------------------------------------------------------------------

describe('unity engine adapter (T7)', () => {
  it('carries the Unity identity: serverName / clientId / stdio ON', () => {
    expect(unityAdapter.engine).toBe('unity');
    expect(unityAdapter.serverName).toBe('ai-game-developer');
    expect(unityAdapter.clientId).toBe('unity-mcp-cli');
    expect(unityAdapter.stdioSupported).toBe(true);
    // MCP_SERVER_NAME is derived from the adapter, so they can never drift.
    expect(MCP_SERVER_NAME).toBe(unityAdapter.serverName);
  });

  it('reduces a pinned hub URL to the AS root for the credential serverTarget (MED-2)', () => {
    expect(unityAdapter.loginServerTarget('https://ai-game.dev/mcp/p/34ea75f2')).toBe('https://ai-game.dev');
    expect(unityAdapter.loginServerTarget('https://ai-game.dev/mcp')).toBe('https://ai-game.dev');
  });
});

describe('project-identity v2 golden vectors (T3 / B5 fix)', () => {
  // Derived from the committed C# v1 golden vectors: v2 == v1 for an all-forward-slash string, and
  // v2 of a backslash root equals v1 of that root's forward-slash form.
  it('v2 leaves an all-forward-slash root identical to v1', () => {
    expect(derivePinV2('/home/user/my-game')).toBe('34ea75f2');
    expect(derivePinV2('/home/user/my-game')).toBe(derivePin('/home/user/my-game'));
  });

  it('v2 folds a Windows backslash root onto its forward-slash form (the B5 fix)', () => {
    // C:\Users\user\my-game (v1 8ef72cf7) folds to C:/Users/user/my-game (v1 5a87324e) under v2.
    expect(derivePinV2('C:\\Users\\user\\my-game')).toBe('5a87324e');
    expect(derivePinV2('C:\\Users\\user\\my-game')).toBe(derivePinV2('C:/Users/user/my-game'));
    expect(derivePinV2('C:\\Users\\user\\my-game')).not.toBe(derivePin('C:\\Users\\user\\my-game'));
  });
});

describe('setup-mcp routes on the v2 pin byte-for-byte (T4)', () => {
  it('writes <base>/mcp/p/<pin-v2> for the resolved project root', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-parity-'));
    try {
      const result = await setupMcp({ agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http' });
      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;

      const entry = (
        JSON.parse(fs.readFileSync(result.configPath, 'utf-8')) as {
          mcpServers: Record<string, { url: string }>;
        }
      ).mcpServers[MCP_SERVER_NAME];
      expect(entry.url).toBe(`https://ai-game.dev/mcp/p/${derivePinV2(path.resolve(tmpDir))}`);
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});

// ---------------------------------------------------------------------------
// RFC 8628 client-MUSTs, exercised through cli-core's deviceLogin (the exact stack the CLI's login
// runs). A mock transport + injected delay/clock keeps this offline and instant.
// ---------------------------------------------------------------------------

const AUTH: DeviceAuthorizeResponse = {
  device_code: 'dev-code',
  user_code: 'WXYZ-9876',
  verification_uri: 'https://ai-game.dev/device',
  verification_uri_complete: 'https://ai-game.dev/device?code=WXYZ-9876',
  expires_in: 300,
  interval: 5,
};

function transportOf(pollResponses: DeviceTokenResponse[]): DeviceAuthTransport {
  let i = 0;
  return {
    requestDeviceCode: async () => AUTH,
    pollToken: async () => pollResponses[Math.min(i++, pollResponses.length - 1)],
  };
}

describe('RFC 8628 device-flow behaviors (via cli-core, T1)', () => {
  it('§3.3: shows BOTH the user code AND the verification URI to the caller', async () => {
    let shownCode: string | undefined;
    let shownUri: string | undefined;
    const result = await deviceLogin({
      serverBaseUrl: 'https://ai-game.dev',
      clientId: 'unity-mcp-cli',
      transport: transportOf([{ access_token: 'jwt' }]),
      onUserCode: (code, uri) => {
        shownCode = code;
        shownUri = uri;
      },
      delay: async () => {},
      now: () => 0,
    });
    expect(result.ok).toBe(true);
    expect(shownCode).toBe('WXYZ-9876');
    expect(shownUri).toBe('https://ai-game.dev/device');
  });

  it('keeps polling through authorization_pending, then succeeds with full credentials', async () => {
    const result = await deviceLogin({
      serverBaseUrl: 'https://ai-game.dev',
      clientId: 'unity-mcp-cli',
      transport: transportOf([
        { error: 'authorization_pending' },
        { access_token: 'jwt', refresh_token: 'rt', expires_in: 3600 },
      ]),
      onUserCode: () => {},
      delay: async () => {},
      now: () => 0,
    });
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.credentials.accessToken).toBe('jwt');
    expect(result.credentials.refreshToken).toBe('rt');
    expect(result.credentials.expiresAt).toBeTruthy();
  });

  it('§3.5: honours the server interval floor and bumps it by 5s on slow_down', async () => {
    const delays: number[] = [];
    await deviceLogin({
      serverBaseUrl: 'https://ai-game.dev',
      clientId: 'unity-mcp-cli',
      transport: transportOf([{ error: 'slow_down' }, { access_token: 'jwt' }]),
      onUserCode: () => {},
      delay: async (ms) => {
        delays.push(ms);
      },
      now: () => 0,
    });
    // First poll waits the server interval (5s = the floor); after slow_down the next wait is +5s.
    expect(delays[0]).toBe(DEFAULT_POLL_INTERVAL_MS);
    expect(delays[1]).toBe(DEFAULT_POLL_INTERVAL_MS + 5000);
  });

  it('stops cleanly on access_denied and expired_token', async () => {
    const denied = await deviceLogin({
      serverBaseUrl: 'https://ai-game.dev',
      clientId: 'unity-mcp-cli',
      transport: transportOf([{ error: 'access_denied' }]),
      onUserCode: () => {},
      delay: async () => {},
      now: () => 0,
    });
    expect(denied).toMatchObject({ ok: false, reason: 'denied' });

    const expired = await deviceLogin({
      serverBaseUrl: 'https://ai-game.dev',
      clientId: 'unity-mcp-cli',
      transport: transportOf([{ error: 'expired_token' }]),
      onUserCode: () => {},
      delay: async () => {},
      now: () => 0,
    });
    expect(expired).toMatchObject({ ok: false, reason: 'expired' });
  });
});
