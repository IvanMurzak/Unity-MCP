import * as path from 'path';
import * as fs from 'fs';
import { removePluginFromManifest } from '../utils/manifest.js';
import { silentLogger } from './logger.js';
import { emitProgress } from './progress.js';
import type { RemovePluginOptions, RemoveResult } from './types.js';

/**
 * Remove the Unity-MCP plugin from a Unity project. Library-safe:
 * never calls `process.exit`, never prints to stdout / stderr, never
 * throws past the public boundary.
 */
export async function removePlugin(opts: RemovePluginOptions): Promise<RemoveResult> {
  const warnings: string[] = [];

  try {
    if (!opts || typeof opts.unityProjectPath !== 'string' || opts.unityProjectPath.length === 0) {
      return {
        success: false,
        warnings,
        error: new Error('unityProjectPath is required and must be a non-empty string.'),
      };
    }

    const projectPath = path.resolve(opts.unityProjectPath);
    const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');

    if (!fs.existsSync(manifestPath)) {
      return {
        success: false,
        manifestPath,
        warnings,
        error: new Error(`Not a valid Unity project (missing Packages/manifest.json): ${projectPath}`),
      };
    }

    emitProgress(opts.onProgress, { phase: 'start', message: `Removing Unity-MCP plugin from ${projectPath}` });

    const result = removePluginFromManifest(projectPath, silentLogger);

    if (!result.removed) {
      warnings.push('Unity-MCP plugin was not installed. Nothing was removed.');
    }

    emitProgress(opts.onProgress, {
      phase: 'manifest-patched',
      message: result.removed
        ? `Removed plugin from ${result.manifestPath}`
        : 'manifest.json left untouched (plugin not installed).',
      manifestPath: result.manifestPath,
    });

    emitProgress(opts.onProgress, { phase: 'done', message: 'Remove complete.' });

    return {
      success: true,
      removed: result.removed,
      manifestPath: result.manifestPath,
      warnings,
    };
  } catch (err: unknown) {
    return {
      success: false,
      warnings,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}
