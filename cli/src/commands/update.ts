import { Command } from 'commander';
import { spawn } from 'child_process';
import { platform } from 'os';
import { resolveInstallTarget, unityAdapter } from '@baizor/gamedev-cli-core';
import { findUnityProjectRoot, updateProject, type ProjectUpdateItem } from '../lib/update-project.js';
import * as ui from '../utils/ui.js';
import { fetchLatestVersion, formatUpdateAvailable, isRunningViaNpx } from '../utils/update-check.js';
import { isNewerVersion } from '../utils/semver.js';

const PACKAGE_NAME = 'unity-mcp-cli';

/** Exit 2 with `--check`: something is outdated (distinct from exit 1 = error). */
const EXIT_OUTDATED = 2;

interface UpdateOptions {
  path?: string;
  check?: boolean;
  /** Commander maps `--no-self`, `--no-project`, `--no-extensions` to these being `false`. */
  self?: boolean;
  project?: boolean;
  extensions?: boolean;
}

function npmCommand(): string {
  return platform() === 'win32' ? 'npm.cmd' : 'npm';
}

function runNpmInstall(packageSpec: string): Promise<number> {
  return new Promise((resolve, reject) => {
    // Node refuses to spawn a `.cmd` without a shell (EINVAL). The version in the spec is checked first.
    const child = spawn(npmCommand(), ['install', '-g', packageSpec], {
      stdio: 'inherit',
      shell: platform() === 'win32',
    });

    child.on('error', reject);
    child.on('close', (code) => resolve(code ?? 1));
  });
}

function printItem(item: ProjectUpdateItem): void {
  const value =
    item.status === 'outdated' ? `${item.from} → ${item.to}`
    : item.status === 'local' ? `${item.from} (local install, left as is)`
    : item.status === 'unknown' ? `${item.from} (latest version unknown)`
    : `${item.from} (up to date)`;
  ui.label(item.name, value);
}

type Outcome = 'done' | 'outdated' | 'failed';

async function updateProjectStep(projectPath: string, options: UpdateOptions): Promise<Outcome> {
  ui.heading(`Project ${projectPath}`);
  const spinner = ui.startSpinner(options.check ? 'Checking the plugin and extensions...' : 'Updating the plugin and extensions...');
  const result = await updateProject({
    unityProjectPath: projectPath,
    noExtensions: options.extensions === false,
    dryRun: options.check,
    onProgress: (event) => {
      spinner.text = event.message;
    },
  });
  if (result.kind === 'failure') {
    spinner.error(result.error.message);
    result.warnings.forEach((warning) => ui.warn(warning));
    return 'failed';
  }

  const outdated = result.items.filter((item) => item.status === 'outdated');
  spinner.success(options.check
    ? `${outdated.length} update(s) available`
    : `${result.updated.length} updated`);
  result.items.forEach(printItem);
  result.warnings.forEach((warning) => ui.warn(warning));
  for (const step of result.nextSteps) ui.info(step);

  if (result.items.length === 0) ui.info('The MCP plugin is not installed here: nothing to update in this project.');
  if (options.check) return outdated.length > 0 ? 'outdated' : 'done';
  return result.updated.length < outdated.length ? 'failed' : 'done';
}

async function updateSelfStep(currentVersion: string, check: boolean | undefined): Promise<Outcome> {
  if (isRunningViaNpx()) {
    ui.info('Running via npx — it always fetches the latest published version.');
    return 'done';
  }

  const spinner = ui.startSpinner(`Checking for a newer ${PACKAGE_NAME}...`);
  let latest: string;
  try {
    latest = await fetchLatestVersion();
  } catch (err) {
    spinner.error('Failed to check for updates');
    ui.error((err as Error).message || String(err));
    return 'failed';
  }

  if (!isNewerVersion(currentVersion, latest)) {
    spinner.success(`${PACKAGE_NAME} is up to date (v${currentVersion})`);
    return 'done';
  }

  if (!/^\d+\.\d+\.\d+[0-9A-Za-z.+-]*$/.test(latest)) {
    spinner.error(`The registry returned an unexpected version: ${latest}`);
    return 'failed';
  }

  spinner.success(formatUpdateAvailable(currentVersion, latest));
  console.log();
  if (check) return 'outdated';

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
      return 'failed';
    }
  } catch (err) {
    console.log();
    ui.error(`Could not run npm: ${(err as Error).message}`);
    ui.info('Make sure npm is installed and in your PATH.');
    return 'failed';
  }

  console.log();
  ui.success(`Updated ${PACKAGE_NAME} to v${latest}`);
  return 'done';
}

/** The project to update, or null when there is none around the current folder. A named path that is not one exits 1. */
function resolveProject(positional: string | undefined, explicit: string | undefined): string | null {
  if (positional === undefined && explicit === undefined) return findUnityProjectRoot(process.cwd());

  const target = resolveInstallTarget({ adapter: unityAdapter, positional, path: explicit });
  if (target.kind === 'failure') {
    ui.error(target.error.message);
    process.exit(1);
  }
  return target.projectRoot;
}

export function createUpdateCommand(currentVersion: string): Command {
  return new Command('update')
    .description(`Update everything: the Unity project's MCP plugin and installed extensions, then ${PACKAGE_NAME} itself`)
    .argument('[path]', 'Unity project path (defaults to the project around the current directory)')
    .option('--path <path>', 'Unity project path')
    .option('--check', 'Only report what would change. Exits 2 when something is outdated')
    .option('--no-self', `Leave ${PACKAGE_NAME} itself as it is`)
    .option('--no-project', 'Leave the project as it is: update only the CLI')
    .option('--no-extensions', 'Leave the installed extensions as they are')
    .action(async (positionalPath: string | undefined, options: UpdateOptions) => {
      const outcomes: Outcome[] = [];

      if (options.project !== false) {
        const projectPath = resolveProject(positionalPath, options.path);
        if (projectPath === null) {
          ui.info('No Unity project here, so only the CLI is updated. Pass a project path to update its plugin and extensions.');
        } else {
          outcomes.push(await updateProjectStep(projectPath, options));
        }
      }

      // Last: npm replaces this very process's files, and the project step does not need a newer CLI
      // because it looks the latest versions up itself.
      if (options.self !== false) outcomes.push(await updateSelfStep(currentVersion, options.check));

      process.exit(outcomes.includes('failed') ? 1 : outcomes.includes('outdated') ? EXIT_OUTDATED : 0);
    });
}
