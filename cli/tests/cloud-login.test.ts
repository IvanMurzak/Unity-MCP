// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  effectiveFamilies,
  identityCredentialCodec,
  type DeviceLoginOptions,
  type DeviceLoginResult,
  type TokenExchangeClient,
  type TokenExchangeRequest,
} from '@baizor/gamedev-cli-core';

import { runCloudLogin } from '../src/utils/cloud-login.js';
import { MachineCredentialStore } from '../src/utils/machine-credentials.js';

// runCloudLogin runs cli-core's OAuth device flow (RFC 8628) at AGENT scope by default and commits
// through the login-commit machinery (two-lock-hold agent commit + RFC 8693 exchange-derived plugin
// family), or — with `toolsOnly` — a plugin-only commit (O10/F10). Everything network-shaped is
// injected: the device `login`, the `exchangeClient`, and the RFC 7009 `revokeToken`.

const AGENT_MINT = {
  accessToken: 'agent-access-token',
  refreshToken: 'agent-refresh-token',
  expiresAt: '2030-01-01T00:00:00.000Z',
  serverTarget: 'https://ai-game.dev',
  subject: 'acct-1',
};

function tempStore(tmp: string): MachineCredentialStore {
  // The identity codec keeps these tests platform-independent (no DPAPI/PowerShell on Windows).
  return new MachineCredentialStore(path.join(tmp, '.ai-game-dev'), identityCredentialCodec);
}

function loginDouble(
  credentials: Record<string, unknown> = AGENT_MINT,
  capture?: { scope?: string; clientId?: string },
): (options: DeviceLoginOptions) => Promise<DeviceLoginResult> {
  return async (options) => {
    if (capture) {
      capture.scope = options.scope;
      capture.clientId = options.clientId;
    }
    return { ok: true, credentials };
  };
}

function exchangeDouble(behaviour?: {
  failures?: number;
  calls?: TokenExchangeRequest[];
}): TokenExchangeClient {
  let remainingFailures = behaviour?.failures ?? 0;
  return {
    exchange: async (request) => {
      behaviour?.calls?.push(request);
      if (remainingFailures > 0) {
        remainingFailures--;
        return { ok: false, reason: 'exchange-unavailable' };
      }
      return {
        ok: true,
        accessToken: 'plugin-access-token',
        refreshToken: 'plugin-refresh-token',
        expiresAt: '2030-01-01T00:30:00.000Z',
        scope: 'mcp:plugin',
        sub: 'acct-1',
      };
    },
  };
}

const noSleep = async (): Promise<void> => {};

