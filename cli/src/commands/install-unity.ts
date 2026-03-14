import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { ensureUnityHub, installEditor, listInstalledEditors, listAvailableReleases, findLatestStableRelease } from '../utils/unity-hub.js';
import { getProjectEditorVersion } from '../utils/unity-editor.js';
import * as ui from '../utils/ui.js';

export const installUnityCommand = new Command('install-unity')
  .description('Install Unity Editor via Unity Hub')
  .argument('[version]', 'Unity Editor version to install (e.g. 6000.3.1f1). Omit to install latest stable release.')
  .option('--path <path>', 'Read version from an existing Unity project')
  .action(async (positionalVersion: string | undefined, options: { path?: string }) => {
    const spinner = ui.startSpinner('Locating Unity Hub...');
    let hubPath: string;
    try {
      hubPath = await ensureUnityHub();
    } catch (err) {
      spinner.error('Failed to locate Unity Hub');
      throw err;
    }
    spinner.success('Unity Hub located');

    let version = positionalVersion;

    if (!version && options.path) {
      const projectPath = path.resolve(options.path);
      if (!fs.existsSync(projectPath)) {
        ui.error(`Project path does not exist: ${projectPath}`);
        process.exit(1);
      }
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        ui.info(`Detected editor version from project: ${version}`);
      } else {
        ui.error('Could not read editor version from ProjectSettings/ProjectVersion.txt');
        process.exit(1);
      }
    }

    // Fetch available releases once — reused for latest-stable lookup and install
    let releases: ReturnType<typeof listAvailableReleases> | undefined;

    // No version specified — resolve latest stable release from Unity Hub
    if (!version) {
      releases = listAvailableReleases(hubPath);
      const latest = findLatestStableRelease(releases);

      if (!latest) {
        ui.error('No stable releases found');
        const editors = listInstalledEditors(hubPath);
        if (editors.length > 0) {
          ui.heading('Currently installed editors:');
          for (const editor of editors) {
            ui.label(editor.version, editor.path);
          }
        }
        process.exit(1);
      }

      ui.success(`Latest stable release: ${latest.version}`);
      version = latest.version;
    }

    // Check if already installed
    const editors = listInstalledEditors(hubPath);
    const alreadyInstalled = editors.find((e) => e.version === version);
    if (alreadyInstalled) {
      ui.success(`Unity Editor ${version} is already installed at: ${alreadyInstalled.path}`);
      return;
    }

    await installEditor(hubPath, version, releases);
    ui.success(`Unity Editor ${version} installed successfully`);
  });
