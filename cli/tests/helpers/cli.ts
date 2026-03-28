// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as path from 'path';
import { spawn } from 'child_process';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const CLI_PATH = path.resolve(__dirname, '..', '..', 'bin', 'unity-mcp-cli.js');

/** Run the CLI as a child process with timeout and error handling. */
export function runCliAsync(args: string[]): Promise<{ stdout: string; exitCode: number }> {
  return new Promise((resolve) => {
    const child = spawn('node', [CLI_PATH, ...args], { stdio: 'pipe' });
    let stdout = '';
    let settled = false;
    const timeoutMs = 30000;

    const timeout = setTimeout(() => {
      if (settled) return;
      settled = true;
      try { child.kill(); } catch { /* noop */ }
      stdout += '\n[runCliAsync] Process timed out.\n';
      resolve({ stdout, exitCode: 1 });
    }, timeoutMs);

    const finish = (exitCode: number) => {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      resolve({ stdout, exitCode });
    };

    child.stdout?.on('data', (d: Buffer) => { stdout += d.toString(); });
    child.stderr?.on('data', (d: Buffer) => { stdout += d.toString(); });
    child.on('close', (code) => { finish(code ?? 0); });
    child.on('error', (err) => {
      stdout += `\n[runCliAsync] Error: ${String(err)}\n`;
      finish(1);
    });
  });
}
