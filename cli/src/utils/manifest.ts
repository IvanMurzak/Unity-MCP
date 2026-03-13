import * as fs from 'fs';
import * as path from 'path';
import * as ui from './ui.js';

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

// Hardcoded fallback version (updated on each release)
const FALLBACK_VERSION = '0.52.0';

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
 * Resolve the latest plugin version. Tries OpenUPM API first, falls back to hardcoded version.
 */
export async function resolveLatestVersion(): Promise<string> {
  try {
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
          ui.info(`Resolved latest version from OpenUPM: ${latest}`);
          return latest;
        }
      }
    } finally {
      clearTimeout(timeout);
    }
  } catch {
    // Network error or timeout — fall through to hardcoded version
  }

  ui.warn(`Using fallback version: ${FALLBACK_VERSION}`);
  return FALLBACK_VERSION;
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

  const numericOnly = /^\d+(\.\d+)*$/;
  if (numericOnly.test(currentVersion) && numericOnly.test(newVersion)) {
    const currentParts = currentVersion.split('.').map(Number);
    const targetParts = newVersion.split('.').map(Number);
    const len = Math.max(currentParts.length, targetParts.length);
    for (let i = 0; i < len; i++) {
      const c = currentParts[i] ?? 0;
      const t = targetParts[i] ?? 0;
      if (t !== c) return t > c;
    }
    return false;
  }

  return newVersion.toLowerCase() > currentVersion.toLowerCase();
}

/**
 * Add Unity-MCP plugin to a Unity project's Packages/manifest.json.
 * Ports the C# Installer.Manifest.cs logic:
 * - Adds OpenUPM scoped registry with required scopes
 * - Adds/updates the plugin dependency (never downgrades)
 */
export function addPluginToManifest(projectPath: string, version: string): void {
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
  if (!currentVersion || shouldUpdateVersion(currentVersion, version)) {
    manifest.dependencies[PACKAGE_ID] = version;
    modified = true;
  } else {
    ui.info(
      `Plugin already at version ${currentVersion} (>= ${version}). Skipping version update.`
    );
  }

  // --- Write back
  if (modified) {
    fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
    ui.success(`Updated ${manifestPath}`);
  } else {
    ui.info('manifest.json is already up to date.');
  }
}

/**
 * Remove Unity-MCP plugin from a Unity project's Packages/manifest.json.
 * Only removes the plugin dependency — scoped registries and scopes are
 * left untouched because other packages may depend on them.
 */
export function removePluginFromManifest(projectPath: string): void {
  const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');

  if (!fs.existsSync(manifestPath)) {
    throw new Error(`manifest.json not found at: ${manifestPath}`);
  }

  const rawJson = fs.readFileSync(manifestPath, 'utf-8');
  const manifest: Manifest = JSON.parse(rawJson);

  if (!manifest.dependencies || !(PACKAGE_ID in manifest.dependencies)) {
    ui.info('Unity-MCP plugin is not installed. Nothing to remove.');
    return;
  }

  delete manifest.dependencies[PACKAGE_ID];
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  ui.success(`Removed ${PACKAGE_ID} from ${manifestPath}`);
}
