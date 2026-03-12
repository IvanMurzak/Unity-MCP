import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor } from '../utils/unity-editor.js';

export const openCommand = new Command('open')
  .description('Open a Unity project in Unity Editor')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--unity-version <version>', 'Specific Unity Editor version to use')
  .action(async (positionalPath: string | undefined, options: { path?: string; unityVersion?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      console.error('Error: Path is required. Usage: unity-mcp open <path> or --path <path>');
      process.exit(1);
    }
    const projectPath = path.resolve(resolvedPath);

    if (!fs.existsSync(projectPath)) {
      console.error(`Error: Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    // Determine editor version
    let version = options.unityVersion;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        console.log(`Detected editor version from project: ${version}`);
      }
    }

    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      const versionMsg = version ? ` (version ${version})` : '';
      console.error(`Error: Unity Editor not found${versionMsg}. Install it with: unity-mcp install-editor --version <version>`);
      process.exit(1);
    }

    console.log(`Opening project: ${projectPath}`);
    console.log(`Using editor: ${editorPath}`);
    launchEditor(editorPath, projectPath);
  });
