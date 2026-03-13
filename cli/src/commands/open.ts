import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor } from '../utils/unity-editor.js';
import * as ui from '../utils/ui.js';

export const openCommand = new Command('open')
  .description('Open a Unity project in Unity Editor')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--unity <version>', 'Specific Unity Editor version to use')
  .action(async (positionalPath: string | undefined, options: { path?: string; unity?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli open <path> or --path <path>');
      process.exit(1);
    }
    const projectPath = path.resolve(resolvedPath);

    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    // Determine editor version
    let version = options.unity;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        ui.info(`Detected editor version from project: ${version}`);
      }
    }

    const spinner = ui.startSpinner('Locating Unity Editor...');
    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      spinner.fail('Unity Editor not found');
      const versionMsg = version ? ` (version ${version})` : '';
      ui.error(`Unity Editor not found${versionMsg}. Install it with: unity-mcp-cli install-editor --version <version>`);
      process.exit(1);
    }
    spinner.succeed('Unity Editor located');

    ui.label('Project', projectPath);
    ui.label('Editor', editorPath);
    launchEditor(editorPath, projectPath);
    ui.success('Unity Editor launched');
  });
