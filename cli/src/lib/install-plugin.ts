import * as fs from 'fs';
import { addPluginToManifest, closePluginGate, PLUGIN_PACKAGE_ID, resolveLatestVersion } from '../utils/manifest.js';
import { silentLogger } from './logger.js';
import { emitProgress } from './progress.js';
import { requireUnityProject } from './validation.js';
import type { InstallPluginOptions, InstallResult } from './types.js';

/**
 * Install the Unity-MCP plugin into a Unity project. Library-safe:
 * never calls `process.exit`, never prints to stdout / stderr, never
 * throws past the public boundary — errors are returned in
 * `{ kind: 'failure', success: false, error }`.
 *
 * The returned `InstallResult` is a discriminated union — narrow with
 * `result.kind === 'success'` to access `installedVersion` /
 * `manifestPath`, or `result.kind === 'failure'` to access `error`.
 */
export async function installPlugin(opts: InstallPluginOptions): Promise<InstallResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];

  try {
    const validated = requireUnityProject(opts?.unityProjectPath);
    if (!validated.ok) {
      return {
        kind: 'failure',
        success: false,
        manifestPath: validated.manifestPath,
        warnings,
        nextSteps,
        error: validated.error,
      };
    }
    const { projectPath } = validated;

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

    const before = installedPluginSpec(validated.manifestPath);
    const result = addPluginToManifest(projectPath, version, isExplicitVersion, silentLogger);
    // Another plugin version: nothing built on the plugin may compile before the new one is ready.
    if (before !== result.resolvedVersion) closePluginGate(projectPath);

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
      kind: 'success',
      success: true,
      installedVersion: result.resolvedVersion,
      manifestPath: result.manifestPath,
      warnings,
      nextSteps,
    };
  } catch (err: unknown) {
    return {
      kind: 'failure',
      success: false,
      warnings,
      nextSteps,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}

/** The plugin entry of the manifest as it is before the install; null when absent or unreadable. */
function installedPluginSpec(manifestPath: string): string | null {
  try {
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8')) as { dependencies?: Record<string, string> };
    return manifest.dependencies?.[PLUGIN_PACKAGE_ID] ?? null;
  } catch {
    return null;
  }
}
