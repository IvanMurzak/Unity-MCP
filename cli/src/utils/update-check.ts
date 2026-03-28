import { homedir } from 'os';
import { join } from 'path';
import { readFileSync, writeFileSync } from 'fs';
import chalk from 'chalk';
import { isNewerVersion, isValidVersion } from './semver.js';

const PACKAGE_NAME = 'unity-mcp-cli';
const NPM_REGISTRY_URL = `https://registry.npmjs.org/${PACKAGE_NAME}/latest`;
const CACHE_FILE = join(homedir(), '.unity-mcp-cli-update.json');
const CACHE_TTL_MS = 24 * 60 * 60 * 1000; // 24 hours
const FETCH_TIMEOUT_MS = 3000;

interface UpdateCache {
  latestVersion: string;
  lastChecked: number;
}

interface UpdateInfo {
  current: string;
  latest: string;
}

/** Fetch the latest version string from the npm registry. */
export async function fetchLatestVersion(): Promise<string> {
  const response = await fetch(NPM_REGISTRY_URL, {
    signal: AbortSignal.timeout(FETCH_TIMEOUT_MS),
    headers: { 'Accept': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`npm registry returned ${response.status}`);
  }

  const data = (await response.json()) as { version?: string };
  if (!data.version) {
    throw new Error('No version field in registry response');
  }
  return data.version;
}

function readCache(): UpdateCache | null {
  try {
    const raw = readFileSync(CACHE_FILE, 'utf-8');
    const parsed = JSON.parse(raw) as UpdateCache;
    if (parsed.latestVersion && typeof parsed.lastChecked === 'number' && isValidVersion(parsed.latestVersion)) {
      return parsed;
    }
  } catch {
    // Cache missing or corrupt — ignore
  }
  return null;
}

function writeCache(latestVersion: string): void {
  try {
    const data: UpdateCache = { latestVersion, lastChecked: Date.now() };
    writeFileSync(CACHE_FILE, JSON.stringify(data), 'utf-8');
  } catch {
    // Non-critical — ignore write failures
  }
}

/**
 * Check if a newer version is available on npm.
 * Returns update info if available, null otherwise.
 * Never throws — all errors are silently caught.
 */
export async function checkForUpdate(currentVersion: string): Promise<UpdateInfo | null> {
  try {
    // Check cache first
    const cache = readCache();
    if (cache && Date.now() - cache.lastChecked < CACHE_TTL_MS) {
      if (isNewerVersion(currentVersion, cache.latestVersion)) {
        return { current: currentVersion, latest: cache.latestVersion };
      }
      return null;
    }

    // Fetch fresh
    const latest = await fetchLatestVersion();
    writeCache(latest);

    if (isNewerVersion(currentVersion, latest)) {
      return { current: currentVersion, latest };
    }
  } catch {
    // Best-effort — never disrupt CLI usage
  }
  return null;
}

/** Format the "update available" message with chalk styling. */
export function formatUpdateAvailable(current: string, latest: string): string {
  return `Update available: ${chalk.dim(current)} → ${chalk.green(latest)}`;
}

/** Print a styled update notification to stderr (does not interfere with stdout piping). */
export function printUpdateNotification(current: string, latest: string): void {
  console.error();
  console.error(chalk.yellow(`  ${formatUpdateAvailable(current, latest)}`));
  console.error(chalk.dim(`  Run ${chalk.cyan(`npm i -g ${PACKAGE_NAME}`)} to update`));
  console.error();
}

/** Returns true if the CLI was likely invoked via npx. */
export function isRunningViaNpx(): boolean {
  const execPath = process.env['npm_execpath'] ?? '';
  const npmCommand = process.env['npm_command'] ?? '';
  return execPath.includes('npx') || npmCommand === 'exec';
}
