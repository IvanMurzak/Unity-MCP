import { Command } from 'commander';
import * as path from 'path';
import { findUnityHub, listInstalledEditors } from '../utils/unity-hub.js';
import { findUnityProcess } from '../utils/unity-process.js';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import {
  openProject,
  resolveProjectPath as libResolveProjectPath,
  isUnityProjectDir as libIsUnityProjectDir,
} from '../lib/open.js';
import type { OpenProjectAuthOption, OpenProjectTransport } from '../lib/types.js';

/**
 * Resolve the project path from the positional argument, `--path` option, or
 * the current working directory when neither is provided.
 *
 * Re-exported from the library helper so existing tests continue to import
 * `resolveOpenProjectPath` from this module.
 */
export interface ResolveProjectPathResult {
  /** Absolute, resolved path to the project directory. */
  projectPath: string;
  /** True if no path was supplied and we fell back to `process.cwd()`. */
  usedCwdFallback: boolean;
}

export function resolveOpenProjectPath(
  positionalPath: string | undefined,
  optionPath: string | undefined,
  cwd: string,
): ResolveProjectPathResult {
  // The CLI takes both a positional `[path]` AND an `--path` flag; the
  // library helper only takes a single `optionPath`. Collapse the two
  // CLI inputs (positional wins) before delegating.
  const explicit = positionalPath ?? optionPath;
  return libResolveProjectPath(explicit, cwd);
}

/** Re-export for backward compatibility with existing tests. */
export const isUnityProjectDir = libIsUnityProjectDir;

/**
 * Print actionable help when a required Unity Editor version is not found.
 * Lists installed editors and suggests install or override commands. Lives
 * here (next to the CLI command) instead of `unity-editor.ts` because it
 * writes to the chalk-styled `ui` and we want to keep the `unity-editor.ts`
 * helpers library-safe.
 */
function printEditorNotFoundHelp(requestedVersion: string | undefined, commandName: string): void {
  if (requestedVersion) {
    ui.error(`Unity Editor ${requestedVersion} is not installed.`);
  } else {
    ui.error('No Unity Editor found.');
  }

  ui.heading('Options:');

  if (requestedVersion) {
    ui.info(`Install it:  npx unity-mcp-cli install-unity ${requestedVersion}`);
  }
  ui.info('Install latest stable:  npx unity-mcp-cli install-unity');

  const hubPath = findUnityHub();
  if (hubPath) {
    const editors = listInstalledEditors(hubPath);
    if (editors.length > 0) {
      ui.heading('Installed editors:');
      for (const editor of editors) {
        ui.label(editor.version, editor.path);
      }
      if (requestedVersion) {
        const hint = commandName === 'connect'
          ? `npx unity-mcp-cli ${commandName} --unity ${editors[0].version} --path <path> --url <url>`
          : `npx unity-mcp-cli ${commandName} <path> --unity ${editors[0].version}`;
        ui.info(`Use a different version:  ${hint}`);
      }
    }
  }
}

