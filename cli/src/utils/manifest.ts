import * as fs from 'fs';
import * as path from 'path';
import { isNewerVersion, isNumericVersion } from '@baizor/gamedev-cli-core';
import { silentLogger, type LibLogger } from '../lib/logger.js';
import { unityExtensionDependencies } from './extension-compatibility.js';

/** The Unity-MCP plugin's package id. */
export const PLUGIN_PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
const REGISTRY_NAME = 'package.openupm.com';
const REGISTRY_URL = 'https://package.openupm.com';
const REQUIRED_SCOPES = [
  'com.ivanmurzak',
  'extensions.unity',
];

interface ScopedRegistry {
  name: string;
  url: string;
  scopes: string[];
}

interface Manifest {
  dependencies?: Record<string, string>;
  scopedRegistries?: ScopedRegistry[];
  [key: string]: unknown;
}

/**
 * Resolve the latest plugin version from the OpenUPM registry.
 * Throws an error with actionable suggestions if the network request fails.
 *
 * @param logger Optional logger. Defaults to `silentLogger` so library
 *   callers stay side-effect-free; CLI call sites must pass a chalk-
 *   styled logger adapter explicitly to preserve the historical output.
 */
export async function resolveLatestVersion(logger: LibLogger = silentLogger): Promise<string> {
  return resolveLatestPackageVersion(PLUGIN_PACKAGE_ID, logger);
}

/**
 * Resolve the latest version of ANY OpenUPM-hosted package (the plugin itself, or
 * one of the catalogue extensions) from the OpenUPM registry. Throws an error with
 * actionable suggestions if the network request fails.
 *
 * This is the generalisation of {@link resolveLatestVersion}: same 5000 ms
 * `AbortController` abort, same "no cached fallback" behaviour, same actionable
 * error — the only difference is that the package id is a parameter and the
 * suggested escape-hatch flag is caller-supplied, because `install-plugin` offers
 * `--plugin-version` where `install-extension` offers `--version`.
 *
 * @param packageId OpenUPM package id to resolve (e.g. `com.ivanmurzak.unity.mcp.tilemap`).
 * @param logger Optional logger. Defaults to `silentLogger` so library callers stay
 *   side-effect-free.
 * @param versionFlag Name of the CLI flag suggested in the failure message.
 */
export async function resolveLatestPackageVersion(
  packageId: string,
  logger: LibLogger = silentLogger,
  versionFlag = '--plugin-version',
): Promise<string> {
  const latest = await readRegistryDocument(packageId, versionFlag, 'resolve the latest version of', (document) => document['dist-tags']?.latest || undefined);
  logger.info(`Resolved latest version from OpenUPM: ${latest}`);
  return latest;
}

/** What OpenUPM serves for a package: its latest tag and every version with its own dependencies. */
interface RegistryDocument {
  'dist-tags'?: { latest?: string };
  versions?: Record<string, { dependencies?: Record<string, string> } | undefined>;
}

/**
 * Fetch a package's OpenUPM document and return what `pick` reads from it. `undefined` from `pick` means the
 * document does not hold the answer, which fails like a failed request: with the same actionable error.
 */
async function readRegistryDocument<T>(
  packageId: string,
  versionFlag: string,
  what: string,
  pick: (document: RegistryDocument) => T | undefined,
): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 5000);

  try {
    const res = await fetch(`https://package.openupm.com/${packageId}`, {
      signal: controller.signal,
      headers: { Accept: 'application/json' },
    });

    if (res.ok) {
      const picked = pick(((await res.json()) ?? {}) as RegistryDocument);
      if (picked !== undefined) return picked;
    }

    throw new Error(
      `OpenUPM returned status ${res.status}. ` +
      `Check your network connection and retry, or specify a version manually with ${versionFlag} <version>`
    );
  } catch (err) {
    if (err instanceof Error && err.message.includes(versionFlag)) {
      throw err;
    }
    throw new Error(
      `Failed to ${what} ${packageId} from OpenUPM. ` +
      `Check your network connection and retry, or specify a version manually with ${versionFlag} <version>`
    );
  } finally {
    clearTimeout(timeout);
  }
}

