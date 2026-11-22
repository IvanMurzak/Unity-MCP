import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import {
  fetchPublishedExtension,
  isNonSemverSpec,
  lockedPluginVersions,
  PLUGIN_PACKAGE_ID,
  pluginRelease,
  runningPluginRelease,
  type PublishedExtension,
} from '../utils/manifest.js';

/**
 * The plugin release a project runs, from its manifest (`dependencies`): the highest of the plugin entry and the
 * plugin versions the installed packages name (see `runningPluginRelease`). What an installed package names comes
 * from Unity's lock file; for a catalogue extension the lock does not hold at the manifest's version (a project
 * Unity has not resolved since the last manifest edit) it comes from OpenUPM. `published` is what the caller has
 * already read from OpenUPM, by package id (undefined: that lookup failed); what is read here is added to it. A
 * lookup that fails leaves that extension out: the release is then the best known one.
 */
export async function resolveRunningPlugin(
  projectPath: string,
  dependencies: Record<string, string>,
  published: Map<string, PublishedExtension | undefined> = new Map(),
): Promise<string | null> {
  const entry = dependencies[PLUGIN_PACKAGE_ID];
  // A local or pre-release plugin runs as it is, whatever anything names: nothing else needs to be read.
  if (entry !== undefined && pluginRelease(entry) === null) return null;

  const locked = lockedPluginVersions(projectPath, dependencies);
  const unlocked = EXTENSIONS_CATALOG
    .map((extension) => extension.packageId)
    .filter((packageId) => dependencies[packageId] !== undefined && !isNonSemverSpec(dependencies[packageId]!) && !locked.has(packageId));
  const named = await Promise.all(unlocked.map(async (packageId) => {
    if (!published.has(packageId)) published.set(packageId, await fetchPublishedExtension(packageId).catch(() => undefined));
    return published.get(packageId)?.versions[dependencies[packageId]!]?.dependencies?.[PLUGIN_PACKAGE_ID];
  }));
  return runningPluginRelease(entry, [...locked.values(), ...named]);
}