export const openCommand = new Command('open')
  .description('Open a Unity project in Unity Editor, optionally passing MCP connection env vars when connection options (--url, --token, etc.) are provided. Use --no-connect to suppress all MCP env vars.')
  .argument('[path]', 'Path to the Unity project (defaults to current directory)')
  .option('--path <path>', 'Path to the Unity project (defaults to current directory)')
  .option('--unity <version>', 'Specific Unity Editor version to use')
  .option('--no-connect', 'Open without MCP connection environment variables')
  .option('--url <url>', 'MCP server URL to connect to (sets UNITY_MCP_HOST)')
  .option('--tools <names>', 'Comma-separated list of tools to enable (sets UNITY_MCP_TOOLS)')
  .option('--token <token>', 'Auth token (sets UNITY_MCP_TOKEN)')
  .option('--auth <option>', 'Auth option: none or required (sets UNITY_MCP_AUTH_OPTION)')
  .option('--keep-connected', 'Force keep connected (sets UNITY_MCP_KEEP_CONNECTED=true)')
  .option('--transport <method>', 'Transport method: streamableHttp or stdio (sets UNITY_MCP_TRANSPORT)')
  .option('--start-server <value>', 'Set to true/false to control server auto-start (sets UNITY_MCP_START_SERVER)', undefined)
  .action(async (positionalPath: string | undefined, options: {
    path?: string;
    unity?: string;
    connect?: boolean;
    url?: string;
    tools?: string;
    token?: string;
    auth?: string;
    keepConnected?: boolean;
    transport?: string;
    startServer?: string;
  }) => {
    // Resolve the path the same way the library does, but also
    // validate the existence + Unity-project shape up front so we
    // can emit the historical, friendlier "Current directory is
    // not a Unity project" message when the cwd-fallback was used.
    // The library always reports the underlying error string, but
    // the CLI is allowed to render two different variants.
    const { projectPath, usedCwdFallback } = resolveOpenProjectPath(
      positionalPath,
      options.path,
      process.cwd(),
    );

    const fs = await import('fs');
    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    if (!libIsUnityProjectDir(projectPath)) {
      if (usedCwdFallback) {
        ui.error(`Current directory is not a Unity project: ${projectPath}`);
        ui.info('Run this command from a Unity project folder, or pass a path: unity-mcp-cli open <path>');
      } else {
        ui.error(`Not a Unity project (missing Assets/ or ProjectSettings/ProjectVersion.txt): ${projectPath}`);
      }
      process.exit(1);
    }

    if (usedCwdFallback) {
      verbose(`No path provided — using current directory: ${projectPath}`);
    }
    verbose(`open invoked for project: ${projectPath}`);
    verbose(`--no-connect: ${options.connect === false}`);

    // Validate auth/transport/startServer here — keeps the historical
    // CLI error messages and exit codes (the library reports these
    // as `kind: 'failure'` instead).
    if (options.auth !== undefined && options.auth !== 'none' && options.auth !== 'required') {
      ui.error('--auth must be "none" or "required"');
      process.exit(1);
    }
    if (options.transport !== undefined && options.transport !== 'streamableHttp' && options.transport !== 'stdio') {
      ui.error('--transport must be "streamableHttp" or "stdio"');
      process.exit(1);
    }
    let startServerBool: boolean | undefined;
    if (options.startServer !== undefined) {
      const val = options.startServer.toLowerCase();
      if (val !== 'true' && val !== 'false') {
        ui.error('--start-server must be "true" or "false"');
        process.exit(1);
      }
      startServerBool = val === 'true';
    }

    // Pre-flight already-running check so we don't flash the
    // "Locating Unity Editor..." spinner when Unity is already open
    // for this project. The lib does its own check too (single
    // source of truth for the result shape); this one only suppresses
    // spinner spam in the common already-open case.
    const alreadyRunningPid = findUnityProcess(projectPath)?.pid;

    // Spinner around editor location for parity with the legacy UX.
    let spinner: ReturnType<typeof ui.startSpinner> | undefined =
      alreadyRunningPid === undefined
        ? ui.startSpinner('Locating Unity Editor...')
        : undefined;

    const result = await openProject({
      projectPath,
      unityVersion: options.unity,
      noConnect: options.connect === false,
      url: options.url,
      token: options.token,
      auth: options.auth as OpenProjectAuthOption | undefined,
      tools: options.tools,
      keepConnected: options.keepConnected,
      transport: options.transport as OpenProjectTransport | undefined,
      startServer: startServerBool,
      onProgress: (event) => {
        switch (event.phase) {
          case 'detecting-editor-version': {
            // unity-version detection is fast — no UI noise needed.
            break;
          }
          case 'editors-located': {
            // Cover the case where editor discovery succeeded — the
            // spinner success message lands on `editor-resolved`,
            // not here, so we don't overwrite the "Locating…" line.
            break;
          }
          case 'editor-resolved': {
            if (spinner) {
              spinner.success('Unity Editor located');
              spinner = undefined;
            }
            verbose(`Editor path: ${event.editorPath}`);
            if (event.version) {
              ui.info(`Detected editor version from project: ${event.version}`);
              verbose(`Resolved editor version from ProjectVersion.txt: ${event.version}`);
            }
            break;
          }
          case 'connection-details': {
            const envVars = event.envVars;
            if (Object.keys(envVars).length > 0) {
              ui.heading('Connection Details');
              ui.label('Project', event.projectPath);
              ui.label('Editor', event.editorPath);
              ui.heading('Environment Variables');
              for (const [key, value] of Object.entries(envVars)) {
                const display = key === 'UNITY_MCP_TOKEN' ? '***' : value;
                ui.label(key, display);
                verbose(`Setting ${key}=${display}`);
              }
              ui.divider();
            } else {
              verbose('MCP connection disabled via --no-connect or no options');
            }
            break;
          }
          case 'launching-editor': {
            ui.label('Project', event.projectPath);
            ui.label('Editor', event.editorPath);
            break;
          }
          case 'editor-launched': {
            ui.success(`Launched Unity Editor (PID: ${event.pid ?? 'unknown'})`);
            break;
          }
          default:
            break;
        }
      },
    });

    if (spinner) {
      // Library returned before emitting `editor-resolved` — most
      // likely failed to locate an editor. Stop the spinner so the
      // error path renders cleanly.
      spinner.stop();
      spinner = undefined;
    }

    if (result.kind === 'failure') {
      // Render the failure with the original CLI surface. Two cases
      // get the rich "no editor found" help; everything else is a
      // plain ui.error.
      if (
        result.errorMessage.startsWith('Unity Editor') &&
        result.errorMessage.endsWith('is not installed.')
      ) {
        printEditorNotFoundHelp(options.unity, 'open');
      } else if (result.errorMessage === 'No Unity Editor found.') {
        printEditorNotFoundHelp(undefined, 'open');
      } else {
        ui.error(result.errorMessage);
      }
      process.exit(1);
    }

    // Already-running short-circuit: the library returns success
    // with `alreadyRunning: true`. Preserve the original exit-0 +
    // friendly message.
    if (result.alreadyRunning) {
      ui.success(`Unity is already running with this project (PID: ${result.editorPid ?? 'unknown'})`);
      ui.info('Skipping launch. Use the running instance or close it first.');
      process.exit(0);
    }

    // Path is normally absolute already; resolve defensively for
    // verbose log parity with the legacy CLI.
    verbose(`Resolved project path: ${path.resolve(result.projectPath)}`);
  });
