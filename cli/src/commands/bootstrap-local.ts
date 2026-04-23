// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath } from '../utils/connection.js';
import {
  readConfig,
  writeConfig,
  isCloudMode,
  type UnityConnectionConfig,
} from '../utils/config.js';

interface BootstrapLocalOptions {
  path?: string;
  url?: string;
  token?: string;
  dryRun?: boolean;
}

/**
 * Produce a new config object with local-mode fields pinned to `url` / `token`.
 * Preserves all other keys from `current` (immutable update — does not mutate `current`).
 *
 * The plugin-side ConnectionMode enum only accepts "Custom" (local) or "Cloud".
 * We intentionally normalize any integer (0 → Custom, 1 → Cloud) representation
 * to the canonical string "Custom" so the on-disk file is explicit.
 */
export function pinLocalModeConfig(
  current: Readonly<UnityConnectionConfig>,
  url: string,
  token: string,
): UnityConnectionConfig {
  return {
    ...current,
    connectionMode: 'Custom',
    host: url,
    token,
  };
}

/**
 * Returns true iff `current` already reflects local mode with matching url + token.
 * Used to make bootstrap-local a no-op when nothing would change on disk.
 */
export function isAlreadyPinned(
  current: Readonly<UnityConnectionConfig>,
  url: string,
  token: string,
): boolean {
  if (isCloudMode(current)) return false;
  // Accept both "Custom" (string) and 0 (legacy int) as already-local.
  const mode = current.connectionMode;
  const isLocalMode = mode === 'Custom' || mode === 0;
  if (!isLocalMode) return false;
  return current.host === url && current.token === token;
}

export const bootstrapLocalCommand = new Command('bootstrap-local')
  .description(
    'Pin a Unity project\'s MCP config to local mode with the given URL/token. Idempotent.',
  )
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--path <path>', 'Unity project path (defaults to cwd)')
  .requiredOption('--url <url>', 'Local MCP server URL to pin (e.g. http://localhost:5140/)')
  .requiredOption('--token <token>', 'Local MCP bearer token to pin')
  .option('--dry-run', 'Report what would change without writing to disk')
  .action(async (positionalPath: string | undefined, options: BootstrapLocalOptions) => {
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);

    // commander's requiredOption guarantees url/token are defined before the action runs.
    const url = options.url!.replace(/\/$/, '');
    const token = options.token!;

    verbose(`Project path: ${projectPath}`);
    verbose(`Target URL: ${url}`);
    verbose(`Target token: ${'*'.repeat(Math.min(token.length, 8))}... (length ${token.length})`);

    const configPath = path.join(projectPath, 'UserSettings', 'AI-Game-Developer-Config.json');
    const existedBefore = fs.existsSync(configPath);

    // Use readConfig (not getOrCreateConfig) so --dry-run truly has no side effects.
    // When the file doesn't exist we pin onto an empty base; this mirrors a fresh
    // worktree where the plugin has not yet written its default Cloud config.
    const current: UnityConnectionConfig = readConfig(projectPath) ?? {};

    if (isAlreadyPinned(current, url, token)) {
      ui.success(
        `MCP config already pinned to local mode (${url}). No changes written.`,
      );
      return;
    }

    const next = pinLocalModeConfig(current, url, token);

    if (options.dryRun) {
      ui.heading('bootstrap-local (dry run)');
      ui.label('Config file', configPath);
      ui.label('connectionMode', `${String(current.connectionMode ?? 'unset')} → Custom`);
      ui.label('host', `${String(current.host ?? 'unset')} → ${url}`);
      ui.label('token', `${current.token ? '<previous>' : '<unset>'} → <supplied>`);
      ui.info('No changes written (--dry-run).');
      return;
    }

    writeConfig(projectPath, next);

    ui.heading('bootstrap-local');
    ui.label('Config file', configPath);
    ui.label('connectionMode', 'Custom');
    ui.label('host', url);
    ui.label('token', '<supplied>');
    ui.success(
      existedBefore
        ? 'Config updated to pin local mode.'
        : 'Config created and pinned to local mode.',
    );
  });
