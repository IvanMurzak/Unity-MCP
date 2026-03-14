import { Command } from 'commander';
import * as path from 'path';
import { ensureUnityHub, createProject, listInstalledEditors, findHighestEditor } from '../utils/unity-hub.js';
import * as ui from '../utils/ui.js';

export const createProjectCommand = new Command('create-project')
  .description('Create a new Unity project')
  .argument('[path]', 'Path where the project will be created')
  .option('--path <path>', 'Path where the project will be created')
  .option('--unity <version>', 'Unity Editor version to use')
  .action(async (positionalPath: string | undefined, options: { path?: string; unity?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli create-project <path> or --path <path>');
      process.exit(1);
    }

    const spinner = ui.startSpinner('Locating Unity Hub...');
    let hubPath: string;
    try {
      hubPath = await ensureUnityHub();
    } catch (err) {
      spinner.error('Failed to locate Unity Hub');
      throw err;
    }
    spinner.success('Unity Hub located');

    let editorVersion = options.unity;

    if (!editorVersion) {
      const editors = listInstalledEditors(hubPath);
      if (editors.length === 0) {
        ui.error('No Unity editors installed. Install one with: unity-mcp-cli install-unity <version>');
        process.exit(1);
      }
      const highest = findHighestEditor(editors);
      editorVersion = highest.version;
      ui.info(`No Unity version specified, using highest installed: ${editorVersion}`);
    }

    const projectPath = path.resolve(resolvedPath);
    createProject(hubPath, projectPath, editorVersion);
  });
