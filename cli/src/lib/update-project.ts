import * as fs from 'fs';
import * as path from 'path';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import { isNonSemverSpec, resolveLatestPackageVersion, shouldUpdateVersion } from '../utils/manifest.js';
import { installExtension } from './install-extension.js';
import { installPlugin } from './install-plugin.js';
import { silentLogger } from './logger.js';
import { requireUnityProject } from './validation.js';
import type { ProgressCallback } from './types.js';

const PLUGIN_PACKAGE_ID = 'com.ivanmurzak.unity.mcp';

function asError(err: unknown): Error {
  return err instanceof Error ? err : new Error(String(err));
}

/** `outdated`: an update applies. `current`: nothing newer. `local`: a `file:`/git/URL install, never replaced. `unknown`: the lookup failed. */
export type ProjectUpdateStatus = 'outdated' | 'current' | 'local' | 'unknown';

export interface ProjectUpdateItem {
  kind: 'plugin' | 'extension';
  name: string;
  packageId: string;
  from: string;
  to: string | null;
  status: ProjectUpdateStatus;
}

export interface UpdateProjectOptions {
  unityProjectPath: string;
  /** Leave the installed extensions as they are. */
  noExtensions?: boolean;
  /** Only report what would change; write nothing. */
  dryRun?: boolean;
  onProgress?: ProgressCallback;
}

export interface UpdateProjectSuccess {
  kind: 'success';
  success: true;
  /** Everything checked, as found before any write. */
  items: ProjectUpdateItem[];
  /** The items this call moved to a new version (always empty with `dryRun`). */
  updated: ProjectUpdateItem[];
  warnings: string[];
  nextSteps: string[];
}

export interface UpdateProjectFailure {
  kind: 'failure';
  success: false;
  warnings: string[];
  error: Error;
}

export type UpdateProjectResult = UpdateProjectSuccess | UpdateProjectFailure;

/**
 * Bring a Unity project's MCP plugin and its installed extensions to their latest OpenUPM versions.
 * The one function behind `unity-mcp-cli update`. Only what the manifest already lists is touched,
 * `file:`/git installs are left alone and nothing is downgraded. One item failing is a warning and
 * does not stop the others. Library-safe: no stdout, no `process.exit`, no throws past the boundary.
 */
export async function updateProject(opts: UpdateProjectOptions): Promise<UpdateProjectResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];
  try {
    const validated = requireUnityProject(opts?.unityProjectPath);
    if (!validated.ok) return { kind: 'failure', success: false, warnings, error: validated.error };
    const { projectPath, manifestPath } = validated;

    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8')) as { dependencies?: Record<string, string> };
    const dependencies = manifest.dependencies ?? {};

    const targets: Array<Pick<ProjectUpdateItem, 'kind' | 'name' | 'packageId'>> = [
      { kind: 'plugin' as const, name: 'MCP plugin', packageId: PLUGIN_PACKAGE_ID },
      ...(opts.noExtensions ? [] : EXTENSIONS_CATALOG.map((extension) => ({
        kind: 'extension' as const,
        name: extension.name,
        packageId: extension.packageId,
      }))),
    ].filter((target) => dependencies[target.packageId] !== undefined);

    const items = await Promise.all(targets.map(async (target): Promise<ProjectUpdateItem> => {
      const from = dependencies[target.packageId]!;
      if (isNonSemverSpec(from)) return { ...target, from, to: null, status: 'local' };
      try {
        const to = await resolveLatestPackageVersion(target.packageId, silentLogger);
        return { ...target, from, to, status: shouldUpdateVersion(from, to) ? 'outdated' : 'current' };
      } catch (err: unknown) {
        warnings.push(`Could not look up the latest ${target.name}: ${asError(err).message}`);
        return { ...target, from, to: null, status: 'unknown' };
      }
    }));

    const updated: ProjectUpdateItem[] = [];
    if (!opts.dryRun) {
      for (const item of items.filter((candidate) => candidate.status === 'outdated')) {
        const result = item.kind === 'plugin'
          ? await installPlugin({ unityProjectPath: projectPath, onProgress: opts.onProgress })
          : await installExtension({ unityProjectPath: projectPath, extensionId: item.packageId, onProgress: opts.onProgress });
        if (result.kind === 'failure') {
          warnings.push(`Could not update ${item.name}: ${result.error.message}`);
          continue;
        }
        warnings.push(...result.warnings);
        updated.push(item);
      }
      if (updated.length > 0) {
        nextSteps.push('Open (or focus) the Unity Editor so the Package Manager resolves the new versions.');
      }
    }

    return { kind: 'success', success: true, items, updated, warnings, nextSteps };
  } catch (err: unknown) {
    return { kind: 'failure', success: false, warnings, error: asError(err) };
  }
}

/**
 * The nearest folder at or above `start` that is a Unity project, or null. `update` run from
 * `MyGame/Assets/Scripts` means MyGame.
 */
export function findUnityProjectRoot(start: string): string | null {
  for (let dir = path.resolve(start); ; dir = path.dirname(dir)) {
    if (fs.existsSync(path.join(dir, 'Packages', 'manifest.json'))) return dir;
    if (dir === path.dirname(dir)) return null;
  }
}
