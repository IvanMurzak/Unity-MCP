import { Command } from 'commander';
import { spawn } from 'child_process';
import { platform } from 'os';
import * as ui from '../utils/ui.js';
import { fetchLatestVersion, formatUpdateAvailable, isRunningViaNpx } from '../utils/update-check.js';
import { isNewerVersion } from '../utils/semver.js';

const PACKAGE_NAME = 'unity-mcp-cli';

interface UpdateOptions {
  check?: boolean;
}

function npmCommand(): string {
  return platform() === 'win32' ? 'npm.cmd' : 'npm';
}

function runNpmInstall(packageSpec: string): Promise<number> {
  return new Promise((resolve, reject) => {
    const child = spawn(npmCommand(), ['install', '-g', packageSpec], {
      stdio: 'inherit',
    });

    child.on('error', reject);
    child.on('close', (code) => resolve(code ?? 1));
  });
}

export function createUpdateCommand(currentVersion: string): Command {
  return new Command('update')
    .description('Update unity-mcp-cli to the latest version')
    .option('--check', 'Only check for updates without installing')
    .action(async (options: UpdateOptions) => {
      if (isRunningViaNpx()) {
        ui.info('Running via npx — it always fetches the latest published version.');
        ui.info('No update action needed.');
        process.exit(0);
      }

      const spinner = ui.startSpinner('Checking for updates...');
      let latest: string;
      try {
        latest = await fetchLatestVersion();
      } catch (err) {
        spinner.error('Failed to check for updates');
        ui.error((err as Error).message || String(err));
        process.exit(1);
        return;
      }

      if (!isNewerVersion(currentVersion, latest)) {
        spinner.success(`Already up to date (v${currentVersion})`);
        process.exit(0);
      }

      spinner.success(formatUpdateAvailable(currentVersion, latest));
      console.log();

      // --check mode: exit 2 = outdated (distinct from exit 1 = error)
      if (options.check) {
        process.exit(2);
      }

      ui.info(`Installing ${PACKAGE_NAME}@${latest}...`);
      console.log();

      try {
        const exitCode = await runNpmInstall(`${PACKAGE_NAME}@${latest}`);

        if (exitCode !== 0) {
          console.log();
          ui.error('Update failed.');
          if (platform() === 'win32') {
            ui.info('Try running the terminal as Administrator.');
          } else {
            ui.info(`Try: sudo npm install -g ${PACKAGE_NAME}@latest`);
          }
          process.exit(1);
        }
      } catch (err) {
        console.log();
        ui.error(`Could not run npm: ${(err as Error).message}`);
        ui.info('Make sure npm is installed and in your PATH.');
        process.exit(1);
      }

      console.log();
      ui.success(`Updated to v${latest}`);
    });
}
