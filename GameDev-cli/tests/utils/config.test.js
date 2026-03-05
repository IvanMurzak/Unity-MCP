import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';

// Mock the 'fs' module before importing the module under test
vi.mock('fs');

import fs from 'fs';
import {
  getConfigPath,
  readConfig,
  writeConfig,
  DEFAULT_CONFIG,
  CONFIG_RELATIVE_PATH,
} from '../../src/utils/config.js';

const FAKE_PROJECT = '/fake/unity/project';
const EXPECTED_CONFIG_PATH = path.join(FAKE_PROJECT, CONFIG_RELATIVE_PATH);

describe('getConfigPath', () => {
  it('returns the correct config path for a project', () => {
    const result = getConfigPath(FAKE_PROJECT);
    expect(result).toBe(EXPECTED_CONFIG_PATH);
  });

  it('handles paths with trailing slashes', () => {
    const result = getConfigPath('/some/project/');
    expect(result).toContain('AI-Game-Developer-Config.json');
  });
});

describe('readConfig', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('returns default config when file does not exist', () => {
    fs.existsSync.mockReturnValue(false);

    const result = readConfig(FAKE_PROJECT);

    expect(result).toEqual(DEFAULT_CONFIG);
  });

  it('reads and parses existing config file', () => {
    const customConfig = {
      host: 'http://myserver:9090',
      timeoutMs: 5000,
      tools: [{ name: 'gameobject-create', enabled: true }],
    };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(customConfig));

    const result = readConfig(FAKE_PROJECT);

    expect(result.host).toBe('http://myserver:9090');
    expect(result.timeoutMs).toBe(5000);
    expect(result.tools).toEqual([{ name: 'gameobject-create', enabled: true }]);
  });

  it('merges custom config with defaults (custom values take precedence)', () => {
    const partialConfig = { host: 'http://custom:8080' };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(partialConfig));

    const result = readConfig(FAKE_PROJECT);

    expect(result.host).toBe('http://custom:8080');
    expect(result.logLevel).toBe(DEFAULT_CONFIG.logLevel);
    expect(result.keepConnected).toBe(DEFAULT_CONFIG.keepConnected);
  });

  it('returns default config when file contains invalid JSON', () => {
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue('{ this is not valid json }');
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const result = readConfig(FAKE_PROJECT);

    expect(result).toEqual(DEFAULT_CONFIG);
    expect(warnSpy).toHaveBeenCalled();
    warnSpy.mockRestore();
  });

  it('returns default config when fs.readFileSync throws', () => {
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockImplementation(() => { throw new Error('Permission denied'); });
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const result = readConfig(FAKE_PROJECT);

    expect(result).toEqual(DEFAULT_CONFIG);
    warnSpy.mockRestore();
  });

  it('calls readFileSync with the correct path and encoding', () => {
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue('{}');

    readConfig(FAKE_PROJECT);

    expect(fs.readFileSync).toHaveBeenCalledWith(EXPECTED_CONFIG_PATH, 'utf-8');
  });
});

describe('writeConfig', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(console, 'log').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates the directory if it does not exist', () => {
    fs.existsSync.mockReturnValue(false);
    fs.mkdirSync.mockImplementation(() => {});
    fs.writeFileSync.mockImplementation(() => {});

    writeConfig(FAKE_PROJECT, { host: 'http://localhost:8080' });

    expect(fs.mkdirSync).toHaveBeenCalledWith(
      path.dirname(EXPECTED_CONFIG_PATH),
      { recursive: true }
    );
  });

  it('does not create directory if it already exists', () => {
    fs.existsSync.mockReturnValue(true);
    fs.writeFileSync.mockImplementation(() => {});

    writeConfig(FAKE_PROJECT, { host: 'http://localhost:8080' });

    expect(fs.mkdirSync).not.toHaveBeenCalled();
  });

  it('writes correctly serialized JSON with trailing newline', () => {
    fs.existsSync.mockReturnValue(true);
    fs.writeFileSync.mockImplementation(() => {});

    const config = { host: 'http://localhost:8080', tools: [] };
    writeConfig(FAKE_PROJECT, config);

    const [writePath, content, encoding] = fs.writeFileSync.mock.calls[0];
    expect(writePath).toBe(EXPECTED_CONFIG_PATH);
    expect(content).toBe(JSON.stringify(config, null, 2) + '\n');
    expect(encoding).toBe('utf-8');
  });

  it('writes config with all default fields preserved', () => {
    fs.existsSync.mockReturnValue(true);
    fs.writeFileSync.mockImplementation(() => {});

    writeConfig(FAKE_PROJECT, DEFAULT_CONFIG);

    const [, content] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(content);
    expect(parsed).toEqual(DEFAULT_CONFIG);
  });

  it('logs the saved config path', () => {
    fs.existsSync.mockReturnValue(true);
    fs.writeFileSync.mockImplementation(() => {});
    const logSpy = vi.spyOn(console, 'log');

    writeConfig(FAKE_PROJECT, {});

    expect(logSpy).toHaveBeenCalledWith(expect.stringContaining('Config saved'));
  });
});
