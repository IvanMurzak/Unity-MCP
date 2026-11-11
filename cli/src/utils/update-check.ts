import { homedir } from 'os';
import { join } from 'path';
import chalk from 'chalk';
import { checkForNpmUpdate, fetchLatestNpmVersion, npmGlobalInstallCommand, type UpdateInfo } from '@baizor/gamedev-cli-core';

export { isRunningViaNpx } from '@baizor/gamedev-cli-core';

const PACKAGE_NAME = 'unity-mcp-cli';
const CACHE_FILE = join(homedir(), '.unity-mcp-cli-update.json');

/** Fetch the latest version string from the npm registry. */
export function fetchLatestVersion(): Promise<string> {
  return fetchLatestNpmVersion(PACKAGE_NAME);
}

/**
 * Check if a newer version is available on npm (cached for a day).
 * Returns update info if available, null otherwise. Never throws.
 */
export function checkForUpdate(currentVersion: string): Promise<UpdateInfo | null> {
  return checkForNpmUpdate(currentVersion, { packageName: PACKAGE_NAME, cacheFile: CACHE_FILE });
}

/** Format the "update available" message with chalk styling. */
export function formatUpdateAvailable(current: string, latest: string): string {
  return `Update available: ${chalk.dim(current)} → ${chalk.green(latest)}`;
}

/** The exact command a user should run to update, printed ALONE on its own line (see {@link npmGlobalInstallCommand}). */
export const UPDATE_COMMAND = npmGlobalInstallCommand(PACKAGE_NAME);

/** Print a styled update notification to stderr (does not interfere with stdout piping). */
export function printUpdateNotification(current: string, latest: string): void {
  console.error();
  console.error(chalk.yellow(`  ${formatUpdateAvailable(current, latest)}. To update, run:`));
  console.error(`    ${chalk.cyan(UPDATE_COMMAND)}`);
  console.error();
}
