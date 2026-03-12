import { Command } from 'commander';
import * as path from 'path';
import { findUnityHub, createProject, listInstalledEditors } from '../utils/unity-hub.js';

export const createProjectCommand = new Command('create-project')
  .description('Create a new Unity project')
  .requiredOption('--path <path>', 'Path where the project will be created')
  .option('--editor-version <version>', 'Unity Editor version to use')
  .action(async (options: { path: string; editorVersion?: string }) => {
    const hubPath = findUnityHub();
    if (!hubPath) {
      console.error('Error: Unity Hub not found. Please install Unity Hub first.');
      process.exit(1);
    }

    let editorVersion = options.editorVersion;

    if (!editorVersion) {
      // Use the latest installed editor
      const editors = listInstalledEditors(hubPath);
      if (editors.length === 0) {
        console.error('Error: No Unity editors installed. Install one with: unity-mcp install-editor --version <version>');
        process.exit(1);
      }
      editorVersion = editors[0].version;
      console.log(`No editor version specified, using latest installed: ${editorVersion}`);
    }

    const projectPath = path.resolve(options.path);
    createProject(hubPath, projectPath, editorVersion);
  });
