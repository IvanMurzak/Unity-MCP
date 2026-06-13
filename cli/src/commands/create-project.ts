import { Command } from 'commander';
import { ensureUnityHub } from '../utils/unity-hub.js';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { createProject } from '../lib/create-project.js';

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

    // Hub bootstrap stays on the CLI surface: `ensureUnityHub` may
    // download + install Unity Hub (spinners, possible admin prompt),
    // which the library deliberately never does. The resolved path is
    // handed to the library so discovery is not repeated.
    const hubSpinner = ui.startSpinner('Locating Unity Hub...');
    let hubPath: string;
    try {
      hubPath = await ensureUnityHub();
    } catch (err) {
      hubSpinner.error('Failed to locate Unity Hub');
      throw err;
    }
    hubSpinner.success('Unity Hub located');
    verbose(`Unity Hub path: ${hubPath}`);

    let createSpinner: ReturnType<typeof ui.startSpinner> | undefined;

    // All project-creation logic lives in the shared library function;
    // this command only renders progress and maps the result onto the
    // historical CLI output / exit codes.
    const result = await createProject({
      projectPath: resolvedPath,
      editorVersion: options.unity,
      hubPath,
      onProgress: (event) => {
        switch (event.phase) {
          case 'editors-located': {
            // The Hub query is synchronous (it has already returned by the
            // time this fires), so emit the result line directly to keep
            // parity with the old "Found N installed editors" output rather
            // than leaving the user with no feedback during the query.
            ui.info(event.message);
            break;
          }
          case 'editor-resolved': {
            if (!options.unity && event.version) {
              ui.info(`No Unity version specified, using highest installed: ${event.version}`);
            }
            verbose(`Unity Editor executable: ${event.editorPath}`);
            break;
          }
          case 'creating-project': {
            ui.info(`Creating Unity project at: ${event.projectPath}`);
            ui.label('Unity Editor', `${event.version} (${event.editorPath})`);
            createSpinner = ui.startSpinner('Creating project...');
            break;
          }
          default:
            break;
        }
      },
    });

    if (result.kind === 'failure') {
      if (createSpinner) {
        createSpinner.error('Project creation failed');
        createSpinner = undefined;
      }
      ui.error(result.errorMessage);
      process.exit(1);
    }

    if (createSpinner) {
      createSpinner.stop();
      createSpinner = undefined;
    }
    ui.success('Project created successfully.');
  });