describe('runCloudLogin — agent login (default)', () => {
  it('mints at mcp:agent scope with the unity-mcp-cli client id', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const capture: { scope?: string; clientId?: string } = {};
      await runCloudLogin(tempStore(tmp), {
        login: loginDouble(AGENT_MINT, capture),
        exchangeClient: exchangeDouble(),
        sleep: noSleep,
      });
      expect(capture.clientId).toBe('unity-mcp-cli');
      expect(capture.scope).toBe('mcp:agent');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('commits BOTH families (agent + exchange-derived plugin) with the v1 mirror — never a project config file', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      const exchangeCalls: TokenExchangeRequest[] = [];

      const token = await runCloudLogin(store, {
        login: loginDouble(),
        exchangeClient: exchangeDouble({ calls: exchangeCalls }),
        sleep: noSleep,
      });

      // The returned token is the PLUGIN-plane credential (what cloud tool calls present).
      expect(token).toBe('plugin-access-token');
      expect(store.exists).toBe(true);

      const document = store.read();
      expect(document?.version).toBe(2);
      expect(document?.subject).toBe('acct-1');
      expect(document?.serverTarget).toBe('https://ai-game.dev');

      const families = effectiveFamilies(document ?? {});
      expect(families.agent?.accessToken).toBe('agent-access-token');
      expect(families.agent?.refreshToken).toBe('agent-refresh-token');
      expect(families.agent?.clientId).toBe('unity-mcp-cli');
      expect(families.plugin?.accessToken).toBe('plugin-access-token');
      expect(families.plugin?.refreshToken).toBe('plugin-refresh-token');
      expect(families.plugin?.clientId).toBe('unity-mcp-cli');

      // v1 compat mirror: top-level token fields mirror the PLUGIN family (old readers key on them).
      expect(document?.accessToken).toBe('plugin-access-token');
      expect(document?.refreshToken).toBe('plugin-refresh-token');

      // The exchange presented the fresh agent access token + our own client id (RFC 8693 / O2).
      expect(exchangeCalls).toHaveLength(1);
      expect(exchangeCalls[0].subjectToken).toBe('agent-access-token');
      expect(exchangeCalls[0].clientId).toBe('unity-mcp-cli');

      // The login must NOT write the legacy per-project cloudToken config.
      expect(
        fs.existsSync(path.join(tmp, 'UserSettings', 'AI-Game-Developer-Config.json')),
      ).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('F1 partial: a failed exchange leaves the committed agent family and the derivation retry completes the store', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      // First exchange attempt (inside commitAgentLogin) fails; the retry loop's next attempt succeeds.
      const token = await runCloudLogin(store, {
        login: loginDouble(),
        exchangeClient: exchangeDouble({ failures: 1 }),
        sleep: noSleep,
      });

      expect(token).toBe('plugin-access-token');
      const families = effectiveFamilies(store.read() ?? {});
      expect(families.agent?.accessToken).toBe('agent-access-token');
      expect(families.plugin?.accessToken).toBe('plugin-access-token');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('F1 partial, derivation never succeeds: returns null but the agent family stays committed', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      const token = await runCloudLogin(store, {
        login: loginDouble(),
        exchangeClient: exchangeDouble({ failures: 99 }),
        sleep: noSleep,
      });

      expect(token).toBeNull();
      const families = effectiveFamilies(store.read() ?? {});
      expect(families.agent?.accessToken).toBe('agent-access-token');
      expect(families.plugin).toBeUndefined();
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('returns null and does not write the store on a failed sign-in', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      const login = async (): Promise<DeviceLoginResult> => ({
        ok: false,
        reason: 'denied',
        message: 'Authorization was denied.',
      });

      const token = await runCloudLogin(store, { login, exchangeClient: exchangeDouble() });

      expect(token).toBeNull();
      expect(store.exists).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('runCloudLogin — tools-only (O10/F10)', () => {
  it('mints at mcp:plugin scope', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const capture: { scope?: string; clientId?: string } = {};
      await runCloudLogin(tempStore(tmp), {
        toolsOnly: true,
        login: loginDouble({ ...AGENT_MINT, accessToken: 'plugin-mint' }, capture),
        exchangeClient: exchangeDouble(),
      });
      expect(capture.scope).toBe('mcp:plugin');
      expect(capture.clientId).toBe('unity-mcp-cli');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('produces a PLUGIN-ONLY store: no agent family, no token exchange (App pickup impossible by design)', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      const exchangeCalls: TokenExchangeRequest[] = [];

      const token = await runCloudLogin(store, {
        toolsOnly: true,
        login: loginDouble({
          accessToken: 'tools-only-access',
          refreshToken: 'tools-only-refresh',
          expiresAt: '2030-01-01T00:00:00.000Z',
          serverTarget: 'https://ai-game.dev',
          subject: 'acct-1',
        }),
        exchangeClient: exchangeDouble({ calls: exchangeCalls }),
      });

      expect(token).toBe('tools-only-access');

      const document = store.read();
      const families = effectiveFamilies(document ?? {});
      // F10: the plugin family is the ONLY family — a tools-only machine holds no agent credential.
      expect(families.plugin?.accessToken).toBe('tools-only-access');
      expect(families.plugin?.clientId).toBe('unity-mcp-cli');
      expect(families.agent).toBeUndefined();
      expect(families.legacy).toBeUndefined();
      // The RFC 8693 exchange is for deriving a plugin family FROM an agent mint — it must not run.
      expect(exchangeCalls).toHaveLength(0);
      // v1 mirror still points old readers at the plugin credential.
      expect(document?.accessToken).toBe('tools-only-access');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});

describe('runCloudLogin — account-switch guard (D6/F7)', () => {
  function seedStoreAs(store: MachineCredentialStore, subject: string): void {
    store.write({
      version: 2,
      serverTarget: 'https://ai-game.dev',
      subject,
      families: {
        agent: {
          accessToken: `${subject}-agent-access`,
          refreshToken: `${subject}-agent-refresh`,
          expiresAt: '2030-01-01T00:00:00.000Z',
          clientId: 'unity-mcp-cli',
          scope: 'mcp:agent',
        },
        plugin: {
          accessToken: `${subject}-plugin-access`,
          refreshToken: `${subject}-plugin-refresh`,
          expiresAt: '2030-01-01T00:00:00.000Z',
          clientId: 'unity-mcp-cli',
          scope: 'mcp:plugin',
        },
      },
    });
  }

  const MINT_AS_B = { ...AGENT_MINT, subject: 'acct-B' };

  it('without --yes (non-interactive): a subject mismatch is DECLINED — store untouched, just-minted family revoked', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      seedStoreAs(store, 'acct-A');
      const revoked: Array<{ token: string; clientId: string }> = [];

      const token = await runCloudLogin(store, {
        login: loginDouble(MINT_AS_B),
        exchangeClient: exchangeDouble(),
        // No confirmAccountSwitch injected and no assumeYes: the default confirm runs, and with
        // a non-TTY stdin (vitest) it fails CLOSED — the F7 `--yes` gate.
        revokeToken: (tok, clientId) => {
          revoked.push({ token: tok, clientId });
          return true;
        },
        sleep: noSleep,
      });

      expect(token).toBeNull();

      // Store untouched: still account A's credential set.
      const document = store.read();
      expect(document?.subject).toBe('acct-A');
      const families = effectiveFamilies(document ?? {});
      expect(families.agent?.accessToken).toBe('acct-A-agent-access');
      expect(families.plugin?.accessToken).toBe('acct-A-plugin-access');

      // The just-minted B family was revoked best-effort (no orphan device row).
      expect(revoked.some((r) => r.token === MINT_AS_B.refreshToken)).toBe(true);
      // Account A's families were NOT revoked.
      expect(revoked.some((r) => r.token === 'acct-A-agent-refresh')).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('with --yes: the switch is confirmed — store replaced with account B, old families revoked best-effort', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      seedStoreAs(store, 'acct-A');
      const revoked: Array<{ token: string; clientId: string }> = [];

      const token = await runCloudLogin(store, {
        assumeYes: true,
        login: loginDouble(MINT_AS_B),
        exchangeClient: exchangeDouble(),
        revokeToken: (tok, clientId) => {
          revoked.push({ token: tok, clientId });
          return true;
        },
        sleep: noSleep,
      });

      expect(token).toBe('plugin-access-token');

      const document = store.read();
      expect(document?.subject).toBe('acct-B');
      const families = effectiveFamilies(document ?? {});
      expect(families.agent?.accessToken).toBe('agent-access-token');
      expect(families.plugin?.accessToken).toBe('plugin-access-token');
      // Account A's material is gone from the store (single-account store, D6)...
      expect(JSON.stringify(document)).not.toContain('acct-A-');
      // ...and its families were revoked best-effort.
      expect(revoked.some((r) => r.token === 'acct-A-agent-refresh')).toBe(true);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it('matching subjects proceed without any confirmation', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = tempStore(tmp);
      seedStoreAs(store, 'acct-1'); // same subject as AGENT_MINT

      const token = await runCloudLogin(store, {
        login: loginDouble(),
        exchangeClient: exchangeDouble(),
        // No revokeToken / confirm needed: same subject ⇒ plain merge, no guard prompt.
        confirmAccountSwitch: () => {
          throw new Error('confirm must not be called for a matching subject');
        },
        sleep: noSleep,
      });

      expect(token).toBe('plugin-access-token');
      expect(store.read()?.subject).toBe('acct-1');
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});
