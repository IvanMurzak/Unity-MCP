// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as net from 'net';
import * as os from 'os';
import * as path from 'path';
import { EXIT_CODES } from '@baizor/gamedev-cli-core';
import { runCliAsync } from './helpers/cli.js';
import { writeCliSkills } from '../src/lib/setup-skills.js';
import { buildGuideFiles, buildInstallGuide, EXIT_CODE_ACTIONS } from '../src/lib/skill-guides.js';
import { ROOT_RQL_RECIPES, RQL_LANGUAGE, RQL_RECIPES } from '../src/lib/cli-guide.js';

const FILES = ['unity-mcp-cli/SKILL.md', 'unity-mcp-cli/references/rql.md', 'unity-mcp-cli/references/install.md', 'ai-game-dev-app/SKILL.md'];
const skillMd = () => buildGuideFiles().find((f) => f.relativePath === 'unity-mcp-cli/SKILL.md')!.content;

/** A loopback port nothing listens on (an editor that is closed). */
async function closedPort(): Promise<number> {
  const server = net.createServer();
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const port = (server.address() as net.AddressInfo).port;
  await new Promise<void>((resolve) => server.close(() => resolve()));
  return port;
}

describe('the unity-mcp-cli skill', () => {
  it('is the CLI skill, its on-demand references and the App guide', () => {
    expect(buildGuideFiles().map((f) => f.relativePath)).toEqual(FILES);
    for (const file of buildGuideFiles().filter((f) => f.relativePath.endsWith('SKILL.md'))) {
      const name = file.relativePath.split('/')[0];
      expect(file.content.startsWith(`---\nname: ${name}\ndescription: "`)).toBe(true);
      expect(file.content).toContain('metadata:\n  generated-by: unity-mcp\n  ai-game-dev: exclude\n---');
    }
  });

  it('shows the root help\'s recipes; its RQL reference has them all and the language (one source)', () => {
    RQL_RECIPES.forEach(([query, what], i) => {
      expect(skillMd().includes(`rql '${query}'  # ${what}`)).toBe(i < ROOT_RQL_RECIPES);
    });
    const ref = buildGuideFiles().find((f) => f.relativePath === 'unity-mcp-cli/references/rql.md')!.content;
    for (const [query] of RQL_RECIPES) expect(ref).toContain(`rql '${query}'`);
    for (const line of RQL_LANGUAGE) expect(ref).toContain(line);
  });

  it('advises on every failing exit code of the shared contract', () => {
    const failing = EXIT_CODES.map((e) => e.code).filter((c) => c > 1).sort((a, b) => a - b);
    expect(Object.keys(EXIT_CODE_ACTIONS).map(Number)).toEqual(failing);
    for (const code of failing) expect(skillMd().split('\n').find((l) => l.startsWith('Exit codes: '))).toContain(` ${code} `);
  });

  it('teaches the cold-start workflow, not the retired one', () => {
    expect(skillMd()).toContain('install-plugin');
    expect(skillMd()).toContain('dialog --press');
    expect(skillMd()).not.toContain('rql-schema --input');
    expect(skillMd()).not.toMatch(/editor must be running/i);
  });

  it('stays short (the agent re-reads every byte each turn) and uses neutral example names', () => {
    expect(Buffer.byteLength(skillMd())).toBeLessThan(2600);
    const all = buildGuideFiles().map((f) => f.content).join('\n');
    expect(all).not.toMatch(/\b(Arena|Crate|Platform|Level1|EnemyStats|GameMath)\b/);
  });

  for (const platform of ['win32', 'linux', 'darwin'] as const) {
    it(`writes only the ${platform} commands into the install reference`, () => {
      const guide = buildInstallGuide(platform);
      const windows = platform === 'win32';
      expect(guide).toContain(windows ? '```powershell' : '```bash');
      expect(guide).not.toContain(windows ? '```bash' : '```powershell');
      expect(guide).toContain(windows ? 'npm.cmd install -g unity-mcp-cli' : 'npm install -g unity-mcp-cli');
      expect(guide).not.toMatch(/@[A-Z_]+@/);
    });
  }
});

describe('setup-skills without an editor', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cli-skill-'));
    fs.mkdirSync(path.join(tmpDir, 'Assets'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('writeCliSkills writes the files once and leaves them alone when unchanged', () => {
    const first = writeCliSkills({ projectPath: tmpDir, agentId: 'cursor' });
    expect(first.kind === 'success' && first.filesWritten.length).toBe(FILES.length);
    expect(fs.existsSync(path.join(tmpDir, '.cursor', 'skills', 'unity-mcp-cli', 'SKILL.md'))).toBe(true);
    const second = writeCliSkills({ projectPath: tmpDir, agentId: 'cursor' });
    expect(second.kind === 'success' && second.filesWritten).toEqual([]);
    const none = writeCliSkills({ projectPath: tmpDir, agentId: 'claude-desktop' });
    expect(none.kind === 'failure' && none.error.message).toContain('does not support skills');
  });

  it('a closed editor is not an error: the CLI skill is written, exit 0', async () => {
    const port = await closedPort();
    const { stdout, exitCode } = await runCliAsync(['setup-skills', 'claude-code', tmpDir, '--url', `http://127.0.0.1:${port}`, '--timeout', '5000']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('No Unity Editor answered');
    expect(fs.readFileSync(path.join(tmpDir, '.claude', 'skills', 'unity-mcp-cli', 'SKILL.md'), 'utf-8')).toBe(skillMd());
  }, 20000);

  it('--offline writes the CLI skill without asking any editor', async () => {
    const { stdout, exitCode } = await runCliAsync(['setup-skills', 'claude-code', tmpDir, '--offline']);
    expect(exitCode).toBe(0);
    expect(stdout).not.toContain('Asking the editor');
    expect(fs.existsSync(path.join(tmpDir, '.claude', 'skills', 'unity-mcp-cli', 'references', 'rql.md'))).toBe(true);
  }, 20000);
});
