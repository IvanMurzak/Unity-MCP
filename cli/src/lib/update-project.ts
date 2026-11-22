import * as fs from 'fs';
import * as path from 'path';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import {
  extensionVersionForPlugin,
  fetchPublishedExtension,
  isNonSemverSpec,
  lockedPluginVersions,
  PLUGIN_PACKAGE_ID,
  pluginRelease,
  resolveLatestPackageVersion,
  runningPluginRelease,
  shouldUpdateVersion,
  type PublishedExtension,
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
 * project ends up with, also when that is an older one. The project is never left half-way: everything is looked
 * up before anything is written, and a plugin release whose extensions are not all published yet is held back
 * (`held`) until they are. Only what the manifest already lists is touched, `file:`/git installs are left alone
 * and the plugin is never downgraded. One item failing is a warning and does not stop the others. Library-safe:
 * no stdout, no `process.exit`, no throws past the boundary.
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
    const checksPlugin = pluginSpec !== undefined && !opts.noPlugin;
    const [latestPlugin, published] = await Promise.all([
      checksPlugin && fromRegistry(pluginTarget) ? lookUp(pluginTarget, () => resolveLatestPackageVersion(PLUGIN_PACKAGE_ID, silentLogger)) : undefined,
      Promise.all((opts.noExtensions ? [] : extensions.filter(fromRegistry)).map(async (target) =>
        [target.packageId, await lookUp(target, () => fetchPublishedExtension(target.packageId))] as const)),
    ]);
    const publishedOf = new Map<string, PublishedExtension | undefined>(published);

    /** The version of an extension to have under plugin release `plugin` (null: none is published; undefined: unknown). */
    const versionFor = (target: Target, plugin: string | null): string | null | undefined => {
      const extension = publishedOf.get(target.packageId);
      if (extension === undefined) return undefined;
      // Without a plugin release in the manifest (a local source, or no plugin of its own): the latest one.
      return plugin === null ? extension.latest : extensionVersionForPlugin(extension.versions, plugin);
    };

    // The plugin release the extensions must be built for: the one the project runs, unless the plugin moves
    // below. That is the manifest entry or a newer one an installed extension names: as OpenUPM tells for the
    // extensions just read, as Unity's lock file tells for the rest.
    const named = lockedPluginVersions(projectPath, dependencies);
    for (const [packageId, extension] of publishedOf) {
      const namedPlugin = extension?.versions[dependencies[packageId]!]?.dependencies?.[PLUGIN_PACKAGE_ID];
      if (namedPlugin !== undefined) named.set(packageId, namedPlugin);
    }
    let plugin = runningPluginRelease(pluginSpec, named.values());
    let pluginItem: ProjectUpdateItem | null = null;
    if (checksPlugin) {
      const from = pluginSpec!;
      pluginItem = !fromRegistry(pluginTarget) ? { ...pluginTarget, from, to: null, status: 'local' }
        : latestPlugin === undefined ? { ...pluginTarget, from, to: null, status: 'unknown' }
        : { ...pluginTarget, from, to: latestPlugin, status: shouldUpdateVersion(from, latestPlugin) ? 'outdated' : 'current' };
    }
    if (pluginItem?.status === 'outdated') {
      const next = pluginRelease(pluginItem.to!);
      // The new plugin alone would switch every extension without a version for it off: wait for them.
      const waiting = next === null ? [] : [...publishedOf.keys()]
        .map((packageId) => extensions.find((target) => target.packageId === packageId)!)
        .filter((target) => typeof versionFor(target, next) !== 'string');
      if (waiting.length === 0) {
        plugin = next;
      } else {
        pluginItem.status = 'held';
        warnings.push(
          `MCP plugin ${pluginItem.to} is out, but no version for it is known yet of: ${waiting.map((target) => target.name).join(', ')} ` +
          `(an extension works with exactly one plugin release). The plugin stays at ${pluginItem.from}: run the update ` +
          'again later, or update the plugin alone without its extensions.',
        );
      }
    }

    const extensionItems = opts.noExtensions ? [] : extensions.map((target): ProjectUpdateItem => {
      const from = dependencies[target.packageId]!;
      if (!fromRegistry(target)) return { ...target, from, to: null, status: 'local' };
      const to = versionFor(target, plugin);
      if (to === undefined) return { ...target, from, to: null, status: 'unknown' };
      if (to === null) return { ...target, from, to, status: 'unavailable' };
      // For a plugin release: the one version built for it, in either direction. Otherwise never a downgrade.
      const applies = plugin === null ? shouldUpdateVersion(from, to) : to !== from;
      return { ...target, from, to, status: applies ? 'outdated' : 'current' };
    });
    const items = [...(pluginItem ? [pluginItem] : []), ...extensionItems];

    for (const item of extensionItems) {
      if (item.status === 'unavailable') {
        warnings.push(
          `${item.name} ${item.from}: no version for plugin ${plugin} is published. It does not work with that plugin ` +
          '(an extension works with exactly one plugin release): update the plugin, or run the update again once its ' +
          'extensions are published.',
        );
      } else if (item.status === 'outdated' && plugin !== null && shouldUpdateVersion(item.to!, item.from)) {
        warnings.push(
          `${item.name} ${item.from} is built for another plugin release, so it does not work with plugin ${plugin}: ` +
          `${item.to} is the version built for this plugin.`,
        );
      }
    }
    const pluginMoves = pluginItem?.status === 'outdated';
    if (pluginMoves && opts.noExtensions && extensions.length > 0) {
      warnings.push(
        `The installed extensions (${extensions.map((target) => target.name).join(', ')}) are left as they are: they do not ` +
        `work with plugin ${pluginItem!.to} until they are moved to their versions for it (the update without noExtensions).`,
      );
    }

    const updated: ProjectUpdateItem[] = [];
    /** Write one outdated item; false (and a warning) when that failed. */
    const apply = async (item: ProjectUpdateItem): Promise<boolean> => {
      const result = item.kind === 'plugin'
        ? await installPlugin({ unityProjectPath: projectPath, version: item.to!, onProgress: opts.onProgress })
        : await installExtension({ unityProjectPath: projectPath, extensionId: item.packageId, version: item.to!, onProgress: opts.onProgress });
      if (result.kind === 'failure') {
        warnings.push(`Could not update ${item.name}: ${result.error.message}`);
        return false;
      }
      warnings.push(...result.warnings);
      updated.push(item);
      return true;
    };

    if (!opts.dryRun) {
      // The extensions were chosen for the plugin this call installs: without it they stay where they are.
      if (pluginMoves && !(await apply(pluginItem!))) {
        if (extensionItems.some((item) => item.status === 'outdated')) warnings.push('The installed extensions were left as they are.');
      } else {
        for (const item of extensionItems.filter((candidate) => candidate.status === 'outdated')) await apply(item);
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
