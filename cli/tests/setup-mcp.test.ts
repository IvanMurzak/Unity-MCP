// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { spawn } from 'child_process';
import { CLI_PATH } from './helpers/cli.js';

function makeFakeUnityProject(dir: string): void {
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  fs.mkdirSync(path.join(dir, 'Packages'), { recursive: true });
  fs.mkdirSync(path.join(dir, 'ProjectSettings'), { recursive: true });
  fs.writeFileSync(path.join(dir, 'Packages', 'manifest.json'), '{"dependencies":{}}\n');
  fs.writeFileSync(path.join(dir, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 6000.0.0f1\n');
}

function runCliAsyncWithEnv(
  args: string[],
  env: NodeJS.ProcessEnv,
): Promise<{ stdout: string; exitCode: number }> {
  return new Promise((resolve) => {
    const child = spawn('node', [CLI_PATH, ...args], {
      stdio: 'pipe',
      env,
    });
    let stdout = '';
    let settled = false;
    const timeoutMs = 30000;

    const timeout = setTimeout(() => {
      if (settled) return;
      settled = true;
      try { child.kill(); } catch { /* noop */ }
      stdout += '\n[runCliAsyncWithEnv] Process timed out.\n';
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
      stdout += `\n[runCliAsyncWithEnv] Error: ${String(err)}\n`;
      finish(1);
    });
  });
}

describe('setup-mcp command', () => {
  let tmpProject: string;
  let tmpHome: string;

  beforeEach(() => {
    tmpProject = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-setup-mcp-project-'));
    tmpHome = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-setup-mcp-home-'));
    makeFakeUnityProject(tmpProject);
  });

  afterEach(() => {
    fs.rmSync(tmpProject, { recursive: true, force: true });
    fs.rmSync(tmpHome, { recursive: true, force: true });
  });

  it('writes Codex MCP config to the user-scoped ~/.codex/config.toml location', async () => {
    const env = {
      ...process.env,
      HOME: tmpHome,
      USERPROFILE: tmpHome,
    };

    const { stdout, exitCode } = await runCliAsyncWithEnv([
      'setup-mcp',
      'codex',
      tmpProject,
      '--transport',
      'http',
    ], env);

    const globalConfigPath = path.join(tmpHome, '.codex', 'config.toml');
    const projectConfigPath = path.join(tmpProject, '.codex', 'config.toml');

    expect(exitCode).toBe(0);
    expect(stdout).toContain('Codex configured successfully');
    expect(stdout).toContain(globalConfigPath);
    expect(fs.existsSync(globalConfigPath)).toBe(true);
    expect(fs.existsSync(projectConfigPath)).toBe(false);
    expect(fs.readFileSync(globalConfigPath, 'utf-8')).toContain('[mcp_servers.ai-game-developer]');
  });

  it('keeps Claude Code MCP config project-scoped via .mcp.json', async () => {
    const env = {
      ...process.env,
      HOME: tmpHome,
      USERPROFILE: tmpHome,
    };

    const { stdout, exitCode } = await runCliAsyncWithEnv([
      'setup-mcp',
      'claude-code',
      tmpProject,
      '--transport',
      'http',
    ], env);

    const projectConfigPath = path.join(tmpProject, '.mcp.json');
    const globalConfigPath = path.join(tmpHome, '.mcp.json');

    expect(exitCode).toBe(0);
    expect(stdout).toContain('Claude Code configured successfully');
    expect(stdout).toContain(projectConfigPath);
    expect(fs.existsSync(projectConfigPath)).toBe(true);
    expect(fs.existsSync(globalConfigPath)).toBe(false);
    expect(fs.readFileSync(projectConfigPath, 'utf-8')).toContain('"ai-game-developer"');
  });
});
