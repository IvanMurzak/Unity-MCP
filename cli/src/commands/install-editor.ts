import { Command } from 'commander';
import { ensureUnityHub, installEditor, listInstalledEditors } from '../utils/unity-hub.js';
import { getProjectEditorVersion } from '../utils/unity-editor.js';

export const installEditorCommand = new Command('install-editor')
  .description('Install Unity Editor via Unity Hub')
  .option('--version <version>', 'Unity Editor version to install')
  .option('--path <path>', 'Read version from an existing Unity project')
  .action(async (options: { version?: string; path?: string }) => {
    const hubPath = await ensureUnityHub();

    let version = options.version;

    if (!version && options.path) {
      version = getProjectEditorVersion(options.path) ?? undefined;
      if (version) {
        console.log(`Detected editor version from project: ${version}`);
      } else {
        console.error('Error: Could not read editor version from ProjectSettings/ProjectVersion.txt');
        process.exit(1);
      }
    }

    if (!version) {
      console.error('Error: Please specify --version or --path');

      // Show installed editors as a hint
      const editors = listInstalledEditors(hubPath);
      if (editors.length > 0) {
        console.log('\nCurrently installed editors:');
        for (const editor of editors) {
          console.log(`  ${editor.version} - ${editor.path}`);
        }
      }

      process.exit(1);
    }

    // Check if already installed
    const editors = listInstalledEditors(hubPath);
    const alreadyInstalled = editors.find((e) => e.version === version);
    if (alreadyInstalled) {
      console.log(`Unity Editor ${version} is already installed at: ${alreadyInstalled.path}`);
      return;
    }

    installEditor(hubPath, version);
  });
