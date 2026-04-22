import * as fs from 'fs';
import * as path from 'path';
import { isNewerVersion, isValidVersion } from './semver.js';
import * as ui from './ui.js';
import type { LibLogger } from '../lib/logger.js';

/**
 * Default logger used by CLI call sites. Mirrors the historical
 * chalk-styled output. Library callers pass `silentLogger` instead.
 */
const defaultLogger: LibLogger = {
  info: (msg) => ui.info(msg),
  success: (msg) => ui.success(msg),
  warn: (msg) => ui.warn(msg),
  error: (msg) => ui.error(msg),
};

const PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
const REGISTRY_NAME = 'package.openupm.com';
const REGISTRY_URL = 'https://package.openupm.com';
const REQUIRED_SCOPES = [
  'com.ivanmurzak',
  'extensions.unity',
  'org.nuget.com.ivanmurzak',
  'org.nuget.microsoft',
  'org.nuget.system',
  'org.nuget.r3',
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
 * @param logger Optional logger. Defaults to chalk-styled CLI output;
 *   pass `silentLogger` from library call sites to suppress all output.
 */
export async function resolveLatestVersion(logger: LibLogger = defaultLogger): Promise<string> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 5000);

  try {
    const res = await fetch(`https://package.openupm.com/${PACKAGE_ID}`, {
      signal: controller.signal,
      headers: { Accept: 'application/json' },
    });

    if (res.ok) {
      const data = (await res.json()) as { 'dist-tags'?: { latest?: string } };
      const latest = data?.['dist-tags']?.latest;
      if (latest) {
        logger.info(`Resolved latest version from OpenUPM: ${latest}`);
        return latest;
      }
    }

    throw new Error(
      `OpenUPM returned status ${res.status}. ` +
      'Check your network connection and retry, or specify a version manually with --plugin-version <version>'
    );
  } catch (err) {
    if (err instanceof Error && err.message.includes('--plugin-version')) {
      throw err;
    }
    throw new Error(
      'Failed to resolve latest plugin version from OpenUPM. ' +
      'Check your network connection and retry, or specify a version manually with --plugin-version <version>'
    );
  } finally {
    clearTimeout(timeout);
  }
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
  const nonSemverPrefixes = ['file:', 'git+', 'http:', 'https:'];
  if (nonSemverPrefixes.some((prefix) => currentVersion.startsWith(prefix))) {
    return false;
  }

  if (isValidVersion(currentVersion) && isValidVersion(newVersion)) {
    return isNewerVersion(currentVersion, newVersion);
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
 * @param logger Optional logger. Defaults to chalk-styled CLI output;
 *   pass `silentLogger` from library call sites to suppress all output.
 */
export function addPluginToManifest(
  projectPath: string,
  version: string,
  force = false,
  logger: LibLogger = defaultLogger,
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

  const currentVersion = manifest.dependencies[PACKAGE_ID];
  let resolvedVersion = version;
  if (!currentVersion || force || shouldUpdateVersion(currentVersion, version)) {
    manifest.dependencies[PACKAGE_ID] = version;
    modified = true;
  } else {
    resolvedVersion = currentVersion;
    logger.info(
      `Plugin already at version ${currentVersion} (>= ${version}). Skipping version update. Use --plugin-version to force a specific version.`
    );
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
 * @param logger Optional logger. Defaults to chalk-styled CLI output;
 *   pass `silentLogger` from library call sites to suppress all output.
 */
export function removePluginFromManifest(
  projectPath: string,
  logger: LibLogger = defaultLogger,
): RemovePluginResult {
  const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');

  if (!fs.existsSync(manifestPath)) {
    throw new Error(`manifest.json not found at: ${manifestPath}`);
  }

  const rawJson = fs.readFileSync(manifestPath, 'utf-8');
  const manifest: Manifest = JSON.parse(rawJson);

  if (!manifest.dependencies || !(PACKAGE_ID in manifest.dependencies)) {
    logger.info('Unity-MCP plugin is not installed. Nothing to remove.');
    return { removed: false, manifestPath };
  }

  delete manifest.dependencies[PACKAGE_ID];
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  logger.success(`Removed ${PACKAGE_ID} from ${manifestPath}`);
  return { removed: true, manifestPath };
}
