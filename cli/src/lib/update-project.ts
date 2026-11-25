import * as fs from 'fs';
import * as path from 'path';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import { isNewerVersion } from '@baizor/gamedev-cli-core';
import {
  addPackagesToManifest,
  closePluginGate,
  extensionVersionForPlugin,
  fetchPublishedExtension,
  isNonSemverSpec,
  PLUGIN_PACKAGE_ID,
  pluginRelease,
  resolveLatestPackageVersion,
  shouldUpdateVersion,
  type PublishedExtension,
} from '../utils/manifest.js';
import { emitProgress } from './progress.js';
import { silentLogger } from './logger.js';
import { resolveRunningPlugin } from './running-plugin.js';
import { requireUnityProject } from './validation.js';
import type { ProgressCallback } from './types.js';

function asError(err: unknown): Error {
  return err instanceof Error ? err : new Error(String(err));
}

/**
 * `outdated`: an update applies. `current`: nothing to change. `local`: a `file:`/git/URL install, never replaced.
 * `unknown`: the lookup failed. `unavailable`: an extension with no version published for the project's plugin
 * release: it stays as it is and does not work with that plugin. `held`: the plugin has a newer release, but an
 * installed extension has no version for it yet, so the plugin stays where it is until it has.
 */
export type ProjectUpdateStatus = 'outdated' | 'current' | 'local' | 'unknown' | 'unavailable' | 'held';

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
 * plugin release, so the plugin is the anchor: each extension moves to the version published for the plugin the
 * project runs afterwards (`runningPluginRelease`). The project is never left half-way: everything is looked
 * up before anything is written, and a plugin release whose extensions are not all published yet is held back
 * (`held`) until they are. Only what the manifest already lists is touched, `file:`/git installs are left alone
 * and the plugin is never downgraded. Lookup failures are warnings; a manifest commit failure fails the update. Library-safe:
 * no stdout, no `process.exit`, no throws past the boundary.
 */
