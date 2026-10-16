import { describe, it, expect, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';

// The CLI-surface half of `installExtension`'s coverage: the library tests drive the
// function in-process, these drive the real `unity-mcp-cli install-extension` binary so
// the commander wiring (registration, argument order, --path, --extension-version, --list,
// exit codes) is exercised end to end. Mirrors tests/cli.test.ts's runner.
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');
const TILEMAP_ID = 'com.ivanmurzak.unity.mcp.tilemap';

function runCli(args: string[], options?: { cwd?: string }): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 20000,
      cwd: options?.cwd,
    });
    return { stdout, exitCode: 0 };
  } catch (err: unknown) {
    const error = err as { stdout?: string; stderr?: string; status?: number };
    return {
      stdout: (error.stdout ?? '') + (error.stderr ?? ''),
      exitCode: error.status ?? 1,
    };
  }
}

const tmpDirs: string[] = [];
function mkUnityProject(): string {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-cli-'));
  tmpDirs.push(tmpDir);
  fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
  fs.writeFileSync(
    path.join(tmpDir, 'Packages', 'manifest.json'),
    JSON.stringify({ dependencies: {} }, null, 2),
  );
  return tmpDir;
}

function readManifest(projectPath: string): {
  dependencies?: Record<string, string>;
  scopedRegistries?: { name: string; url: string; scopes: string[] }[];
} {
  return JSON.parse(fs.readFileSync(path.join(projectPath, 'Packages', 'manifest.json'), 'utf-8'));
}

afterEach(() => {
  while (tmpDirs.length) {
    const dir = tmpDirs.pop();
    if (dir) fs.rmSync(dir, { recursive: true, force: true });
  }
});

describe('install-extension CLI — registration and help', () => {
  it('is registered on the root command', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('install-extension');
  });

  it('documents its arguments and options', () => {
    const { stdout, exitCode } = runCli(['install-extension', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--extension-version');
    expect(stdout).toContain('--path');
    expect(stdout).toContain('--list');
  });

  // REGRESSION GUARD for a real bug found by these CLI tests and invisible to the
  // in-process library tests: the flag was originally spelled `--version`, which the ROOT
  // program's `.version()` option intercepts. `install-extension Tilemap --version 1.0.16`
  // printed the CLI's own version ("0.88.0") and exited 0 — a silent no-op install on the
  // primary documented flag. Pin the observable so a rename back cannot pass.
  it('the version flag installs rather than printing the CLI version', () => {
    const project = mkUnityProject();
    const cliVersion = runCli(['--version']).stdout.trim();
    expect(cliVersion).toMatch(/^\d+\.\d+\.\d+/);

    const { stdout, exitCode } = runCli([
      'install-extension',
      'Tilemap',
      project,
      '--extension-version',
      '1.0.16',
    ]);

    expect(exitCode).toBe(0);
    expect(stdout.trim()).not.toBe(cliVersion);
    // The positive artifact: the dependency really landed. An assertion that stdout merely
    // "is not the version string" could be satisfied by any other output, including a
    // different no-op.
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });
});

describe('install-extension CLI — --list', () => {
  it('lists the shipped extensions without needing a project or the network', () => {
    // Run from a directory that is NOT a Unity project: --list must not require one.
    const notAProject = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-cli-none-'));
    tmpDirs.push(notAProject);

    const { stdout, exitCode } = runCli(['install-extension', '--list'], { cwd: notAProject });
    expect(exitCode).toBe(0);
    expect(stdout).toContain(TILEMAP_ID);
    expect(stdout).toContain('com.ivanmurzak.unity.mcp.cinemachine');
    expect(stdout).toContain('Timeline');
  });
});

describe('install-extension CLI — failure exit codes', () => {
  it('exits 1 with a listing hint when no id is given', () => {
    const project = mkUnityProject();
    const { stdout, exitCode } = runCli(['install-extension'], { cwd: project });
    expect(exitCode).toBe(1);
    expect(stdout).toContain('--list');
  });

  it('exits 1 for an unknown extension id', () => {
    const project = mkUnityProject();
    const { stdout, exitCode } = runCli(['install-extension', 'com.example.nope', project]);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Unknown extension');
  });

  it('exits 1 when the target directory is not a Unity project', () => {
    const notAProject = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-cli-none-'));
    tmpDirs.push(notAProject);
    const { exitCode } = runCli([
      'install-extension',
      'Tilemap',
      notAProject,
      '--extension-version',
      '1.0.16',
    ]);
    expect(exitCode).toBe(1);
  });
});

describe('install-extension CLI — real installs', () => {
  // An explicit --extension-version keeps these offline: the version-resolution path is
  // covered in-process (with a stubbed fetch) by tests/install-extension.test.ts.
  it('installs into the positional path and patches the manifest', () => {
    const project = mkUnityProject();
    const { stdout, exitCode } = runCli([
      'install-extension',
      'Tilemap',
      project,
      '--extension-version',
      '1.0.16',
    ]);

    expect(exitCode).toBe(0);
    expect(stdout).toContain(TILEMAP_ID);

    const manifest = readManifest(project);
    expect(manifest.dependencies?.[TILEMAP_ID]).toBe('1.0.16');
    const registry = manifest.scopedRegistries?.find((r) => r.name === 'package.openupm.com');
    expect(registry?.scopes).toContain('com.ivanmurzak');
  });

  it('accepts the project via --path', () => {
    const project = mkUnityProject();
    const { exitCode } = runCli([
      'install-extension',
      'Tilemap',
      '--path',
      project,
      '--extension-version',
      '1.0.16',
    ]);
    expect(exitCode).toBe(0);
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('defaults the project to the current directory', () => {
    const project = mkUnityProject();
    const { exitCode } = runCli(['install-extension', 'Tilemap', '--extension-version', '1.0.16'], {
      cwd: project,
    });
    expect(exitCode).toBe(0);
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('accepts a full package id as well as the display name', () => {
    const project = mkUnityProject();
    const { exitCode } = runCli([
      'install-extension',
      TILEMAP_ID,
      project,
      '--extension-version',
      '1.0.16',
    ]);
    expect(exitCode).toBe(0);
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('is idempotent across process boundaries (exit 0, up-to-date on the second run)', () => {
    const project = mkUnityProject();
    const first = runCli(['install-extension', 'Tilemap', project, '--extension-version', '1.0.16']);
    expect(first.exitCode).toBe(0);

    const second = runCli(['install-extension', 'Tilemap', project, '--extension-version', '1.0.16']);
    expect(second.exitCode).toBe(0);
    expect(second.stdout).toContain('already up to date');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });
});
