import * as fs from 'fs';
import {
  EXTENSIONS_CATALOG,
  findExtension,
  hasVersion,
  type ExtensionDescriptor,
} from '../utils/extensions-catalog.js';
import { addPackageToManifest, resolveLatestPackageVersion } from '../utils/manifest.js';
import { silentLogger } from './logger.js';
import { emitProgress } from './progress.js';
import { requireUnityProject } from './validation.js';
import type { InstallExtensionOptions, InstallExtensionResult } from './types.js';

/**
 * Install (or update) a Unity-MCP extension into a consumer Unity project, as a
 * REAL installer that is behaviourally identical to the in-editor window's
 * `ExtensionPanel`:
 *
 *  1. Resolve `extensionId` to a descriptor in the SHARED catalogue
 *     (`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json`,
 *     mirrored here by `EXTENSIONS_CATALOG`).
 *  2. Validate that the target path hosts a Unity project (`Packages/manifest.json`).
 *  3. Resolve the version to install: the explicit `version` override, else the
 *     catalogue pin, else OpenUPM's `dist-tags.latest` fetched live. Every shipped
 *     catalogue entry is unpinned, so the live fetch is the normal path.
 *  4. Read-modify-write `Packages/manifest.json`: ensure the OpenUPM scoped registry
 *     and its required scopes, then add or version-bump the package dependency.
 *  5. Tell the caller Unity must resolve packages (the CLI cannot call
 *     `UnityEditor.PackageManager.Client.Resolve()` — only the editor can).
 *
 * Library-safe: no stdout noise, no `process.exit`, no throws past the public
 * boundary; returns a `{ kind: 'success' | 'failure' }` union. Idempotent: a re-run
 * that finds the dependency at an equal-or-newer version reports
 * `already-up-to-date` and makes no write. Mirrors `installPlugin`'s shape exactly
 * so the app can adopt it the same way.
 */
export async function installExtension(
  opts: InstallExtensionOptions,
): Promise<InstallExtensionResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];
  const extensionId = opts?.extensionId ?? '';

  try {
    const catalog = opts.catalog ?? EXTENSIONS_CATALOG;
    const resolved = findExtension(extensionId, catalog);
    if (resolved === null) {
      return {
        kind: 'failure',
        success: false,
        extensionId,
        warnings,
        nextSteps,
        error: new Error(unknownExtensionMessage(extensionId, catalog)),
      };
    }

    const validated = requireUnityProject(opts?.unityProjectPath);
    if (!validated.ok) {
      return {
        kind: 'failure',
        success: false,
        extensionId,
        packageId: resolved.packageId,
        manifestPath: validated.manifestPath,
        warnings,
        nextSteps,
        error: validated.error,
      };
    }
    const { projectPath } = validated;

    emitProgress(opts.onProgress, {
      phase: 'start',
      message: `Installing extension ${resolved.packageId} into ${projectPath}`,
    });

    // Apply the optional `version` override (drives both the pin written and, via
    // `force`, whether a downgrade is permitted).
    const descriptor = applyVersionOverride(resolved, opts.version, warnings);
    const isExplicitVersion = (opts.version ?? '').trim() !== '';

    let version: string;
    if (hasVersion(descriptor)) {
      // Non-null only for an explicit override or a (currently unused) catalogue pin.
      version = descriptor.version as string;
    } else {
      version = await resolveLatestPackageVersion(
        descriptor.packageId,
        silentLogger,
        '--extension-version',
      );
      emitProgress(opts.onProgress, {
        phase: 'dependencies-resolved',
        message: `Resolved latest ${descriptor.packageId} version: ${version}`,
        version,
      });
    }

    const before = readInstalledVersion(validated.manifestPath, descriptor.packageId);

    const result = addPackageToManifest(
      projectPath,
      descriptor.packageId,
      version,
      isExplicitVersion,
      silentLogger,
    );

    // `addPackageToManifest` refuses to downgrade unless `force`; surface that as a
    // warning rather than silently reporting the requested version.
    if (result.resolvedVersion !== version && !isExplicitVersion) {
      warnings.push(
        `${descriptor.packageId} is already at version ${result.resolvedVersion} (>= ${version}). ` +
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

    // Derive the outcome from what was actually on disk before the write, NOT from
    // `modified` alone: `modified` is also true when only the scoped registry had to
    // be added, which is not an "added extension".
    const outcome =
      before === null
        ? 'added'
        : result.resolvedVersion === before
          ? 'already-up-to-date'
          : 'updated';

    const message =
      outcome === 'added'
        ? `Added ${descriptor.packageId} ${result.resolvedVersion}. Unity will resolve the package on next focus.`
        : outcome === 'updated'
          ? `Updated ${descriptor.packageId} from ${before} to ${result.resolvedVersion}.`
          : `${descriptor.name} is already installed and up to date (${result.resolvedVersion}).`;

    if (outcome !== 'already-up-to-date') {
      nextSteps.push(
        'Open (or focus) the Unity Editor so the Package Manager resolves the new dependency.',
      );
    }

    emitProgress(opts.onProgress, { phase: 'done', message: 'Extension install complete.' });

    return {
      kind: 'success',
      success: true,
      outcome,
      changed: outcome !== 'already-up-to-date',
      extensionId,
      packageId: descriptor.packageId,
      fromVersion: before,
      toVersion: result.resolvedVersion,
      manifestPath: result.manifestPath,
      message,
      warnings,
      nextSteps,
    };
  } catch (err: unknown) {
    return {
      kind: 'failure',
      success: false,
      extensionId,
      warnings,
      nextSteps,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}

/**
 * Read the version currently pinned for `packageId` in the project manifest, or
 * `null` when the dependency is absent. Used to classify the outcome as
 * added / updated / already-up-to-date, which `addPackageToManifest`'s
 * `{ modified }` flag cannot express on its own.
 *
 * Deliberately forgiving: any read/parse problem yields `null` (treated as
 * "not installed"). The subsequent `addPackageToManifest` call performs the
 * authoritative read and throws a precise error if the manifest is unusable, so
 * swallowing here cannot mask a real failure — it only avoids duplicating the
 * error path.
 */
function readInstalledVersion(manifestPath: string, packageId: string): string | null {
  try {
    const raw = fs.readFileSync(manifestPath, 'utf-8');
    const parsed = JSON.parse(raw) as { dependencies?: Record<string, string> };
    const current = parsed?.dependencies?.[packageId];
    return typeof current === 'string' && current.trim() !== '' ? current : null;
  } catch {
    return null;
  }
}

/** Return a descriptor with the `version` override applied (empty/whitespace override is ignored). */
function applyVersionOverride(
  descriptor: ExtensionDescriptor,
  version: string | undefined,
  warnings: string[],
): ExtensionDescriptor {
  const v = (version ?? '').trim();
  if (v === '') return descriptor;
  if (descriptor.version !== null && descriptor.version !== v) {
    warnings.push(
      `version ${v} overrides the catalogue pin ${descriptor.packageId}@${descriptor.version} for this install.`,
    );
  }
  return { ...descriptor, version: v };
}

/** A clear "unknown extension" error that lists what IS installable (or says the catalogue is empty). */
function unknownExtensionMessage(
  id: string,
  catalog: readonly ExtensionDescriptor[],
): string {
  if (catalog.length === 0) {
    return (
      `Unknown extension "${id}". The Unity-MCP extension catalogue is currently empty, ` +
      'so there is nothing to install.'
    );
  }
  const available = catalog.map((d) => d.packageId).join(', ');
  return `Unknown extension "${id}". Available extensions: ${available}.`;
}