/**
 * The plugin release a manifest entry stands for: its `major.minor.patch` (build metadata changes nothing), or
 * null for anything else (`file:`, git, a range, a pre-release: no extension is built for a pre-release plugin,
 * Unity orders it below its release).
 */
export function pluginRelease(spec: string): string | null {
  const match = /^(\d+\.\d+\.\d+)(?:\+.*)?$/.exec(spec.trim());
  return match ? match[1] : null;
}

/**
 * The highest published stable version of an extension built for plugin release `plugin`: the one whose
 * dependency on the plugin names exactly that version. An extension works with exactly one plugin release
 * (its assemblies name it), so any other version is skipped by Unity without an error.
 */
export function extensionVersionForPlugin(
  versions: NonNullable<RegistryDocument['versions']>,
  plugin: string,
): string | null {
  let best: string | null = null;
  for (const [version, info] of Object.entries(versions)) {
    if (!isNumericVersion(version) || info?.dependencies?.[PLUGIN_PACKAGE_ID] !== plugin) continue;
    if (best === null || isNewerVersion(best, version)) best = version;
  }
  return best;
}

/** What OpenUPM publishes of an extension: its latest version and every version with its dependencies. */
export interface PublishedExtension {
  latest: string | null;
  versions: NonNullable<RegistryDocument['versions']>;
}

/**
 * Read from OpenUPM what is published of an extension. Throws like {@link resolveLatestPackageVersion} when the
 * registry cannot be read.
 */
export async function fetchPublishedExtension(packageId: string, versionFlag = '--extension-version'): Promise<PublishedExtension> {
  return readRegistryDocument(packageId, versionFlag, 'read the published versions of', (document) =>
    document.versions ? { latest: document['dist-tags']?.latest || null, versions: document.versions } : undefined);
}

/**
 * Resolve, from OpenUPM, the version of an extension built for plugin release `plugin` (see
 * {@link extensionVersionForPlugin}); null when none is published (yet). Throws like
 * {@link resolveLatestPackageVersion} when the registry cannot be read.
 */
export async function resolveExtensionVersionForPlugin(
  packageId: string,
  plugin: string,
  versionFlag = '--extension-version',
): Promise<string | null> {
  return extensionVersionForPlugin((await fetchPublishedExtension(packageId, versionFlag)).versions, plugin);
}

const READY_DEFINE = 'UNITY_MCP_READY';

/**
 * Remove the plugin's "dependencies are in place" define from the project's scripting defines, as the plugin's
 * own update does before it swaps its files. The define outlives a plugin change, and until the new plugin has
 * restored its dependencies it would let Unity compile what is built on the plugin (an extension published
 * before extensions named their plugin release) while the plugin itself is still skipped: compile errors, Safe
 * Mode. Without it Unity skips all of it, the plugin restores its dependencies and sets the define again.
 * Best effort and never throws: an unreadable or binary settings file is left alone. True when the file changed.
 */
export function closePluginGate(projectPath: string): boolean {
  const file = path.join(projectPath, 'ProjectSettings', 'ProjectSettings.asset');
  try {
    const before = fs.readFileSync(file, 'utf-8');
    const after = before.replace(/^(  scriptingDefineSymbols:\r?\n)((?:    .*\r?\n)*)/m, (_match, head: string, block: string) =>
      head + block.replace(/^(    [^:\r\n]+:)(.*)$/gm, (line: string, key: string, value: string) => {
        const defines = value.trim().split(';').filter((define) => define !== '');
        return defines.includes(READY_DEFINE) ? `${key} ${defines.filter((define) => define !== READY_DEFINE).join(';')}` : line;
      }));
    if (after === before) return false;
    fs.writeFileSync(file, after);
    return true;
  } catch {
    return false;
  }
}

/** A `file:`, git or URL dependency: a local or pinned source, not a registry version. */
export function isNonSemverSpec(spec: string): boolean {
  return ['file:', 'git+', 'git:', 'http:', 'https:', 'ssh:'].some((prefix) => spec.startsWith(prefix));
}

