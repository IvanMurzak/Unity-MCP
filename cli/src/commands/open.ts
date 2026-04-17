import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor, printEditorNotFoundHelp } from '../utils/unity-editor.js';
import { findUnityProcess } from '../utils/unity-process.js';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { readConfig, isCloudMode, writeConfig } from '../utils/config.js';

export interface ResolveProjectPathResult {
  /** Absolute, resolved path to the project directory. */
  projectPath: string;
  /** True if no path was supplied and we fell back to `process.cwd()`. */
  usedCwdFallback: boolean;
}

/**
 * Resolve the project path from the positional argument, `--path` option, or
 * the current working directory when neither is provided.
 *
 * Exported for unit testing.
 */
export function resolveOpenProjectPath(
  positionalPath: string | undefined,
  optionPath: string | undefined,
  cwd: string,
): ResolveProjectPathResult {
  const explicit = positionalPath ?? optionPath;
  const resolvedPath = explicit ?? cwd;
  return {
    projectPath: path.resolve(resolvedPath),
    usedCwdFallback: explicit === undefined,
  };
}

/**
 * Returns true if `projectPath` looks like a Unity project — i.e. it contains
 * an `Assets/` directory and a `ProjectSettings/ProjectVersion.txt` file.
 *
 * Exported for unit testing.
 */
export function isUnityProjectDir(projectPath: string): boolean {
  const hasAssets = fs.existsSync(path.join(projectPath, 'Assets'));
  const hasProjectVersion = fs.existsSync(
    path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'),
  );
  return hasAssets && hasProjectVersion;
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
    const { projectPath, usedCwdFallback } = resolveOpenProjectPath(
      positionalPath,
      options.path,
      process.cwd(),
    );

    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    // Validate the directory looks like a Unity project. We require the
    // presence of both `Assets/` and `ProjectSettings/ProjectVersion.txt`
    // to avoid launching the Editor against an unrelated folder — this
    // matters most when the user omits the path and we fall back to cwd.
    if (!isUnityProjectDir(projectPath)) {
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

    // Check if Unity is already running with this project
    const existingProcess = findUnityProcess(projectPath);
    if (existingProcess) {
      ui.success(`Unity is already running with this project (PID: ${existingProcess.pid})`);
      ui.info('Skipping launch. Use the running instance or close it first.');
      process.exit(0);
    }

    // Determine editor version
    let version = options.unity;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        ui.info(`Detected editor version from project: ${version}`);
        verbose(`Resolved editor version from ProjectVersion.txt: ${version}`);
      }
    }

    const spinner = ui.startSpinner('Locating Unity Editor...');
    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      spinner.stop();
      printEditorNotFoundHelp(version, 'open');
      process.exit(1);
    }
    spinner.success('Unity Editor located');
    verbose(`Editor path: ${editorPath}`);

    // Auto-detect Cloud mode: if project has cloudToken, ensure keep-connected
    // so the Unity plugin connects to the cloud server on startup.
    // Also enable auto-generate skills for claude-code by default.
    {
      const config = readConfig(projectPath);
      if (config && isCloudMode(config) && config.cloudToken) {
        if (!options.keepConnected) {
          options.keepConnected = true;
          verbose('Cloud mode with token detected — auto-enabling keep-connected');
        }

        const skillAutoGenerate = { ...(config.skillAutoGenerate ?? {}) } as Record<string, boolean>;
        if (!skillAutoGenerate['claude-code']) {
          skillAutoGenerate['claude-code'] = true;
          writeConfig(projectPath, { ...config, skillAutoGenerate });
          verbose('Auto-enabled skill generation for claude-code');
        }
      }
    }

    // Build environment variables for MCP connection (unless --no-connect)
    const useConnect = options.connect !== false;
    let env: Record<string, string> | undefined;

    if (useConnect) {
      const envVars: Record<string, string> = {};

      if (options.url) {
        envVars['UNITY_MCP_HOST'] = options.url;
      }

      if (options.keepConnected) {
        envVars['UNITY_MCP_KEEP_CONNECTED'] = 'true';
      }

      if (options.tools) {
        envVars['UNITY_MCP_TOOLS'] = options.tools;
      }

      if (options.token) {
        envVars['UNITY_MCP_TOKEN'] = options.token;
      }

      if (options.auth) {
        if (options.auth !== 'none' && options.auth !== 'required') {
          ui.error('--auth must be "none" or "required"');
          process.exit(1);
        }
        envVars['UNITY_MCP_AUTH_OPTION'] = options.auth;
      }

      if (options.transport) {
        if (options.transport !== 'streamableHttp' && options.transport !== 'stdio') {
          ui.error('--transport must be "streamableHttp" or "stdio"');
          process.exit(1);
        }
        envVars['UNITY_MCP_TRANSPORT'] = options.transport;
      }

      if (options.startServer !== undefined) {
        const val = options.startServer.toLowerCase();
        if (val !== 'true' && val !== 'false') {
          ui.error('--start-server must be "true" or "false"');
          process.exit(1);
        }
        envVars['UNITY_MCP_START_SERVER'] = val;
      }

      if (Object.keys(envVars).length > 0) {
        env = envVars;
        ui.heading('Connection Details');
        ui.label('Project', projectPath);
        ui.label('Editor', editorPath);

        ui.heading('Environment Variables');
        for (const [key, value] of Object.entries(envVars)) {
          const display = key === 'UNITY_MCP_TOKEN' ? '***' : value;
          ui.label(key, display);
          verbose(`Setting ${key}=${display}`);
        }
        ui.divider();
      }
    } else {
      verbose('MCP connection disabled via --no-connect');
    }

    ui.label('Project', projectPath);
    ui.label('Editor', editorPath);
    launchEditor(editorPath, projectPath, env);
  });
