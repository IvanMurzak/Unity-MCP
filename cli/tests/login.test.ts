// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { runCliAsync } from './helpers/cli.js';

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

  it('shows already authenticated when cloudToken exists', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-login-test-'));
    try {
      fs.mkdirSync(path.join(tmpDir, 'Assets'), { recursive: true });
      const configDir = path.join(tmpDir, 'UserSettings');
      fs.mkdirSync(configDir, { recursive: true });
      fs.writeFileSync(
        path.join(configDir, 'AI-Game-Developer-Config.json'),
        JSON.stringify({ connectionMode: 'Cloud', cloudToken: 'existing-token' }),
      );

      const { stdout, exitCode } = await runCliAsync(['login', tmpDir]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('Already authenticated');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});
