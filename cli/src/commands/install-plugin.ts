import { Command } from 'commander';
import * as fs from 'fs';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { installPlugin } from '../lib/install-plugin.js';
import { downloadServerBinary } from '../utils/managed-server.js';
import { DEFAULT_SERVER_VERSION } from '../utils/server-version.js';
import { resolveEnrollCode, runEnroll, EnrollmentError } from '../utils/enroll.js';
import { MachineCredentialStore } from '../utils/machine-credentials.js';
import { resolveInstallTarget, unityAdapter } from '@baizor/gamedev-cli-core';

interface InstallPluginOptions {
  path?: string;
  pluginVersion?: string;
  withServer?: boolean;
  serverVersion?: string;
  serverSource?: string;
  enroll?: string;
  enrollStdin?: boolean;
}

export const installPluginCommand = new Command('install-plugin')
  .description('Install Unity-MCP plugin into a Unity project (optionally download the server + enroll)')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--plugin-version <version>', 'Plugin version to install (default: latest)')
  .option('--with-server', 'Also download the RID-matched GameDev-MCP-Server binary into the CLI managed dir')
  .option('--server-version <version>', `Server version to download with --with-server (default: ${DEFAULT_SERVER_VERSION})`)
  .option('--server-source <path-or-url>', 'Offline/CI override: install the server from a local zip path or URL (skips SHA256SUMS verification)')
  .option('--enroll <code>', 'Redeem an enrollment code for a plugin credential (planted in the shared machine store)')
  .option('--enroll-stdin', 'Read the enrollment code from stdin (never argv/shell history)')
  .action(async (positionalPath: string | undefined, options: InstallPluginOptions) => {
    // T5/B1: path is OPTIONAL — resolve `path? → --path? → cwd`, then verify the directory is a real
    // Unity project (marker probe for `Packages/manifest.json`). On a miss the error lists exactly
    // what was checked, so `install-plugin` "just works" from a project folder with no path.
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

    // ── Phase 1: install the plugin into the Unity project's manifest ────────
    // Wire the library's progress events back into the CLI's chalk-
    // styled ui so the terminal experience stays identical.
    let spinner: ReturnType<typeof ui.startSpinner> | undefined;
    if (!options.pluginVersion) {
      spinner = ui.startSpinner('Resolving latest plugin version...');
    }

    const result = await installPlugin({
      unityProjectPath: projectPath,
      version: options.pluginVersion,
      onProgress: (event) => {
        if (event.phase === 'dependencies-resolved' && spinner) {
          spinner.success(`Resolved plugin version: ${event.version}`);
          spinner = undefined;
          return;
        }
        if (event.phase === 'manifest-patched') {
          // Preserve the historical `ui.success("Updated …")` vs
          // `ui.info("manifest.json is already up to date.")` split
          // that the shared manifest helpers used to print directly.
          if (event.message.startsWith('Updated ')) {
            ui.success(event.message);
          } else {
            ui.info(event.message);
          }
        }
      },
    });

    if (result.kind === 'failure') {
      if (spinner) {
        spinner.error('Failed to resolve plugin version');
        spinner = undefined;
      }
      ui.error(result.error.message);
      process.exit(1);
    }

    // Narrowed: result.kind === 'success' below — `installedVersion`
    // and `manifestPath` are non-optional.
    verbose(`Plugin version: ${result.installedVersion} (explicit: ${!!options.pluginVersion})`);
    verbose(`Manifest path: ${result.manifestPath}`);
    ui.info(`Installing Unity-MCP plugin v${result.installedVersion} into: ${projectPath}`);

    for (const warning of result.warnings) {
      ui.warn(warning);
    }

    // ── Phase 2: download the RID-matched server binary (--with-server) ──────
    if (options.withServer) {
      const serverSpinner = ui.startSpinner('Downloading GameDev-MCP-Server binary...');
      try {
        const download = await downloadServerBinary({
          version: options.serverVersion ?? DEFAULT_SERVER_VERSION,
          source: options.serverSource,
          onProgress: (msg) => {
            serverSpinner.text = msg;
            verbose(msg);
          },
        });
        serverSpinner.success(
          `Server ${download.rid} v${download.version} installed${download.verified ? ' (checksum verified)' : ''}`,
        );
        ui.label('Server binary', download.binaryPath);
      } catch (err) {
        serverSpinner.error('Server download failed');
        ui.error(err instanceof Error ? err.message : String(err));
        process.exit(1);
      }
    }

    // ── Phase 3: redeem an enrollment code (--enroll / --enroll-stdin) ───────
    if (options.enroll || options.enrollStdin) {
      let code: string;
      try {
        code = resolveEnrollCode(
          { enroll: options.enroll, enrollStdin: options.enrollStdin },
          () => fs.readFileSync(0, 'utf-8'),
        );
      } catch (err) {
        ui.error(err instanceof Error ? err.message : String(err));
        process.exit(1);
      }

      const enrollSpinner = ui.startSpinner('Redeeming enrollment code...');
      try {
        const enrolled = await runEnroll({
          code,
          projectPath,
          adapter: unityAdapter,
          store: new MachineCredentialStore(),
        });
        enrollSpinner.success('Enrollment complete');
        ui.label('Credential', enrolled.credentialPath);
        ui.label('Server target', enrolled.serverTarget);
        ui.label('Project marker', enrolled.markerPath);
        ui.label('Project pin', enrolled.pin);
        if (enrolled.pinnedConfigs.length > 0) {
          for (const cfg of enrolled.pinnedConfigs) {
            ui.info(`Pinned project routing in ${cfg}`);
          }
        }
      } catch (err) {
        enrollSpinner.error('Enrollment failed');
        ui.error(err instanceof EnrollmentError || err instanceof Error ? err.message : String(err));
        process.exit(1);
      }
    }

    ui.success('Done! Open the project in Unity Editor to complete installation.');
  });
