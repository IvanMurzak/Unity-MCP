// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import type { SetupMcpResult } from '../src/lib/types.js';

/**
 * The `setup-mcp` COMMAND's rendering of a result. The `--regenerate-key` outputs (the configs moved to
 * the new key, and the warnings a failed write carries) need a signed-in machine, so no subprocess test
 * in cli.test.ts can reach them — the library result is stubbed here instead.
 */
const setupMcpMock = vi.fn<(opts: unknown) => Promise<SetupMcpResult>>();
vi.mock('../src/lib/setup-mcp.js', () => ({ setupMcp: (opts: unknown) => setupMcpMock(opts) }));

const { setupMcpCommand } = await import('../src/commands/setup-mcp.js');

describe('setup-mcp command — result rendering', () => {
  let out: string[];

  beforeEach(() => {
    out = [];
    const capture = (...args: unknown[]): void => {
      out.push(args.map(String).join(' '));
    };
    vi.spyOn(console, 'log').mockImplementation(capture);
    vi.spyOn(console, 'error').mockImplementation(capture);
    vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`process.exit(${code})`);
    }) as never);
    setupMcpMock.mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const run = (): Promise<unknown> => setupMcpCommand.parseAsync(['cursor'], { from: 'user' });

  it('prints every config file written and every config moved to the new key', async () => {
    setupMcpMock.mockResolvedValue({
      kind: 'success', success: true, agentId: 'cursor',
      configPath: '/p/.cursor/mcp.json', configPaths: ['/p/.cursor/mcp.json'],
      rewrittenConfigPaths: ['/home/.gemini/config/mcp_config.json', '/home/.gemini/antigravity/mcp_config.json'],
      transport: 'http', credential: 'project-key', projectKeyId: 'pk_1', projectKeySource: 'minted',
      warnings: [], nextSteps: [],
    });

    await run();

    const text = out.join('\n');
    expect(text).toMatch(/Config file:.*\/p\/\.cursor\/mcp\.json/);
    expect(text).toMatch(/Moved to new key:.*\/home\/\.gemini\/config\/mcp_config\.json/);
    expect(text).toMatch(/Moved to new key:.*\/home\/\.gemini\/antigravity\/mcp_config\.json/);
  });

  it('prints the failure warnings (previous key left active) before exiting 1', async () => {
    const warning = 'The config(s) /p/x.json could not be written with the new project key, so the previous project key was left active.';
    setupMcpMock.mockResolvedValue({
      kind: 'failure', success: false, warnings: [warning], nextSteps: [],
      error: new Error('Could not write the Cursor config /p/.cursor/mcp.json.'),
    });

    await expect(run()).rejects.toThrow('process.exit(1)');

    const text = out.join('\n');
    expect(text).toContain('Could not write the Cursor config');
    expect(text).toContain(warning);
  });
});
