// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { DeviceLoginResult } from '@baizor/gamedev-cli-core';

import { runCloudLogin } from '../src/utils/cloud-login.js';
import { MachineCredentialStore } from '../src/utils/machine-credentials.js';

// runCloudLogin now runs cli-core's OAuth device flow (RFC 8628). We inject a `login` double that
// resolves success WITHOUT the callbacks / network, so openBrowser and the spinner are never reached
// and the FULL credential set is persisted.
describe('runCloudLogin', () => {
  it('persists the full OAuth credential to the credential store (never a project config file)', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'));

      const login = async (): Promise<DeviceLoginResult> => ({
        ok: true,
        credentials: {
          accessToken: 'test-access-token',
          refreshToken: 'test-refresh-token',
          expiresAt: '2030-01-01T00:00:00.000Z',
          serverTarget: 'https://ai-game.dev',
          subject: 'acct-1',
        },
      });

      const token = await runCloudLogin(store, { login });

      expect(token).toBe('test-access-token');
      expect(store.exists).toBe(true);

      const creds = store.read();
      // B3 fix: the full credential set is persisted (not just accessToken + serverTarget).
      expect(creds?.accessToken).toBe('test-access-token');
      expect(creds?.refreshToken).toBe('test-refresh-token');
      expect(creds?.expiresAt).toBe('2030-01-01T00:00:00.000Z');
      expect(creds?.serverTarget).toBe('https://ai-game.dev');
      expect(creds?.subject).toBe('acct-1');
      expect(creds?.version).toBe(1);

      // The login must NOT write the legacy per-project cloudToken config.
      expect(
        fs.existsSync(path.join(tmp, 'UserSettings', 'AI-Game-Developer-Config.json')),
      ).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('passes the unity-mcp-cli client id + mcp:plugin scope to the device flow (T1)', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'));
      let seenClientId: string | undefined;
      let seenScope: string | undefined;

      const login = async (opts: {
        clientId: string;
        scope?: string;
      }): Promise<DeviceLoginResult> => {
        seenClientId = opts.clientId;
        seenScope = opts.scope;
        return { ok: true, credentials: { accessToken: 'tok', serverTarget: 'https://ai-game.dev' } };
      };

      await runCloudLogin(store, { login });

      expect(seenClientId).toBe('unity-mcp-cli');
      expect(seenScope).toBe('mcp:plugin');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('returns null and does not write the store on a failed sign-in', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'));
      const login = async (): Promise<DeviceLoginResult> => ({
        ok: false,
        reason: 'denied',
        message: 'Authorization was denied.',
      });

      const token = await runCloudLogin(store, { login });

      expect(token).toBeNull();
      expect(store.exists).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});