/**
 * Determines if the version should be updated.
 * Only update if the new version is higher than the current version.
 * Ports the C# Installer.ShouldUpdateVersion() logic.
 */
export function shouldUpdateVersion(currentVersion: string, newVersion: string): boolean {
  if (!currentVersion) return true;
  if (!newVersion) return false;

  // Skip automatic update for non-semver specs (file:, git+, http, etc.)
  if (isNonSemverSpec(currentVersion)) {
    return false;
  }

  if (isNumericVersion(currentVersion) && isNumericVersion(newVersion)) {
    return isNewerVersion(currentVersion, newVersion);
  }

  // A prerelease on either side: compare the numbers, and a prerelease is older than its own release.
  const current = /^(\d+)\.(\d+)\.(\d+)(-.+)?$/.exec(currentVersion);
  const next = /^(\d+)\.(\d+)\.(\d+)(-.+)?$/.exec(newVersion);
  if (current && next) {
    for (let part = 1; part <= 3; part++) {
      const difference = Number(next[part]) - Number(current[part]);
      if (difference !== 0) return difference > 0;
    }
    return current[4] !== undefined && next[4] === undefined;
  }

  return newVersion.toLowerCase() > currentVersion.toLowerCase();
}

export interface AddPluginResult {
  /** Whether the file was modified on disk (false = already up to date). */
  modified: boolean;
  /** Final plugin version in the manifest (may differ from the requested
   *  version if the existing version was higher and force was false). */
  resolvedVersion: string;
  /** Absolute path to the manifest.json that was inspected / written. */
  manifestPath: string;
}

/**
 * Add Unity-MCP plugin to a Unity project's Packages/manifest.json.
 * Ports the C# Installer.Manifest.cs logic:
 * - Adds OpenUPM scoped registry with required scopes
 * - Adds/updates the plugin dependency
 * - When force is false (auto-resolved version): never downgrades
 * - When force is true (user-specified --plugin-version): allows downgrade
 *
 * @param logger Optional logger. Defaults to `silentLogger` so library
 *   callers stay side-effect-free; CLI call sites must pass a chalk-
 *   styled logger adapter explicitly to preserve the historical output.
 */
export function addPluginToManifest(
  projectPath: string,
  version: string,
  force = false,
  logger: LibLogger = silentLogger,
): AddPluginResult {
  return addPackageToManifest(projectPath, PLUGIN_PACKAGE_ID, version, force, logger);
}

/**
 * Add ANY OpenUPM-hosted package to a Unity project's `Packages/manifest.json` —
 * the plugin itself, or one of the catalogue extensions.
 *
 * This is the generalisation of {@link addPluginToManifest}, which now delegates
 * here with `PLUGIN_PACKAGE_ID`. Keeping ONE implementation matters because the
 * scoped-registry rules (registry name, URL, and the `REQUIRED_SCOPES` pair
 * `com.ivanmurzak` / `extensions.unity`) must be identical for the plugin and for
 * every extension — the extensions live under the `com.ivanmurzak` scope, so an
 * extension installed without that scope resolves against Unity's default registry
 * and fails. It also mirrors the C# editor installer
 * (`ExtensionPanel.AddToManifest` / `EnsureScopedRegistry`), which single-sources
 * the same three constants on the Unity side.
 *
 * Behaviour (unchanged from the plugin-only version):
 * - Adds the OpenUPM scoped registry with the required scopes when missing.
 * - Adds/updates the package dependency.
 * - When `force` is false (auto-resolved version): never downgrades.
 * - When `force` is true (user-specified version): allows downgrade.
 */
