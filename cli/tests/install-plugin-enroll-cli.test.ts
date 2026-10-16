// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import { runCliAsync } from './helpers/cli.js';

// Wiring smoke: confirm the new install-plugin / configure options are registered and reachable
// through the real CLI entry point (no network — --help only).

describe('install-plugin new option wiring', () => {
  it('exposes --with-server / --server-version / --server-source / --enroll / --enroll-stdin', async () => {
    const { stdout, exitCode } = await runCliAsync(['install-plugin', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--with-server');
    expect(stdout).toContain('--server-version');
    expect(stdout).toContain('--server-source');
    expect(stdout).toContain('--enroll');
    expect(stdout).toContain('--enroll-stdin');
  });
});

describe('configure new option wiring', () => {
  it('exposes --agent', async () => {
    const { stdout, exitCode } = await runCliAsync(['configure', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--agent');
  });
});
