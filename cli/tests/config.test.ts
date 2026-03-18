import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import {
  createDefaultConfig,
  readConfig,
  writeConfig,
  getOrCreateConfig,
  updateFeatures,
  type UnityConnectionConfig,
  type McpFeature,
} from '../src/utils/config.js';

describe('config', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-config-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  // --- createDefaultConfig ---

  describe('createDefaultConfig', () => {
    it('returns a config with a valid host URL', () => {
      const config = createDefaultConfig(tmpDir);
      expect(config.host).toMatch(/^http:\/\/localhost:\d+$/);
    });

    it('returns a config with a port in range 20000-29999', () => {
      const config = createDefaultConfig(tmpDir);
      const port = parseInt(config.host!.split(':').pop()!, 10);
      expect(port).toBeGreaterThanOrEqual(20000);
      expect(port).toBeLessThanOrEqual(29999);
    });

    it('returns correct default values', () => {
      const config = createDefaultConfig(tmpDir);
      expect(config.keepConnected).toBe(false);
      expect(config.logLevel).toBe(3);
      expect(config.timeoutMs).toBe(10000);
      expect(config.keepServerRunning).toBe(false);
      expect(config.transportMethod).toBe('streamableHttp');
      expect(config.authOption).toBe('none');
      expect(config.connectionMode).toBe('Custom');
      expect(config.tools).toEqual([]);
      expect(config.prompts).toEqual([]);
      expect(config.resources).toEqual([]);
    });

    it('is deterministic for the same path', () => {
      const config1 = createDefaultConfig(tmpDir);
      const config2 = createDefaultConfig(tmpDir);
      expect(config1.host).toBe(config2.host);
    });
  });

  // --- readConfig / writeConfig ---

  describe('readConfig / writeConfig', () => {
    it('returns null when config file does not exist', () => {
      const result = readConfig(tmpDir);
      expect(result).toBeNull();
    });

    it('writes and reads config correctly', () => {
      const config: UnityConnectionConfig = {
        host: 'http://localhost:50000',
        keepConnected: true,
        logLevel: 2,
        timeoutMs: 5000,
        tools: [{ name: 'test-tool', enabled: true }],
        prompts: [],
        resources: [],
      };
      writeConfig(tmpDir, config);
      const result = readConfig(tmpDir);
      expect(result).toBeDefined();
      expect(result!.host).toBe('http://localhost:50000');
      expect(result!.keepConnected).toBe(true);
      expect(result!.logLevel).toBe(2);
      expect(result!.timeoutMs).toBe(5000);
      expect(result!.tools).toEqual([{ name: 'test-tool', enabled: true }]);
    });

    it('creates UserSettings directory if needed', () => {
      const nestedDir = path.join(tmpDir, 'deep', 'nested');
      fs.mkdirSync(nestedDir, { recursive: true });
      const config = createDefaultConfig(nestedDir);
      writeConfig(nestedDir, config);

      const configPath = path.join(nestedDir, 'UserSettings', 'AI-Game-Developer-Config.json');
      expect(fs.existsSync(configPath)).toBe(true);
    });

    it('writes pretty-printed JSON', () => {
      const config = createDefaultConfig(tmpDir);
      writeConfig(tmpDir, config);
      const content = fs.readFileSync(
        path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json'),
        'utf-8'
      );
      expect(content).toContain('\n');
      expect(content).toContain('  ');
    });
  });

  // --- getOrCreateConfig ---

  describe('getOrCreateConfig', () => {
    it('creates a default config when none exists', () => {
      const config = getOrCreateConfig(tmpDir);
      expect(config).toBeDefined();
      expect(config.host).toMatch(/^http:\/\/localhost:\d+$/);
      // Should also have written the file
      const onDisk = readConfig(tmpDir);
      expect(onDisk).toBeDefined();
    });

    it('returns existing config when file exists', () => {
      const original: UnityConnectionConfig = {
        host: 'http://localhost:12345',
        keepConnected: true,
        tools: [{ name: 'custom-tool', enabled: false }],
      };
      writeConfig(tmpDir, original);
      const config = getOrCreateConfig(tmpDir);
      expect(config.host).toBe('http://localhost:12345');
      expect(config.keepConnected).toBe(true);
      expect(config.tools).toEqual([{ name: 'custom-tool', enabled: false }]);
    });
  });

  // --- updateFeatures ---

  describe('updateFeatures', () => {
    it('enables specific tools by name', () => {
      const config: UnityConnectionConfig = {
        tools: [
          { name: 'tool-a', enabled: false },
          { name: 'tool-b', enabled: false },
        ],
      };
      updateFeatures(config, 'tools', { enableNames: ['tool-a'] });
      expect(config.tools![0].enabled).toBe(true);
      expect(config.tools![1].enabled).toBe(false);
    });

    it('disables specific tools by name', () => {
      const config: UnityConnectionConfig = {
        tools: [
          { name: 'tool-a', enabled: true },
          { name: 'tool-b', enabled: true },
        ],
      };
      updateFeatures(config, 'tools', { disableNames: ['tool-b'] });
      expect(config.tools![0].enabled).toBe(true);
      expect(config.tools![1].enabled).toBe(false);
    });

    it('adds new tool entry when enabling a non-existing tool', () => {
      const config: UnityConnectionConfig = { tools: [] };
      updateFeatures(config, 'tools', { enableNames: ['new-tool'] });
      expect(config.tools!.length).toBe(1);
      expect(config.tools![0]).toEqual({ name: 'new-tool', enabled: true });
    });

    it('adds new tool entry when disabling a non-existing tool', () => {
      const config: UnityConnectionConfig = { tools: [] };
      updateFeatures(config, 'tools', { disableNames: ['new-tool'] });
      expect(config.tools!.length).toBe(1);
      expect(config.tools![0]).toEqual({ name: 'new-tool', enabled: false });
    });

    it('enables all tools', () => {
      const config: UnityConnectionConfig = {
        tools: [
          { name: 'tool-a', enabled: false },
          { name: 'tool-b', enabled: false },
          { name: 'tool-c', enabled: false },
        ],
      };
      updateFeatures(config, 'tools', { enableAll: true });
      for (const tool of config.tools!) {
        expect(tool.enabled).toBe(true);
      }
    });

    it('disables all tools', () => {
      const config: UnityConnectionConfig = {
        tools: [
          { name: 'tool-a', enabled: true },
          { name: 'tool-b', enabled: true },
        ],
      };
      updateFeatures(config, 'tools', { disableAll: true });
      for (const tool of config.tools!) {
        expect(tool.enabled).toBe(false);
      }
    });

    it('works with prompts', () => {
      const config: UnityConnectionConfig = {
        prompts: [{ name: 'prompt-a', enabled: false }],
      };
      updateFeatures(config, 'prompts', { enableNames: ['prompt-a'] });
      expect(config.prompts![0].enabled).toBe(true);
    });

    it('works with resources', () => {
      const config: UnityConnectionConfig = {
        resources: [{ name: 'resource-a', enabled: true }],
      };
      updateFeatures(config, 'resources', { disableNames: ['resource-a'] });
      expect(config.resources![0].enabled).toBe(false);
    });

    it('handles both enable and disable in the same call', () => {
      const config: UnityConnectionConfig = {
        tools: [
          { name: 'tool-a', enabled: false },
          { name: 'tool-b', enabled: true },
        ],
      };
      updateFeatures(config, 'tools', {
        enableNames: ['tool-a'],
        disableNames: ['tool-b'],
      });
      expect(config.tools![0]).toEqual({ name: 'tool-a', enabled: true });
      expect(config.tools![1]).toEqual({ name: 'tool-b', enabled: false });
    });

    it('handles undefined feature list gracefully', () => {
      const config: UnityConnectionConfig = {};
      updateFeatures(config, 'tools', { enableNames: ['new-tool'] });
      expect(config.tools!.length).toBe(1);
      expect(config.tools![0]).toEqual({ name: 'new-tool', enabled: true });
    });
  });
});
