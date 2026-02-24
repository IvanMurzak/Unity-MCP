import path from 'path';
import { openUnityProject } from '../utils/unity.js';

/**
 * Registers the `open` command with the CLI program.
 *
 * Usage:
 *   gamedev open <project-path> [options]
 *
 * @param {import('commander').Command} program
 */
export function registerOpenCommand(program) {
  program
    .command('open <project-path>')
    .description('Open a Unity project in the Unity Editor')
    .option('--unity-path <path>', 'Explicit path to the Unity executable (auto-detected if omitted)')
    .option('--unity-version <version>', 'Unity version to use when multiple versions are installed (e.g. 2022.3.62f1)')
    .action(async (projectPath, options) => {
      const absPath = path.resolve(projectPath);
      console.log(`Opening Unity project: ${absPath}`);

      try {
        await openUnityProject(absPath, {
          unityPath: options.unityPath,
          unityVersion: options.unityVersion,
        });
      } catch (err) {
        console.error(`Error: ${err.message}`);
        process.exit(1);
      }
    });
}
