import * as fs from 'fs';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
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
    // Unity installs whatever an installed package depends on: the plugin stays while an extension names it.
    const dependents = installedExtensions(result.manifestPath);
    if (dependents.length > 0) {
      warnings.push(
        `Unity still installs the plugin: ${dependents.join(', ')} ${dependents.length === 1 ? 'depends' : 'depend'} on it. ` +
        'Remove the extensions from Packages/manifest.json as well.',
      );
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

/** The names of the catalogue extensions the manifest lists; none when it cannot be read. */
function installedExtensions(manifestPath: string): string[] {
  try {
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8')) as { dependencies?: Record<string, unknown> };
    return EXTENSIONS_CATALOG.filter((extension) => manifest.dependencies?.[extension.packageId] !== undefined).map((extension) => extension.name);
  } catch {
    return [];
  }
}
