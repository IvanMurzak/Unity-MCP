import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { ensureUnityHub, installEditor, listInstalledEditors } from '../utils/unity-hub.js';
import { getProjectEditorVersion } from '../utils/unity-editor.js';
import * as ui from '../utils/ui.js';

export const installUnityCommand = new Command('install-unity')
  .description('Install Unity Editor via Unity Hub')
  .option('--version <version>', 'Unity Editor version to install')
  .option('--path <path>', 'Read version from an existing Unity project')
  .action(async (options: { version?: string; path?: string }) => {
    const spinner = ui.startSpinner('Locating Unity Hub...');
    let hubPath: string;
    try {
      hubPath = await ensureUnityHub();
    } catch (err) {
      spinner.error('Failed to locate Unity Hub');
      throw err;
    }
    spinner.success('Unity Hub located');

    let version = options.version;

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

    if (!version) {
      ui.error('Please specify --version or --path');

      const editors = listInstalledEditors(hubPath);
      if (editors.length > 0) {
        ui.heading('Currently installed editors:');
        for (const editor of editors) {
          ui.label(editor.version, editor.path);
        }
      }

      process.exit(1);
    }

    // Check if already installed
    const editors = listInstalledEditors(hubPath);
    const alreadyInstalled = editors.find((e) => e.version === version);
    if (alreadyInstalled) {
      ui.success(`Unity Editor ${version} is already installed at: ${alreadyInstalled.path}`);
      return;
    }

    installEditor(hubPath, version);
    ui.success(`Unity Editor ${version} installed successfully`);
  });
