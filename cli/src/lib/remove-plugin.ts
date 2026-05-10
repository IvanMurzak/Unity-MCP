import { removePluginFromManifest } from '../utils/manifest.js';
import { silentLogger } from './logger.js';
import { emitProgress } from './progress.js';
import { requireUnityProject } from './validation.js';
import type { RemovePluginOptions, RemoveResult } from './types.js';

/**
 * Remove the Unity-MCP plugin from a Unity project. Library-safe:
 * never calls `process.exit`, never prints to stdout / stderr, never
 * throws past the public boundary.
 *
 * The returned `RemoveResult` is a discriminated union — narrow with
 * `result.kind === 'success'` to access `removed` / `manifestPath`, or
 * `result.kind === 'failure'` to access `error`.
 */
export async function removePlugin(opts: RemovePluginOptions): Promise<RemoveResult> {
  const warnings: string[] = [];

  try {
    const validated = requireUnityProject(opts?.unityProjectPath);
    if (!validated.ok) {
      return {
        kind: 'failure',
        success: false,
        manifestPath: validated.manifestPath,
        warnings,
        error: validated.error,
      };
    }
    const { projectPath } = validated;

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
      kind: 'success',
      success: true,
      removed: result.removed,
      manifestPath: result.manifestPath,
      warnings,
    };
  } catch (err: unknown) {
    return {
      kind: 'failure',
      success: false,
      warnings,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}
