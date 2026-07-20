// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { setupMcp } from '../src/lib/setup-mcp.js';
import { getAgentById, MCP_SERVER_NAME } from '../src/utils/agents.js';
import { derivePinV2 } from '@baizor/gamedev-cli-core';
import type { UnityConnectionConfig } from '../src/utils/config.js';

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

describe('setup-mcp — credential-free OAuth config (mcp-authorize g2 / D11)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-setup-mcp-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  // DoD 1: URL-only (no Authorization header) for every OAuth-capable client,
  // even against the hosted endpoint with a config token + required auth.
  it.each(['claude-code', 'cursor', 'vscode-copilot', 'codex'])(
    'writes a URL-only config (no Authorization header) for %s',
    async (agentId) => {
      seedHostedConfig(tmpDir);

      const result = await setupMcp({
        agentId,
        unityProjectPath: tmpDir,
        transport: 'http',
      });

      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;

      const raw = fs.readFileSync(result.configPath, 'utf-8');
      // The hosted OAuth URL is present…
      expect(raw).toContain('https://ai-game.dev/mcp');
      // …but NO credential and NO Authorization header leaked into the file.
      expect(raw).not.toContain('Authorization');
      expect(raw).not.toContain(CONFIG_TOKEN);
      // No project-file-PAT warning on the credential-free default path.
      expect(result.warnings).toHaveLength(0);
    },
  );

  it('claude-code entry is exactly {type,url} with the T4-pinned URL and no headers key', async () => {
    seedHostedConfig(tmpDir);

    const result = await setupMcp({
      agentId: 'claude-code',
      unityProjectPath: tmpDir,
      transport: 'http',
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
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const root = JSON.parse(fs.readFileSync(result.configPath, 'utf-8')) as {
      mcpServers: Record<string, { url: string; headers?: Record<string, string> }>;
    };
    const entry = root.mcpServers[MCP_SERVER_NAME];
    expect(entry.url).toBe(pinnedHostedUrl(tmpDir));
    expect(entry.headers).toEqual({ Authorization: `Bearer ${pat}` });

    // Flow C credential-placement rule: warn on a project-scoped PAT.
    expect(result.warnings.some((w) => w.includes('project-scoped'))).toBe(true);
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
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;

    const raw = fs.readFileSync(result.configPath, 'utf-8');
    expect(raw).not.toContain('Authorization');
    expect(raw).not.toContain(CONFIG_TOKEN);
    expect(result.warnings).toHaveLength(0);
  });

  it('OAuth-capable clients default to supportsOAuth !== false in the registry', () => {
    for (const id of ['claude-code', 'cursor', 'vscode-copilot', 'codex']) {
      expect(getAgentById(id)?.supportsOAuth).not.toBe(false);
    }
  });
});
