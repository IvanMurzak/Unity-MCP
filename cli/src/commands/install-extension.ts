import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { installExtension } from '../lib/install-extension.js';
import { EXTENSIONS_CATALOG } from '../utils/extensions-catalog.js';
import { resolveInstallTarget, unityAdapter } from '@baizor/gamedev-cli-core';

interface InstallExtensionCliOptions {
  path?: string;
  extensionVersion?: string;
  list?: boolean;
}

export const installExtensionCommand = new Command('install-extension')
  .description(
    'Install a Unity-MCP extension into a Unity project: resolve the extension <id> from the shared catalogue, add (or update) its dependency plus the OpenUPM scoped registry in Packages/manifest.json, then let Unity resolve it. Idempotent. The project path defaults to the current directory (like install-plugin).',
  )
  .argument('[id]', 'Extension to install (the OpenUPM package id, or the catalogue name e.g. "Tilemap")')
  .argument('[path]', 'Path to the Unity project (defaults to the current directory)')
  .option('--path <path>', 'Path to the Unity project')
  // NOT `--version`: the root program registers `-V, --version` via `.version()`, which
  // intercepts it before this subcommand sees it — `install-extension X --version 1.2.3`
  // would print the CLI's own version and exit 0 without installing anything. This is the
  // same reason `install-plugin` spells its flag `--plugin-version`.
  .option('--extension-version <version>', 'Extension version to install (default: latest from OpenUPM)')
  .option('--list', 'List the installable extensions and exit')
  .action(async (id: string | undefined, positionalPath: string | undefined, options: InstallExtensionCliOptions) => {
    // `--list` is a pure read: no project needed, no network, exit 0.
    if (options.list) {
      ui.heading('Installable Unity-MCP extensions');
      for (const ext of EXTENSIONS_CATALOG) {
        ui.label(ext.name, ext.packageId);
      }
      ui.info(
        `${EXTENSIONS_CATALOG.length} extensions. Install one with: unity-mcp-cli install-extension <id> [path]`,
      );
      return;
    }

    if (!id || id.trim() === '') {
      ui.error(
        'Missing extension id. Pass one (e.g. `unity-mcp-cli install-extension Tilemap`), or run with --list to see what is installable.',
      );
      process.exit(1);
    }

    // Same path resolution as install-plugin: `path? → --path? → cwd`, then verify the
    // directory really is a Unity project so the error names what was checked.
    const target = resolveInstallTarget({
      adapter: unityAdapter,
      positional: positionalPath,
      path: options.path,
    });
    if (target.kind === 'failure') {
      ui.error(target.error.message);
      process.exit(1);
    }

    const projectPath = target.projectRoot;

    ui.heading('Installing Unity-MCP extension');
    verbose(`Extension id: ${id}`);
    verbose(`Project path: ${projectPath}`);
    if (options.extensionVersion) verbose(`--extension-version: ${options.extensionVersion}`);

    let spinner: ReturnType<typeof ui.startSpinner> | undefined;
    if (!options.extensionVersion) {
      spinner = ui.startSpinner('Resolving latest extension version...');
    }

    const result = await installExtension({
      unityProjectPath: projectPath,
      extensionId: id,
      version: options.extensionVersion,
      onProgress: (event) => {
        if (event.phase === 'dependencies-resolved' && spinner) {
          spinner.success(`Resolved extension version: ${event.version}`);
          spinner = undefined;
          return;
        }
        if (event.phase === 'manifest-patched') {
          verbose(event.message);
        }
      },
    });

    if (result.kind === 'failure') {
      if (spinner) {
        spinner.error('Failed to resolve extension version');
        spinner = undefined;
      }
      ui.error(result.error.message);
      for (const warning of result.warnings) {
        ui.warn(warning);
      }
      process.exit(1);
    }

    switch (result.outcome) {
      case 'added':
        ui.success(`Installed ${result.packageId} ${result.toVersion}`);
        break;
      case 'updated':
        ui.success(`Updated ${result.packageId} ${result.fromVersion} → ${result.toVersion}`);
        break;
      case 'already-up-to-date':
        ui.info(`${result.packageId} is already up to date (${result.toVersion})`);
        break;
    }

    ui.label('Status', result.message);
    ui.label('Manifest', result.manifestPath);

    for (const warning of result.warnings) {
      ui.warn(warning);
    }
    for (const step of result.nextSteps) {
      ui.label('Next step', step);
    }
  });