export function addPackageToManifest(
  projectPath: string,
  packageId: string,
  version: string,
  force = false,
  logger: LibLogger = silentLogger,
): AddPluginResult {
  const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');

  if (!fs.existsSync(manifestPath)) {
    throw new Error(`manifest.json not found at: ${manifestPath}`);
  }

  const rawJson = fs.readFileSync(manifestPath, 'utf-8');
  const manifest: Manifest = JSON.parse(rawJson);
  let modified = false;

  // --- Ensure scopedRegistries array exists
  if (!manifest.scopedRegistries) {
    manifest.scopedRegistries = [];
    modified = true;
  }

  // --- Find or create the OpenUPM registry
  let openUpmRegistry = manifest.scopedRegistries.find(
    (r) => r.name === REGISTRY_NAME
  );

  if (!openUpmRegistry) {
    openUpmRegistry = {
      name: REGISTRY_NAME,
      url: REGISTRY_URL,
      scopes: [],
    };
    manifest.scopedRegistries.push(openUpmRegistry);
    modified = true;
  }

  // --- Add missing scopes
  if (!openUpmRegistry.scopes) {
    openUpmRegistry.scopes = [];
    modified = true;
  }

  for (const scope of REQUIRED_SCOPES) {
    if (!openUpmRegistry.scopes.includes(scope)) {
      openUpmRegistry.scopes.push(scope);
      modified = true;
    }
  }

  // --- Add/update dependency (version-aware, never downgrade)
  if (!manifest.dependencies) {
    manifest.dependencies = {};
    modified = true;
  }

  const currentVersion = manifest.dependencies[packageId];
  let resolvedVersion = version;
  if (!currentVersion || force || shouldUpdateVersion(currentVersion, version)) {
    manifest.dependencies[packageId] = version;
    modified = true;
  } else {
    resolvedVersion = currentVersion;
    logger.info(
      `${packageId} already at version ${currentVersion} (>= ${version}). Skipping version update. Pass an explicit version to force a specific version.`
    );
  }

  // UPM dependencies cannot vary by editor version. Select compatible external packages
  // before Unity imports an extension; old package floors retain Unity 2022/2023 support.
  const editorVersionPath = path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt');
  const editorVersion = fs.existsSync(editorVersionPath)
    ? /^m_EditorVersion:\s*(\S+)/m.exec(fs.readFileSync(editorVersionPath, 'utf8'))?.[1] ?? ''
    : '';
  for (const [dependency, required] of Object.entries(unityExtensionDependencies(packageId, editorVersion))) {
    const existing = manifest.dependencies[dependency];
    if (!existing || (isNumericVersion(existing) && shouldUpdateVersion(existing, required))) {
      manifest.dependencies[dependency] = required;
      modified = true;
    }
  }

  // --- Write back
  if (modified) {
    fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
    logger.success(`Updated ${manifestPath}`);
  } else {
    logger.info('manifest.json is already up to date.');
  }

  return { modified, resolvedVersion, manifestPath };
}

export interface RemovePluginResult {
  /** Whether the plugin was present and has been removed. */
  removed: boolean;
  /** Absolute path to the manifest.json that was inspected. */
  manifestPath: string;
}

/**
 * Remove Unity-MCP plugin from a Unity project's Packages/manifest.json.
 * Only removes the plugin dependency — scoped registries and scopes are
 * left untouched because other packages may depend on them.
 *
 * @param logger Optional logger. Defaults to `silentLogger` so library
 *   callers stay side-effect-free; CLI call sites must pass a chalk-
 *   styled logger adapter explicitly to preserve the historical output.
 */
export function removePluginFromManifest(
  projectPath: string,
  logger: LibLogger = silentLogger,
): RemovePluginResult {
  const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');

  if (!fs.existsSync(manifestPath)) {
    throw new Error(`manifest.json not found at: ${manifestPath}`);
  }

  const rawJson = fs.readFileSync(manifestPath, 'utf-8');
  const manifest: Manifest = JSON.parse(rawJson);

  if (!manifest.dependencies || !(PLUGIN_PACKAGE_ID in manifest.dependencies)) {
    logger.info('Unity-MCP plugin is not installed. Nothing to remove.');
    return { removed: false, manifestPath };
  }

  delete manifest.dependencies[PLUGIN_PACKAGE_ID];
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  logger.success(`Removed ${PLUGIN_PACKAGE_ID} from ${manifestPath}`);
  return { removed: true, manifestPath };
}
