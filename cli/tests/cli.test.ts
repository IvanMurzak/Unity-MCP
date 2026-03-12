import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp.js');

function runCli(args: string[], options?: { cwd?: string }): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
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

describe('CLI integration', () => {
  // --- Global CLI behavior ---

  describe('global options', () => {
    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('unity-mcp-cli');
      expect(stdout).toContain('create-project');
      expect(stdout).toContain('install-editor');
      expect(stdout).toContain('open');
      expect(stdout).toContain('install-plugin');
      expect(stdout).toContain('configure');
      expect(stdout).toContain('connect');
    });

    it('shows version with --version', () => {
      const { stdout, exitCode } = runCli(['--version']);
      expect(exitCode).toBe(0);
      expect(stdout.trim()).toMatch(/^\d+\.\d+\.\d+$/);
    });
  });

  // --- install-plugin command ---

  describe('install-plugin', () => {
    let tmpDir: string;

    beforeEach(() => {
      tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cli-test-'));
      fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
    });

    afterEach(() => {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    });

    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['install-plugin', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--project-path');
      expect(stdout).toContain('--plugin-version');
    });

    it('fails when project path has no manifest.json', () => {
      const emptyDir = fs.mkdtempSync(path.join(os.tmpdir(), 'empty-'));
      const { stdout, exitCode } = runCli([
        'install-plugin',
        '--project-path', emptyDir,
        '--plugin-version', '0.51.6',
      ]);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('Not a valid Unity project');
      fs.rmSync(emptyDir, { recursive: true, force: true });
    });

    it('installs plugin into a fresh manifest', () => {
      fs.writeFileSync(
        path.join(tmpDir, 'Packages', 'manifest.json'),
        JSON.stringify({ dependencies: {} }, null, 2)
      );

      const { stdout, exitCode } = runCli([
        'install-plugin',
        '--project-path', tmpDir,
        '--plugin-version', '0.51.6',
      ]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('Installing Unity-MCP plugin');

      const manifest = JSON.parse(
        fs.readFileSync(path.join(tmpDir, 'Packages', 'manifest.json'), 'utf-8')
      );
      expect(manifest.dependencies['com.ivanmurzak.unity.mcp']).toBe('0.51.6');
      expect(manifest.scopedRegistries).toBeDefined();
      expect(manifest.scopedRegistries[0].name).toBe('package.openupm.com');
    });
  });

  // --- configure command ---

  describe('configure', () => {
    let tmpDir: string;

    beforeEach(() => {
      tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cli-test-'));
    });

    afterEach(() => {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    });

    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['configure', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--enable-tools');
      expect(stdout).toContain('--disable-tools');
      expect(stdout).toContain('--list');
    });

    it('creates default config and lists it', () => {
      const { stdout, exitCode } = runCli([
        'configure',
        '--project-path', tmpDir,
        '--list',
      ]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('Current configuration');
      expect(stdout).toContain('Host:');
      expect(stdout).toContain('localhost');
    });

    it('enables and disables tools', () => {
      // First create default config
      runCli(['configure', '--project-path', tmpDir, '--enable-tools', 'tool-a,tool-b']);

      const { stdout } = runCli(['configure', '--project-path', tmpDir, '--list']);
      expect(stdout).toContain('[enabled] tool-a');
      expect(stdout).toContain('[enabled] tool-b');

      // Now disable one
      runCli(['configure', '--project-path', tmpDir, '--disable-tools', 'tool-a']);
      const { stdout: stdout2 } = runCli(['configure', '--project-path', tmpDir, '--list']);
      expect(stdout2).toContain('[disabled] tool-a');
      expect(stdout2).toContain('[enabled] tool-b');
    });

    it('fails when project path does not exist', () => {
      const { exitCode, stdout } = runCli([
        'configure',
        '--project-path', '/nonexistent/path/12345',
        '--list',
      ]);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('does not exist');
    });
  });

  // --- connect command ---

  describe('connect', () => {
    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['connect', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--url');
      expect(stdout).toContain('--tools');
      expect(stdout).toContain('--token');
      expect(stdout).toContain('--auth');
      expect(stdout).toContain('--keep-connected');
    });

    it('requires --project-path and --url', () => {
      const { exitCode, stdout } = runCli(['connect']);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('--project-path');
    });
  });

  // --- open command ---

  describe('open', () => {
    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['open', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--project-path');
      expect(stdout).toContain('--editor-version');
    });
  });

  // --- create-project command ---

  describe('create-project', () => {
    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['create-project', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--path');
      expect(stdout).toContain('--editor-version');
    });
  });

  // --- install-editor command ---

  describe('install-editor', () => {
    it('shows help with --help', () => {
      const { stdout, exitCode } = runCli(['install-editor', '--help']);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('--version');
      expect(stdout).toContain('--project-path');
    });
  });
});
