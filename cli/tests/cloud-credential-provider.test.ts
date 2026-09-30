// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as http from 'http';
import * as os from 'os';
import * as path from 'path';
import type { AddressInfo } from 'net';
import { effectiveFamilies, identityCredentialCodec } from '@baizor/gamedev-cli-core';

import { MachineCredentialStore } from '../src/utils/machine-credentials.js';
import {
  readCloudAccessToken,
  refreshCloudAccessToken,
} from '../src/utils/cloud-credentials.js';

/**
 * INTEGRATION: the CLI works straight through token expiry with NO re-login (task d2 DoD).
 *
 * A real HTTP fake authorization server serves `POST /oauth/token`; the machine store holds an
 * EXPIRED plugin-family access token plus its rotating refresh token. `readCloudAccessToken` —
 * the exact seam every Cloud-mode call resolves its Bearer through — must come back with a FRESH
 * access token minted via `grant_type=refresh_token`, rotate the stored family, and never touch
 * any login/device-authorization endpoint.
 *
 * This test is the tripwire for the provider-bypass regression: if anyone restores the raw
 * `store.read()?.accessToken` path (the pre-d2 defect), the expired token comes back verbatim and
 * the assertions below go RED.
 */

interface RecordedTokenRequest {
  grantType: string | null;
  refreshToken: string | null;
  clientId: string | null;
  hasScope: boolean;
  hasResource: boolean;
}

interface FakeAs {
  baseUrl: string;
  tokenRequests: RecordedTokenRequest[];
  otherRequests: string[];
  close: () => Promise<void>;
}

/** A minimal fake AS on an ephemeral 127.0.0.1 port (the OS picks it — no fixed-port collisions). */
async function startFakeAs(): Promise<FakeAs> {
  const tokenRequests: RecordedTokenRequest[] = [];
  const otherRequests: string[] = [];

  const server = http.createServer((req, res) => {
    let body = '';
    req.on('data', (chunk: Buffer) => (body += chunk.toString('utf-8')));
    req.on('end', () => {
      if (req.method === 'POST' && req.url === '/oauth/token') {
        const params = new URLSearchParams(body);
        tokenRequests.push({
          grantType: params.get('grant_type'),
          refreshToken: params.get('refresh_token'),
          clientId: params.get('client_id'),
          hasScope: params.has('scope'),
          hasResource: params.has('resource'),
        });
        if (
          params.get('grant_type') === 'refresh_token' &&
          params.get('refresh_token') === 'rotating-refresh-1'
        ) {
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(
            JSON.stringify({
              access_token: 'fresh-access-token',
              refresh_token: 'rotating-refresh-2',
              token_type: 'Bearer',
              expires_in: 3600,
            }),
          );
          return;
        }
        res.writeHead(400, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: 'invalid_grant' }));
        return;
      }
      otherRequests.push(`${req.method} ${req.url}`);
      res.writeHead(404);
      res.end();
    });
  });

  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address() as AddressInfo;
  return {
    baseUrl: `http://127.0.0.1:${port}`,
    tokenRequests,
    otherRequests,
    close: () => new Promise<void>((resolve, reject) => server.close((err) => (err ? reject(err) : resolve()))),
  };
}

