// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { setupMcp } from '../src/lib/setup-mcp.js';
import { getAgentById, MCP_SERVER_NAME } from '../src/utils/agents.js';
import { derivePinV2, type ProjectKeyRequest, type ProjectKeyResolver } from '@baizor/gamedev-cli-core';
import type { UnityConnectionConfig } from '../src/utils/config.js';

/**
 * A resolver standing in for "this machine is not signed in". Every test injects one: the default
 * resolver reads the REAL `~/.ai-game-dev` credential store and would mint real project keys against
 * production on a signed-in developer machine.
 */
const noLogin: ProjectKeyResolver = async () => ({ kind: 'no-login', reason: 'not signed in' });

const PROJECT_KEY = 'agd_pk_unit_test_key_0123456789';

/** A signed-in resolver that records every request and hands back `PROJECT_KEY`. */
function signedIn(): { resolver: ProjectKeyResolver; calls: ProjectKeyRequest[]; revoked: string[] } {
  const calls: ProjectKeyRequest[] = [];
  const revoked: string[] = [];
  const resolver: ProjectKeyResolver = async (request) => {
    calls.push(request);
    return {
      kind: 'ok',
      key: PROJECT_KEY,
      keyId: 'pk_1',
      pin: request.pin,
      source: request.regenerate ? 'minted' : 'reused',
      warnings: [],
      revokePrevious: request.regenerate
        ? async () => {
            revoked.push('pk_0');
            return undefined;
          }
        : undefined,
    };
  };
  return { resolver, calls, revoked };
}

/** The v2 routing pin cli-core's setup-mcp appends to the hosted URL by default (T4). */
function pinnedHostedUrl(projectDir: string): string {
  return `https://ai-game.dev/mcp/p/${derivePinV2(path.resolve(projectDir))}`;
}

// ---------------------------------------------------------------------------
// mcp-authorize g2 — setup-mcp writes a credential-free, URL-only http config
// for OAuth-capable clients (design decision D11 / Flow A), and a static
// Authorization header ONLY on an explicit PAT opt-in (Flow C). Regression
// guard for the g1 flagship bug (setup-mcp injected a static Bearer token that
// the hosted OAuth endpoint 401s AND that suppresses the client's own OAuth).
// ---------------------------------------------------------------------------

/** Seed a project's UserSettings/AI-Game-Developer-Config.json. */
function seedConfig(projectDir: string, config: UnityConnectionConfig): void {
  const dir = path.join(projectDir, 'UserSettings');
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(
    path.join(dir, 'AI-Game-Developer-Config.json'),
    JSON.stringify(config, null, 2) + '\n',
  );
}

/** The hosted-Cloud config that reproduces the g1 flagship scenario: a
 *  cloud token pinned on disk + `authOption: required` + the hosted endpoint. */
const CONFIG_TOKEN = 'agd_cloud_token_from_config_SHOULD_NOT_LEAK';
function seedHostedConfig(projectDir: string): void {
  seedConfig(projectDir, {
    connectionMode: 'Cloud',
    cloudToken: CONFIG_TOKEN,
    authOption: 'required',
    timeoutMs: 10000,
  });
}

