import fs from 'fs';
import path from 'path';

const MANIFEST_RELATIVE_PATH = path.join('Packages', 'manifest.json');

const PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
const REGISTRY_NAME = 'package.openupm.com';
const REGISTRY_URL = 'https://package.openupm.com';

// Scopes required for Unity MCP and its NuGet / OpenUPM dependencies
const REQUIRED_SCOPES = [
  'com.ivanmurzak',
  'extensions.unity',
  'org.nuget.com.ivanmurzak',
  'org.nuget.microsoft',
  'org.nuget.system',
  'org.nuget.r3',
];

const OPENUPM_PACKAGE_INFO_URL = `https://package.openupm.com/${PACKAGE_ID}`;

/**
 * Fetches the latest published version of com.ivanmurzak.unity.mcp from OpenUPM.
 * Falls back to a known stable version when the registry is unreachable.
 * @returns {Promise<string>} Version string (e.g. "0.48.1")
 */
export async function fetchLatestVersion() {
  try {
    const response = await fetch(OPENUPM_PACKAGE_INFO_URL);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data = await response.json();
    const latest = data['dist-tags']?.latest;
    if (latest) return latest;
    // Fall back to the highest version key
    const versions = Object.keys(data.versions ?? {});
    if (versions.length > 0) {
      versions.sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
      return versions[0];
    }
    throw new Error('No versions found in registry response');
  } catch (err) {
    const fallback = '0.48.1';
    console.warn(`Warning: Could not fetch latest version from OpenUPM (${err.message}).`);
    console.warn(`Falling back to version ${fallback}.`);
    return fallback;
  }
}

/**
 * Modifies a Unity project's Packages/manifest.json to:
 *  - Add the OpenUPM scoped registry with all required scopes
 *  - Add (or update) the com.ivanmurzak.unity.mcp dependency
 *
 * @param {string} projectPath - Absolute path to the Unity project root
 * @param {string} version - Package version to install (e.g. "0.48.1")
 */
export function installUnityMcp(projectPath, version) {
  const manifestPath = path.join(projectPath, MANIFEST_RELATIVE_PATH);

  if (!fs.existsSync(manifestPath)) {
    throw new Error(
      `Packages/manifest.json not found at:\n  ${manifestPath}\n` +
      'Make sure the path points to the root of a valid Unity project.'
    );
  }

  const raw = fs.readFileSync(manifestPath, 'utf-8');
  let manifest;
  try {
    manifest = JSON.parse(raw);
  } catch (err) {
    throw new Error(`Failed to parse ${manifestPath}: ${err.message}`);
  }

  // --- Ensure scopedRegistries array exists
  if (!Array.isArray(manifest.scopedRegistries)) {
    manifest.scopedRegistries = [];
  }

  // --- Find or create the OpenUPM registry entry
  let registry = manifest.scopedRegistries.find(
    r => r.name === REGISTRY_NAME || r.url === REGISTRY_URL
  );
  if (!registry) {
    registry = { name: REGISTRY_NAME, url: REGISTRY_URL, scopes: [] };
    manifest.scopedRegistries.push(registry);
  }
  if (!Array.isArray(registry.scopes)) {
    registry.scopes = [];
  }

  // --- Add any missing scopes
  for (const scope of REQUIRED_SCOPES) {
    if (!registry.scopes.includes(scope)) {
      registry.scopes.push(scope);
    }
  }

  // --- Ensure dependencies object exists
  if (typeof manifest.dependencies !== 'object' || manifest.dependencies === null) {
    manifest.dependencies = {};
  }

  // --- Install / update the package version
  const current = manifest.dependencies[PACKAGE_ID];
  if (current === version) {
    console.log(`${PACKAGE_ID}@${version} is already present in manifest.`);
  } else {
    if (current) {
      console.log(`Updating ${PACKAGE_ID}: ${current} -> ${version}`);
    } else {
      console.log(`Adding ${PACKAGE_ID}@${version}`);
    }
    manifest.dependencies[PACKAGE_ID] = version;
  }

  // --- Write the updated manifest back (2-space indent, Unity convention)
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n', 'utf-8');
  console.log(`Manifest saved: ${manifestPath}`);
}
