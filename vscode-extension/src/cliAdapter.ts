import type {
  InstallPluginOptions,
  InstallResult,
  OpenProjectOptions,
  OpenProjectResult,
  ProgressEvent,
  SetupMcpOptions,
  SetupMcpResult,
} from 'unity-mcp-cli' with { "resolution-mode": "import" };
import { ExtensionLogger } from './logging';

export type CliModuleLoader = () => Promise<UnityMcpCliModule>;

interface UnityMcpCliModule {
  installPlugin(options: InstallPluginOptions): Promise<InstallResult>;
  openProject(options: OpenProjectOptions): Promise<OpenProjectResult>;
  setupMcp(options: SetupMcpOptions): Promise<SetupMcpResult>;
  listAgentIds(): string[];
}

export async function configureVscodeProject(
  logger: ExtensionLogger,
  options: ConfigureVscodeProjectOptions,
  loader: CliModuleLoader = defaultLoader,
): Promise<SetupMcpResult> {
  logger.debug('cliAdapter:loadStart', {});

  let cliModule: UnityMcpCliModule;
  try {
    cliModule = await loader();
    logger.debug('cliAdapter:loadSuccess', {
      availableAgents: cliModule.listAgentIds(),
    });
  } catch (error) {
    const message = toErrorMessage(error);
    logger.error('cliAdapter:loadFailure', {
      message,
    });

    return {
      kind: 'failure',
      success: false,
      warnings: [],
      nextSteps: [],
      error: new Error(`Could not load unity-mcp-cli: ${message}`),
    };
  }

  if (!cliModule.listAgentIds().includes('vscode-copilot')) {
    logger.error('cliAdapter:loadFailure', {
      reason: 'missing-agent-id',
    });

    return {
      kind: 'failure',
      success: false,
      warnings: [],
      nextSteps: [],
      error: new Error('unity-mcp-cli does not expose the vscode-copilot agent configuration.'),
    };
  }

  logger.info('cliAdapter:callStart', {
    workspacePath: options.workspacePath,
    transport: options.transport,
  });

  const result = await cliModule.setupMcp({
    agentId: 'vscode-copilot',
    unityProjectPath: options.workspacePath,
    transport: options.transport,
    onProgress: (event) => {
      logger.debug('cliAdapter:progress', {
        phase: event.phase,
        message: event.message,
      });
      options.onProgress?.(event);
    },
  });

  if (result.kind === 'success') {
    logger.info('cliAdapter:callSuccess', {
      configPath: result.configPath,
      transport: result.transport,
      warnings: result.warnings.length,
    });
    return result;
  }

  logger.error('cliAdapter:callFailure', {
    message: result.error.message,
    warnings: result.warnings.length,
  });
  return result;
}

export async function installUnityMcpPlugin(
  logger: ExtensionLogger,
  options: InstallUnityMcpPluginOptions,
  loader: CliModuleLoader = defaultLoader,
): Promise<InstallResult> {
  logger.debug('cliAdapter:loadStart', {});

  let cliModule: UnityMcpCliModule;
  try {
    cliModule = await loader();
    logger.debug('cliAdapter:loadSuccess', {});
  } catch (error) {
    const message = toErrorMessage(error);
    logger.error('cliAdapter:loadFailure', {
      message,
    });

    return {
      kind: 'failure',
      success: false,
      warnings: [],
      nextSteps: [],
      error: new Error(`Could not load unity-mcp-cli: ${message}`),
    };
  }

  logger.info('cliAdapter:callStart', {
    workspacePath: options.workspacePath,
    operation: 'installPlugin',
  });

  const result = await cliModule.installPlugin({
    unityProjectPath: options.workspacePath,
    version: options.version,
    onProgress: (event) => {
      logger.debug('cliAdapter:progress', {
        phase: event.phase,
        message: event.message,
      });
      options.onProgress?.(event);
    },
  });

  if (result.kind === 'success') {
    logger.info('cliAdapter:callSuccess', {
      manifestPath: result.manifestPath,
      installedVersion: result.installedVersion,
      warnings: result.warnings.length,
    });
    return result;
  }

  logger.error('cliAdapter:callFailure', {
    message: result.error.message,
    warnings: result.warnings.length,
  });
  return result;
}

export async function openUnityProject(
  logger: ExtensionLogger,
  options: OpenUnityProjectOptions,
  loader: CliModuleLoader = defaultLoader,
): Promise<OpenProjectResult> {
  logger.debug('cliAdapter:loadStart', {});

  let cliModule: UnityMcpCliModule;
  try {
    cliModule = await loader();
    logger.debug('cliAdapter:loadSuccess', {});
  } catch (error) {
    const message = toErrorMessage(error);
    logger.error('cliAdapter:loadFailure', {
      message,
    });

    return {
      kind: 'failure',
      success: false,
      warnings: [],
      errorMessage: `Could not load unity-mcp-cli: ${message}`,
      error: new Error(`Could not load unity-mcp-cli: ${message}`),
    };
  }

  logger.info('cliAdapter:callStart', {
    workspacePath: options.workspacePath,
    operation: 'openProject',
    noConnect: options.noConnect,
    startServer: options.startServer,
    transport: options.transport,
  });

  const result = await cliModule.openProject({
    projectPath: options.workspacePath,
    noConnect: options.noConnect,
    url: options.url,
    token: options.token,
    auth: options.auth,
    keepConnected: options.keepConnected,
    transport: options.transport,
    startServer: options.startServer,
    onProgress: (event) => {
      logger.debug('cliAdapter:progress', {
        phase: event.phase,
        message: event.message,
      });
      options.onProgress?.(event);
    },
  });

  if (result.kind === 'success') {
    logger.info('cliAdapter:callSuccess', {
      editorPath: result.editorPath,
      editorPid: result.editorPid ?? null,
      alreadyRunning: result.alreadyRunning ?? false,
      warnings: result.warnings.length,
    });
    return result;
  }

  logger.error('cliAdapter:callFailure', {
    message: result.error.message,
    warnings: result.warnings.length,
  });
  return result;
}

async function defaultLoader(): Promise<UnityMcpCliModule> {
  // tsconfig uses the Node16 module target so this CommonJS extension
  // entrypoint keeps native import() at runtime for ESM-only packages.
  return import('unity-mcp-cli') as Promise<UnityMcpCliModule>;
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export interface ConfigureVscodeProjectOptions {
  workspacePath: string;
  transport: 'http' | 'stdio';
  onProgress?: (event: ProgressEvent) => void;
}

export interface InstallUnityMcpPluginOptions {
  workspacePath: string;
  version?: string;
  onProgress?: (event: ProgressEvent) => void;
}

export interface OpenUnityProjectOptions {
  workspacePath: string;
  noConnect?: boolean;
  url?: string;
  token?: string;
  auth?: 'none' | 'required';
  keepConnected?: boolean;
  transport?: 'streamableHttp' | 'stdio';
  startServer?: boolean;
  onProgress?: (event: ProgressEvent) => void;
}
