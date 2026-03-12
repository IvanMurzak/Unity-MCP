import { Command } from 'commander';
import * as path from 'path';
import { ensureUnityHub, createProject, listInstalledEditors, findHighestEditor } from '../utils/unity-hub.js';

export const createProjectCommand = new Command('create-project')
  .description('Create a new Unity project')
  .argument('[path]', 'Path where the project will be created')
  .option('--path <path>', 'Path where the project will be created')
  .option('--unity <version>', 'Unity Editor version to use')
  .action(async (positionalPath: string | undefined, options: { path?: string; unity?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      console.error('Error: Path is required. Usage: unity-mcp-cli create-project <path> or --path <path>');
      process.exit(1);
    }
    const hubPath = await ensureUnityHub();

    let editorVersion = options.unity;

    if (!editorVersion) {
      // Use the highest installed editor version
      const editors = listInstalledEditors(hubPath);
      if (editors.length === 0) {
        console.error('Error: No Unity editors installed. Install one with: unity-mcp-cli install-editor --version <version>');
        process.exit(1);
      }
      const highest = findHighestEditor(editors);
      editorVersion = highest.version;
      console.log(`No Unity version specified, using highest installed: ${editorVersion}`);
    }

    const projectPath = path.resolve(resolvedPath);
    createProject(hubPath, projectPath, editorVersion);
  });
