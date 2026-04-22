import * as path from 'path';
import * as fs from 'fs';
import { addPluginToManifest, resolveLatestVersion } from '../utils/manifest.js';
import { silentLogger } from './logger.js';
import { emitProgress } from './progress.js';
import type { InstallPluginOptions, InstallResult } from './types.js';

/**
 * Install the Unity-MCP plugin into a Unity project. Library-safe:
 * never calls `process.exit`, never prints to stdout / stderr, never
 * throws past the public boundary — errors are returned in
 * `{ success: false, error }`.
 */
export async function installPlugin(opts: InstallPluginOptions): Promise<InstallResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];

  try {
    if (!opts || typeof opts.unityProjectPath !== 'string' || opts.unityProjectPath.length === 0) {
      return {
        success: false,
        warnings,
        nextSteps,
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
        nextSteps,
        error: new Error(`Not a valid Unity project (missing Packages/manifest.json): ${projectPath}`),
      };
    }

    emitProgress(opts.onProgress, { phase: 'start', message: `Installing Unity-MCP plugin into ${projectPath}` });

    let version = opts.version;
    const isExplicitVersion = !!version;
    if (!version) {
      version = await resolveLatestVersion(silentLogger);
      emitProgress(opts.onProgress, {
        phase: 'dependencies-resolved',
        message: `Resolved latest plugin version: ${version}`,
        version,
      });
    }

    const result = addPluginToManifest(projectPath, version, isExplicitVersion, silentLogger);

    if (result.resolvedVersion !== version && !isExplicitVersion) {
      warnings.push(
        `Plugin already at version ${result.resolvedVersion} (>= ${version}). ` +
        'Skipping version update. Pass an explicit `version` to force a specific value.',
      );
    }

    emitProgress(opts.onProgress, {
      phase: 'manifest-patched',
      message: result.modified
        ? `Updated ${result.manifestPath}`
        : 'manifest.json is already up to date.',
      manifestPath: result.manifestPath,
    });

    nextSteps.push('Open the Unity project in the Editor to complete installation.');

    emitProgress(opts.onProgress, { phase: 'done', message: 'Install complete.' });

    return {
      success: true,
      installedVersion: result.resolvedVersion,
      manifestPath: result.manifestPath,
      warnings,
      nextSteps,
    };
  } catch (err: unknown) {
    return {
      success: false,
      warnings,
      nextSteps,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}