describe('cloud credential provider — expiry self-heal against a fake AS (integration)', () => {
  let tmp: string;
  let fakeAs: FakeAs;

  beforeEach(async () => {
    tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-provider-'));
    fakeAs = await startFakeAs();
  });

  afterEach(async () => {
    await fakeAs.close();
    fs.rmSync(tmp, { recursive: true, force: true });
  });

  function seedExpiredPluginFamily(store: MachineCredentialStore): void {
    store.write({
      version: 2,
      serverTarget: fakeAs.baseUrl,
      subject: 'acct-1',
      families: {
        plugin: {
          accessToken: 'expired-access-token',
          refreshToken: 'rotating-refresh-1',
          // Expired a minute ago: the provider MUST refresh, never serve this token.
          expiresAt: new Date(Date.now() - 60_000).toISOString(),
          clientId: 'unity-mcp-cli',
          scope: 'mcp:plugin',
        },
      },
    });
  }

  it('serves a FRESH token through expiry with no re-login, rotating the stored family', async () => {
    const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
    seedExpiredPluginFamily(store);

    const token = await readCloudAccessToken({ store, serverBaseUrl: fakeAs.baseUrl });

    // Through expiry: the served Bearer is the freshly minted one, NEVER the expired on-disk token.
    expect(token).toBe('fresh-access-token');

    // Exactly one refresh-grant call; no device-authorization / login endpoint was ever touched.
    expect(fakeAs.tokenRequests).toHaveLength(1);
    expect(fakeAs.otherRequests).toEqual([]);
    const request = fakeAs.tokenRequests[0];
    expect(request.grantType).toBe('refresh_token');
    expect(request.refreshToken).toBe('rotating-refresh-1');
    // 04 §3 rule 2: the family's STORED clientId is presented.
    expect(request.clientId).toBe('unity-mcp-cli');
    // 04 §3 rule 3 (P0-3): scope and resource are omitted ENTIRELY.
    expect(request.hasScope).toBe(false);
    expect(request.hasResource).toBe(false);

    // The rotation was persisted under the lock: the stored family carries the new pair.
    const families = effectiveFamilies(store.read() ?? {});
    expect(families.plugin?.accessToken).toBe('fresh-access-token');
    expect(families.plugin?.refreshToken).toBe('rotating-refresh-2');
  });

  it('a still-valid token is served as-is (no gratuitous refresh traffic)', async () => {
    const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
    store.write({
      version: 2,
      serverTarget: fakeAs.baseUrl,
      families: {
        plugin: {
          accessToken: 'still-valid-token',
          refreshToken: 'rotating-refresh-1',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          clientId: 'unity-mcp-cli',
          scope: 'mcp:plugin',
        },
      },
    });

    const token = await readCloudAccessToken({ store, serverBaseUrl: fakeAs.baseUrl });
    expect(token).toBe('still-valid-token');
    expect(fakeAs.tokenRequests).toHaveLength(0);
  });

  it('reactive refresh (hub 401 path) rotates a still-valid-looking family on demand', async () => {
    const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
    store.write({
      version: 2,
      serverTarget: fakeAs.baseUrl,
      families: {
        plugin: {
          accessToken: 'locally-valid-but-revoked',
          refreshToken: 'rotating-refresh-1',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          clientId: 'unity-mcp-cli',
          scope: 'mcp:plugin',
        },
      },
    });

    const token = await refreshCloudAccessToken({ store, serverBaseUrl: fakeAs.baseUrl });
    expect(token).toBe('fresh-access-token');
    expect(fakeAs.tokenRequests).toHaveLength(1);
    const families = effectiveFamilies(store.read() ?? {});
    expect(families.plugin?.refreshToken).toBe('rotating-refresh-2');
  });

  it('a dead family (invalid_grant) degrades to "not signed in" — undefined, store families intact', async () => {
    const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
    store.write({
      version: 2,
      serverTarget: fakeAs.baseUrl,
      families: {
        plugin: {
          accessToken: 'expired-access-token',
          refreshToken: 'unknown-refresh-token', // the fake AS answers invalid_grant
          expiresAt: new Date(Date.now() - 60_000).toISOString(),
          clientId: 'unity-mcp-cli',
          scope: 'mcp:plugin',
        },
      },
    });

    const token = await readCloudAccessToken({ store, serverBaseUrl: fakeAs.baseUrl });
    expect(token).toBeUndefined();
    // The provider never deletes the store on a dead family (04 §3 rule 5).
    expect(store.exists).toBe(true);
  });

  it('an empty store resolves to undefined without any network traffic', async () => {
    const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
    const token = await readCloudAccessToken({ store, serverBaseUrl: fakeAs.baseUrl });
    expect(token).toBeUndefined();
    expect(fakeAs.tokenRequests).toHaveLength(0);
    expect(fakeAs.otherRequests).toEqual([]);
  });
});
