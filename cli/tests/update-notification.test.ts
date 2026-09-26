// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, vi, afterEach } from 'vitest';
import { printUpdateNotification, UPDATE_COMMAND } from '../src/utils/update-check.js';

// eslint-disable-next-line no-control-regex
const stripAnsi = (s: string): string => s.replace(/\x1b\[[0-9;]*m/g, '');

function captureNotification(): string[] {
  const lines: string[] = [];
  const spy = vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
    lines.push(stripAnsi(args.map(String).join(' ')));
  });
  try {
    printUpdateNotification('0.1.0', '0.2.0');
  } finally {
    spy.mockRestore();
  }
  return lines;
}

describe('update notification', () => {
  afterEach(() => vi.restoreAllMocks());

  it('installs exactly one package when the hint line is copied after its colon', () => {
    // Regression: "Run npm i -g unity-mcp-cli to update" was copy-pasted whole,
    // so npm also installed the unrelated `to` and `update` packages (the
    // latter pulls in ~600 deprecated deps: set-value, glob@5, rimraf@2, ...).
    const hint = captureNotification().find((l) => l.includes('npm i -g'));
    expect(hint).toBeDefined();
    const command = hint!.slice(hint!.indexOf('npm i -g')).trim();
    expect(command).toBe(UPDATE_COMMAND);
    const packages = command.split(/\s+/).slice(3);
    expect(packages).toEqual(['unity-mcp-cli@latest']);
  });
});
