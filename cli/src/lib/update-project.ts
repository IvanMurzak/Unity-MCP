import * as fs from 'fs';
import * as path from 'path';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import {
  isNonSemverSpec,
  PLUGIN_PACKAGE_ID,
  pluginRelease,
  resolveExtensionVersionForPlugin,
  resolveLatestPackageVersion,
  shouldUpdateVersion,
} from '../utils/manifest.js';
import { installExtension } from './install-extension.js';
import { installPlugin } from './install-plugin.js';
import { silentLogger } from './logger.js';
import { requireUnityProject } from './validation.js';
import type { ProgressCallback } from './types.js';

function asError(err: unknown): Error {
  return err instanceof Error ? err : new Error(String(err));
}

/**
 * `outdated`: an update applies. `current`: nothing to change. `local`: a `file:`/git/URL install, never replaced.
 * `unknown`: the lookup failed. `unavailable`: an extension with no version published for the project's plugin
 * release (yet): it stays as it is and Unity skips it.
 */
export type ProjectUpdateStatus = 'outdated' | 'current' | 'local' | 'unknown' | 'unavailable';

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
  /** Leave the plugin as it is: the installed extensions move to their versions for the plugin the project has. */
  noPlugin?: boolean;
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
 * Bring a Unity project's MCP plugin to its latest OpenUPM version and its installed extensions to the versions
 * built for that plugin. The one function behind `unity-mcp-cli update`. An extension works with exactly one
 * plugin release (Unity skips any other without an error), so the plugin is the anchor: each extension moves to
 * the version published for the plugin the project ends up with, also when that is an older one. Only what the
 * manifest already lists is touched, `file:`/git installs are left alone and the plugin is never downgraded.
 * One item failing is a warning and does not stop the others. Library-safe: no stdout, no `process.exit`, no
 * throws past the boundary.
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

    type Target = Pick<ProjectUpdateItem, 'kind' | 'name' | 'packageId'>;
    const installed = (target: Target): boolean => dependencies[target.packageId] !== undefined;

    /** What applies to one installed package; `resolve` names its target version (null: none is published). */
    const lookUp = async (target: Target, resolve: () => Promise<string | null>, anyDirection: boolean): Promise<ProjectUpdateItem> => {
      const from = dependencies[target.packageId]!;
      if (isNonSemverSpec(from)) return { ...target, from, to: null, status: 'local' };
      try {
        const to = await resolve();
        if (to === null) return { ...target, from, to, status: 'unavailable' };
        const applies = anyDirection ? to !== from : shouldUpdateVersion(from, to);
        return { ...target, from, to, status: applies ? 'outdated' : 'current' };
      } catch (err: unknown) {
        warnings.push(`Could not look up the latest ${target.name}: ${asError(err).message}`);
        return { ...target, from, to: null, status: 'unknown' };
      }
    };

    const updated: ProjectUpdateItem[] = [];
    /** Write one outdated item; false (and a warning) when that failed. Never called with `dryRun`. */
    const apply = async (item: ProjectUpdateItem): Promise<boolean> => {
      const result = item.kind === 'plugin'
        ? await installPlugin({ unityProjectPath: projectPath, onProgress: opts.onProgress })
        : await installExtension({ unityProjectPath: projectPath, extensionId: item.packageId, version: item.to!, onProgress: opts.onProgress });
      if (result.kind === 'failure') {
        warnings.push(`Could not update ${item.name}: ${result.error.message}`);
        return false;
      }
      warnings.push(...result.warnings);
      updated.push(item);
      return true;
    };

    // The plugin first: the extensions follow the plugin release the project ends up with.
    const items: ProjectUpdateItem[] = [];
    const pluginTarget: Target = { kind: 'plugin', name: 'MCP plugin', packageId: PLUGIN_PACKAGE_ID };
    // Null: the manifest names no plugin release (a local source, or no plugin of its own).
    let plugin = pluginRelease(dependencies[PLUGIN_PACKAGE_ID] ?? '');
    if (installed(pluginTarget) && !opts.noPlugin) {
      const item = await lookUp(pluginTarget, () => resolveLatestPackageVersion(PLUGIN_PACKAGE_ID, silentLogger), false);
      items.push(item);
      if (item.status === 'outdated' && (opts.dryRun || await apply(item))) plugin = pluginRelease(item.to!) ?? plugin;
    }

    const extensions = opts.noExtensions ? [] : EXTENSIONS_CATALOG
      .map((extension): Target => ({ kind: 'extension', name: extension.name, packageId: extension.packageId }))
      .filter(installed);
    const extensionItems = await Promise.all(extensions.map((target) => plugin === null
      ? lookUp(target, () => resolveLatestPackageVersion(target.packageId, silentLogger), false)
      : lookUp(target, () => resolveExtensionVersionForPlugin(target.packageId, plugin!), true)));
    items.push(...extensionItems);

    for (const item of extensionItems) {
      if (item.status === 'unavailable') {
        warnings.push(
          `${item.name} ${item.from}: no version for plugin ${plugin} is published yet. It stays as it is and Unity ` +
          'skips it (an extension works with exactly one plugin release). Run the update again later.',
        );
      } else if (item.status === 'outdated') {
        if (plugin !== null && shouldUpdateVersion(item.to!, item.from)) {
          warnings.push(
            `${item.name} ${item.from} is built for another plugin release, so Unity skips it under plugin ${plugin}: ` +
            `${item.to} is the version built for this plugin.`,
          );
        }
        if (!opts.dryRun) await apply(item);
      }
    }
    if (updated.length > 0) {
      nextSteps.push('Open (or focus) the Unity Editor so the Package Manager resolves the new versions.');
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