describe('setup-mcp — signed out: URL-only Cloud config (mcp-authorize g2 / D11)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-setup-mcp-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  // DoD 1: without a machine login no project key can be minted, so every client gets a URL-only
  // config (no Authorization header) — even with a config token + required auth.
  it.each(['claude-code', 'cursor', 'vscode-copilot', 'codex'])(
    'writes a URL-only config (no Authorization header) for %s when signed out',
    async (agentId) => {
      seedHostedConfig(tmpDir);

      const result = await setupMcp({
        agentId,
        unityProjectPath: tmpDir,
        transport: 'http',
        projectKeyResolver: noLogin,
      });

      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;

      const raw = fs.readFileSync(result.configPath, 'utf-8');
      // The hosted OAuth URL is present…
      expect(raw).toContain('https://ai-game.dev/mcp');
      // …but NO credential and NO Authorization header leaked into the file.
      expect(raw).not.toContain('Authorization');
      expect(raw).not.toContain(CONFIG_TOKEN);
      expect(result.credential).toBe('none');
      // The only warning is the sign-in hint (never a project-file-PAT or git warning).
      expect(result.warnings).toHaveLength(1);
      expect(result.warnings[0]).toContain('Sign in');
    },
  );

  it('claude-code entry is exactly {type,url} with the T4-pinned URL and no headers key', async () => {
    seedHostedConfig(tmpDir);

    const result = await setupMcp({
      agentId: 'claude-code',
      unityProjectPath: tmpDir,
      transport: 'http',
      projectKeyResolver: noLogin,
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const root = JSON.parse(fs.readFileSync(result.configPath, 'utf-8')) as {
      mcpServers: Record<string, Record<string, unknown>>;
    };
    const entry = root.mcpServers[MCP_SERVER_NAME];
    // T4: the URL is pinned to this project's routing segment by default (byte-for-byte the
    // Unity Editor Configure output), and no credential is written.
    expect(entry).toEqual({ type: 'http', url: pinnedHostedUrl(tmpDir) });
    expect(entry).not.toHaveProperty('headers');
  });

  it('--no-pin writes the unpinned canonical URL (B4 escape hatch)', async () => {
    seedHostedConfig(tmpDir);

    const result = await setupMcp({
      agentId: 'claude-code',
      unityProjectPath: tmpDir,
      transport: 'http',
      noPin: true,
      projectKeyResolver: noLogin,
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const root = JSON.parse(fs.readFileSync(result.configPath, 'utf-8')) as {
      mcpServers: Record<string, Record<string, unknown>>;
    };
    expect(root.mcpServers[MCP_SERVER_NAME]).toEqual({ type: 'http', url: 'https://ai-game.dev/mcp' });
  });

  // DoD 2: the PAT fallback still writes a header — but ONLY on an explicit
  // opt-in (a `--token` the caller passed), not from a config-resolved token.
  it('writes the Authorization header for an explicit PAT opt-in (--token)', async () => {
    seedHostedConfig(tmpDir);
    const pat = 'agd_pat_explicit_optin';

    const result = await setupMcp({
      agentId: 'claude-code',
      unityProjectPath: tmpDir,
      transport: 'http',
      token: pat,
      projectKeyResolver: noLogin,
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const root = JSON.parse(fs.readFileSync(result.configPath, 'utf-8')) as {
      mcpServers: Record<string, { url: string; headers?: Record<string, string> }>;
    };
    const entry = root.mcpServers[MCP_SERVER_NAME];
    expect(entry.url).toBe(pinnedHostedUrl(tmpDir));
    expect(entry.headers).toEqual({ Authorization: `Bearer ${pat}` });

    expect(result.credential).toBe('token');
    // Owner ruling (project keys, 2026-09-23): no git / version-control warnings — just write the file.
    expect(result.warnings).toHaveLength(0);
  });

  it('does NOT write a header when a token merely sits in the project config (no --token opt-in)', async () => {
    // A config carrying a token but no explicit --token opt-in. cli-core's setup-mcp never reads
    // the project config for a credential (M7) — the default config stays credential-free.
    seedConfig(tmpDir, {
      connectionMode: 'Custom',
      host: 'http://localhost:12345',
      token: CONFIG_TOKEN,
      authOption: 'required',
    });

    const result = await setupMcp({
      agentId: 'claude-code',
      unityProjectPath: tmpDir,
      transport: 'http',
      projectKeyResolver: noLogin,
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const raw = fs.readFileSync(result.configPath, 'utf-8');
    expect(raw).not.toContain('Authorization');
    expect(raw).not.toContain(CONFIG_TOKEN);
  });

  it('OAuth-capable clients default to supportsOAuth !== false in the registry', () => {
    for (const id of ['claude-code', 'cursor', 'vscode-copilot', 'codex']) {
      expect(getAgentById(id)?.supportsOAuth).not.toBe(false);
    }
  });
});

// ---------------------------------------------------------------------------
// project-keys contract §7 — a Cloud http config carries the project key for EVERY client;
// `--oauth` opts out, `--regenerate-key` mints + revokes, `--token` wins, stdio/local stay unchanged.
// ---------------------------------------------------------------------------

describe('setup-mcp — Cloud project key (project-keys §7)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-setup-mcp-pk-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it.each(['claude-code', 'cursor', 'vscode-copilot', 'codex'])(
    'writes Authorization: Bearer <project key> for %s',
    async (agentId) => {
      const { resolver, calls } = signedIn();
      const result = await setupMcp({ agentId, unityProjectPath: tmpDir, transport: 'http', projectKeyResolver: resolver });

      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;
      const raw = fs.readFileSync(result.configPath, 'utf-8');
      expect(raw).toContain(`Bearer ${PROJECT_KEY}`);
      expect(raw).toContain(`/mcp/p/${derivePinV2(path.resolve(tmpDir))}`);
      expect(raw).not.toContain('GAME_DEV_AUTH_TOKEN'); // Codex: static http_headers, no env-var indirection
      expect(result.credential).toBe('project-key');
      expect(result.projectKeyId).toBe('pk_1');
      expect(result.warnings.join('\n').toLowerCase()).not.toContain('git');

      // The key is bound to this project's pin and requested for the Unity engine.
      expect(calls).toHaveLength(1);
      expect(calls[0].pin).toBe(derivePinV2(path.resolve(tmpDir)));
      expect(calls[0].engine).toBe('unity');
      expect(calls[0].regenerate).toBe(false);
    },
  );

  it('--oauth writes the URL-only config and never resolves a key', async () => {
    const { resolver, calls } = signedIn();
    const result = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', oauth: true, projectKeyResolver: resolver,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;
    expect(fs.readFileSync(result.configPath, 'utf-8')).not.toContain('Authorization');
    expect(result.credential).toBe('none');
    expect(calls).toHaveLength(0);
  });

  it('--oauth removes a previously written project-key header', async () => {
    const { resolver } = signedIn();
    await setupMcp({ agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', projectKeyResolver: resolver });
    const result = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', oauth: true, projectKeyResolver: resolver,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;
    const raw = fs.readFileSync(result.configPath, 'utf-8');
    expect(raw).not.toContain(PROJECT_KEY);
    expect(raw).not.toContain('Authorization');
  });

  it('--regenerate-key asks for a fresh key and revokes the previous one after the write', async () => {
    const { resolver, calls, revoked } = signedIn();
    const result = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', regenerateKey: true, projectKeyResolver: resolver,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;
    expect(calls[0].regenerate).toBe(true);
    expect(result.projectKeySource).toBe('minted');
    expect(fs.readFileSync(result.configPath, 'utf-8')).toContain(`Bearer ${PROJECT_KEY}`);
    expect(revoked).toEqual(['pk_0']);
  });

  it('--regenerate-key fails when no key can be minted (nothing is written)', async () => {
    const result = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', regenerateKey: true, projectKeyResolver: noLogin,
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') return;
    expect(result.error.message).toContain('regenerate');
  });

  it('an explicit --token wins over the project key', async () => {
    const { resolver, calls } = signedIn();
    const result = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', token: 'agd_pat_explicit', projectKeyResolver: resolver,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;
    const raw = fs.readFileSync(result.configPath, 'utf-8');
    expect(raw).toContain('Bearer agd_pat_explicit');
    expect(raw).not.toContain(PROJECT_KEY);
    expect(result.credential).toBe('token');
    expect(calls).toHaveLength(0);
  });

  it('a local-server URL and the stdio transport never carry a project key', async () => {
    const { resolver, calls } = signedIn();
    const local = await setupMcp({
      agentId: 'claude-code', unityProjectPath: tmpDir, transport: 'http', url: 'http://localhost:23456', projectKeyResolver: resolver,
    });
    const stdio = await setupMcp({
      agentId: 'cursor', unityProjectPath: tmpDir, transport: 'stdio', projectKeyResolver: resolver,
    });

    for (const result of [local, stdio]) {
      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;
      expect(fs.readFileSync(result.configPath, 'utf-8')).not.toContain(PROJECT_KEY);
      expect(result.credential).toBe('none');
    }
    expect(calls).toHaveLength(0);
  });
});
