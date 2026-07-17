// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { runCliAsync } from './helpers/cli.js';

// ---------------------------------------------------------------------------
// login now signs in to the shared machine credential store (~/.ai-game-dev/credentials.json)
// instead of writing a per-project cloudToken. --project routes the credential to a project-local
// store (<path>/.ai-game-dev/). These CLI-subprocess tests only exercise the offline
// "already signed in" short-circuit + --help; the write path (which needs the device flow) is
// covered offline by tests/cloud-login.test.ts with a mocked deviceAuthFlow.
// ---------------------------------------------------------------------------

describe('login command', () => {
  it('shows help with --help (documents --force and --project)', async () => {
    const { stdout, exitCode } = await runCliAsync(['login', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('login');
    expect(stdout).toContain('--force');
    expect(stdout).toContain('--project');
  });

  it('reports "Already signed in" when a --project store credential exists (offline short-circuit)', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-login-test-'));
    try {
      // Pre-create a project-local store credential. login only checks presence (never decrypts
      // for the "already signed in" gate), so a plaintext stub is enough to drive the short-circuit
      // without any network call.
      const storeDir = path.join(tmpDir, '.ai-game-dev');
      fs.mkdirSync(storeDir, { recursive: true });
      fs.writeFileSync(
        path.join(storeDir, 'credentials.json'),
        JSON.stringify({ version: 1, accessToken: 'existing-token' }),
      );

      const { stdout, exitCode } = await runCliAsync(['login', '--project', tmpDir]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('Already signed in');
      expect(stdout).toContain('Use --force to re-authenticate');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('a legacy project cloudToken config alone does NOT count as signed-in', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-login-test-'));
    try {
      // Legacy per-project cloudToken config present, but NO project-store credential. login's
      // sign-in gate consults ONLY the credential store (never the project config), so
      // store.exists stays false — the legacy cloudToken is structurally ignored. Asserted
      // offline (no CLI subprocess / device flow) to avoid firing the real cloud endpoint.
      const cfgDir = path.join(tmpDir, 'UserSettings');
      fs.mkdirSync(cfgDir, { recursive: true });
      fs.writeFileSync(
        path.join(cfgDir, 'AI-Game-Developer-Config.json'),
        JSON.stringify({ connectionMode: 'Cloud', cloudToken: 'legacy-token' }),
      );

      const { MachineCredentialStore } = await import('../src/utils/machine-credentials.js');
      const store = new MachineCredentialStore(path.join(tmpDir, '.ai-game-dev'));
      expect(store.exists).toBe(false);
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});
