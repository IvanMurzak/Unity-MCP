import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { Command } from 'commander';

// Hoist mock — applies to all imports in this file
vi.mock('../../src/utils/config.js', () => ({
  readConfig: vi.fn(),
  writeConfig: vi.fn(),
}));

import { readConfig, writeConfig } from '../../src/utils/config.js';
import {
  buildFeatureList,
  parseCommaSeparated,
  registerSetFeaturesCommands,
} from '../../src/commands/set-features.js';

// ---------------------------------------------------------------------------
// parseCommaSeparated
// ---------------------------------------------------------------------------
describe('parseCommaSeparated', () => {
  it('splits a comma-separated string into an array', () => {
    expect(parseCommaSeparated('a,b,c')).toEqual(['a', 'b', 'c']);
  });

  it('trims whitespace around each entry', () => {
    expect(parseCommaSeparated(' a , b , c ')).toEqual(['a', 'b', 'c']);
  });

  it('filters out empty entries', () => {
    expect(parseCommaSeparated('a,,b')).toEqual(['a', 'b']);
  });

  it('handles a single value without commas', () => {
    expect(parseCommaSeparated('tool-name')).toEqual(['tool-name']);
  });

  it('returns empty array for an empty string', () => {
    expect(parseCommaSeparated('')).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// buildFeatureList
// ---------------------------------------------------------------------------
describe('buildFeatureList', () => {
  describe('with enabledNames provided', () => {
    it('replaces the list with exactly the enabled names', () => {
      const existing = [
        { name: 'a', enabled: true },
        { name: 'b', enabled: true },
      ];
      const result = buildFeatureList(existing, ['c', 'd'], []);

      expect(result).toHaveLength(2);
      expect(result.find(f => f.name === 'c')?.enabled).toBe(true);
      expect(result.find(f => f.name === 'd')?.enabled).toBe(true);
    });

    it('disables names that appear in both enabled and disabled lists', () => {
      const result = buildFeatureList([], ['a', 'b'], ['b']);

      expect(result.find(f => f.name === 'a')?.enabled).toBe(true);
      expect(result.find(f => f.name === 'b')?.enabled).toBe(false);
    });

    it('includes disabled-only names in the output', () => {
      const result = buildFeatureList([], ['a'], ['b']);

      const names = result.map(f => f.name);
      expect(names).toContain('a');
      expect(names).toContain('b');
    });

    it('does not carry over old existing entries', () => {
      const existing = [{ name: 'old', enabled: true }];
      const result = buildFeatureList(existing, ['new'], []);

      expect(result.find(f => f.name === 'old')).toBeUndefined();
    });
  });

  describe('with only disabledNames provided (patch mode)', () => {
    it('marks specified names as disabled, keeps others intact', () => {
      const existing = [
        { name: 'a', enabled: true },
        { name: 'b', enabled: true },
        { name: 'c', enabled: true },
      ];
      const result = buildFeatureList(existing, [], ['b']);

      expect(result.find(f => f.name === 'a')?.enabled).toBe(true);
      expect(result.find(f => f.name === 'b')?.enabled).toBe(false);
      expect(result.find(f => f.name === 'c')?.enabled).toBe(true);
    });

    it('adds disabled entries that were not previously in the list', () => {
      const existing = [{ name: 'a', enabled: true }];
      const result = buildFeatureList(existing, [], ['new-tool']);

      const newEntry = result.find(f => f.name === 'new-tool');
      expect(newEntry).toBeDefined();
      expect(newEntry?.enabled).toBe(false);
    });

    it('preserves already-disabled entries', () => {
      const existing = [{ name: 'a', enabled: false }];
      const result = buildFeatureList(existing, [], ['b']);

      expect(result.find(f => f.name === 'a')?.enabled).toBe(false);
    });

    it('does not duplicate existing entries that are disabled', () => {
      const existing = [{ name: 'a', enabled: true }];
      const result = buildFeatureList(existing, [], ['a']);

      const entries = result.filter(f => f.name === 'a');
      expect(entries).toHaveLength(1);
      expect(entries[0].enabled).toBe(false);
    });
  });

  describe('edge cases', () => {
    it('returns existing list when both enabledNames and disabledNames are empty', () => {
      const existing = [{ name: 'a', enabled: true }];
      const result = buildFeatureList(existing, [], []);

      expect(result).toEqual(existing);
    });

    it('handles empty existing list with only disabledNames', () => {
      const result = buildFeatureList([], [], ['tool-x']);

      expect(result).toEqual([{ name: 'tool-x', enabled: false }]);
    });

    it('handles empty existing list with only enabledNames', () => {
      const result = buildFeatureList([], ['tool-y'], []);

      expect(result).toEqual([{ name: 'tool-y', enabled: true }]);
    });
  });
});

// ---------------------------------------------------------------------------
// registerSetFeaturesCommands — integration smoke tests
// ---------------------------------------------------------------------------
describe('registerSetFeaturesCommands (CLI integration)', () => {
  let program;
  let logSpy;
  let errSpy;
  let exitSpy;

  beforeEach(() => {
    vi.resetAllMocks();
    readConfig.mockReturnValue({ tools: [], prompts: [], resources: [] });
    writeConfig.mockImplementation(() => {});

    program = new Command();
    program.exitOverride();
    registerSetFeaturesCommands(program);

    logSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    errSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    // No throw — just capture the call so the action can finish
    exitSpy = vi.spyOn(process, 'exit').mockImplementation((code) => { /* captured */ });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('set-tools clears the list when --all is specified', () => {
    readConfig.mockReturnValue({ tools: [{ name: 'old', enabled: true }], prompts: [], resources: [] });

    program.parse(['node', 'gamedev', 'set-tools', '/project', '--all']);

    const [, writtenConfig] = writeConfig.mock.calls[0];
    expect(writtenConfig.tools).toEqual([]);
    expect(logSpy).toHaveBeenCalledWith(expect.stringContaining('enabled'));
  });

  it('set-prompts sets enabled prompts from positional args', () => {
    program.parse(['node', 'gamedev', 'set-prompts', '/project', 'prompt-a', 'prompt-b']);

    const [, writtenConfig] = writeConfig.mock.calls[0];
    expect(writtenConfig.prompts.find(p => p.name === 'prompt-a')?.enabled).toBe(true);
    expect(writtenConfig.prompts.find(p => p.name === 'prompt-b')?.enabled).toBe(true);
  });

  it('set-resources disables specified resource via --disable', () => {
    readConfig.mockReturnValue({ tools: [], prompts: [], resources: [{ name: 'res-x', enabled: true }] });

    program.parse(['node', 'gamedev', 'set-resources', '/project', '--disable', 'res-x']);

    const [, writtenConfig] = writeConfig.mock.calls[0];
    expect(writtenConfig.resources.find(r => r.name === 'res-x')?.enabled).toBe(false);
  });

  it('shows error and calls process.exit(1) when no names or flags provided', () => {
    program.parse(['node', 'gamedev', 'set-tools', '/project']);

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('No tools specified'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('does not modify tool list when an error occurs (no names or flags)', () => {
    readConfig.mockReturnValue({ tools: [{ name: 'existing-tool', enabled: true }], prompts: [], resources: [] });

    program.parse(['node', 'gamedev', 'set-tools', '/project']);

    // process.exit was called — but even if mock doesn't terminate,
    // the config object should NOT have its tools cleared
    const callArgs = writeConfig.mock.calls[0];
    if (callArgs) {
      // If writeConfig was called despite the error (mock doesn't exit),
      // the existing tools should be unchanged since the else branch doesn't modify config
      const writtenConfig = callArgs[1];
      expect(writtenConfig.tools).toEqual([{ name: 'existing-tool', enabled: true }]);
    }
  });
});