export async function updateProject(opts: UpdateProjectOptions): Promise<UpdateProjectResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];
  try {
    const validated = requireUnityProject(opts?.unityProjectPath);
    if (!validated.ok) return { kind: 'failure', success: false, warnings, error: validated.error };
    const { projectPath, manifestPath } = validated;

    const originalManifest = fs.readFileSync(manifestPath, 'utf-8');
    const manifest = JSON.parse(originalManifest) as { dependencies?: Record<string, string> };
    const dependencies = manifest.dependencies ?? {};

    type Target = Pick<ProjectUpdateItem, 'kind' | 'name' | 'packageId'>;
    const pluginTarget: Target = { kind: 'plugin', name: 'MCP plugin', packageId: PLUGIN_PACKAGE_ID };
    const extensions = EXTENSIONS_CATALOG
      .map((extension): Target => ({ kind: 'extension', name: extension.name, packageId: extension.packageId }))
      .filter((target) => dependencies[target.packageId] !== undefined);
    const fromRegistry = (target: Target): boolean => !isNonSemverSpec(dependencies[target.packageId]!);

    /** A lookup that failed is a warning and `undefined`; its item is then `unknown`. */
    const lookUp = async <T>(target: Target, read: () => Promise<T>): Promise<T | undefined> => {
      try {
        return await read();
      } catch (err: unknown) {
        warnings.push(`Could not look up the latest ${target.name}: ${asError(err).message}`);
        return undefined;
      }
    };

    const pluginSpec = dependencies[PLUGIN_PACKAGE_ID];
    const registryExtensions = extensions.filter(fromRegistry);
    // The latest plugin matters when the manifest lists a registry plugin, and also when it lists none but an
    // installed extension makes Unity install one.
    const looksUpPlugin = !opts.noPlugin && (pluginSpec === undefined ? registryExtensions.length > 0 : fromRegistry(pluginTarget));
    const [latestPlugin, published] = await Promise.all([
      looksUpPlugin ? lookUp(pluginTarget, () => resolveLatestPackageVersion(PLUGIN_PACKAGE_ID, silentLogger)) : undefined,
      Promise.all((opts.noExtensions ? [] : registryExtensions).map(async (target) =>
        [target.packageId, await lookUp(target, () => fetchPublishedExtension(target.packageId))] as const)),
    ]);
    const lookedUp = new Map<string, PublishedExtension | undefined>(published);

    /** The version of an extension to have under plugin release `plugin` (null: none is published; undefined: unknown). */
    const versionFor = (target: Target, plugin: string | null): string | null | undefined => {
      const extension = lookedUp.get(target.packageId);
      if (extension === undefined) return undefined;
      // No plugin release is known (a local plugin, or nothing names one): the latest one.
      return plugin === null ? extension.latest : extensionVersionForPlugin(extension.versions, plugin);
    };

    let pluginItem: ProjectUpdateItem | null = null;
    if (pluginSpec !== undefined && !opts.noPlugin) {
      const from = pluginSpec;
      pluginItem = !fromRegistry(pluginTarget) ? { ...pluginTarget, from, to: null, status: 'local' }
        : latestPlugin === undefined ? { ...pluginTarget, from, to: null, status: 'unknown' }
        : { ...pluginTarget, from, to: latestPlugin, status: shouldUpdateVersion(from, latestPlugin) ? 'outdated' : 'current' };
    }

    // The plugin release the project runs now (not only what the manifest's entry says), and the one this update
    // takes it to: the latest plugin, when that is newer. `next` null: the plugin the project runs does not move.
    const running = await resolveRunningPlugin(projectPath, dependencies, new Map(lookedUp));
    const latest = latestPlugin === undefined ? null : pluginRelease(latestPlugin);
    const moves = latest !== null && (running === null ? pluginItem?.status === 'outdated' : isNewerVersion(running, latest));
    let next = moves ? latest : null;
    if (next !== null) {
      // The new plugin alone would switch every extension without a version for it off: wait for them.
      const waiting = opts.noExtensions ? [] : registryExtensions.filter((target) => typeof versionFor(target, next) !== 'string');
      if (waiting.length > 0) {
        warnings.push(
          `MCP plugin ${next} is out, but no version for it is known yet of: ${waiting.map((target) => target.name).join(', ')} ` +
          '(an extension works with exactly one plugin release). The plugin is not updated: run the update ' +
          'again later, or update the plugin alone without its extensions.',
        );
        if (pluginItem?.status === 'outdated') pluginItem.status = 'held';
        next = null;
      }
    }
    // The plugin release the extensions must be built for.
    const plugin = next ?? running;

    const extensionItems = opts.noExtensions ? [] : extensions.map((target): ProjectUpdateItem => {
      const from = dependencies[target.packageId]!;
      if (!fromRegistry(target)) return { ...target, from, to: null, status: 'local' };
      const to = versionFor(target, plugin);
      if (to === undefined) return { ...target, from, to: null, status: 'unknown' };
      if (to === null) return { ...target, from, to, status: 'unavailable' };
      // Never a downgrade: the plugin that runs is at least the one every installed version names.
      return { ...target, from, to, status: shouldUpdateVersion(from, to) ? 'outdated' : 'current' };
    });
    const items = [...(pluginItem ? [pluginItem] : []), ...extensionItems];

    for (const item of extensionItems) {
      if (item.status === 'unavailable') {
        warnings.push(
          `${item.name} ${item.from}: no version for plugin ${plugin} is published. It does not work with that plugin ` +
          '(an extension works with exactly one plugin release): update the plugin, or run the update again once its ' +
          'extensions are published.',
        );
      }
    }
    if (next !== null && opts.noExtensions && extensions.length > 0) {
      warnings.push(
        `The installed extensions (${extensions.map((target) => target.name).join(', ')}) are left as they are: they do not ` +
        `work with plugin ${next} until they are moved to their versions for it (the update without noExtensions).`,
      );
    }

    const updated: ProjectUpdateItem[] = [];
    if (!opts.dryRun) {
      const planned = items.filter(item => item.status === 'outdated');
      if (planned.length > 0) {
        addPackagesToManifest(projectPath, planned.map(item => ({ packageId: item.packageId, version: item.to! })), originalManifest, () => {
          // A direct plugin update, or extensions that bring a new transitive plugin.
          if (planned.some(item => item.kind === 'plugin') || (next !== null && pluginItem === null)) closePluginGate(projectPath);
        });
        updated.push(...planned);
        emitProgress(opts.onProgress, { phase: 'manifest-patched', message: `Updated ${manifestPath}`, manifestPath });
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
