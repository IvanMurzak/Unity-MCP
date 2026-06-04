import type {
  ProgressEvent,
  SetupMcpOptions,
  SetupMcpResult,
} from 'unity-mcp-cli';
import { ExtensionLogger } from './logging';

export type CliModuleLoader = () => Promise<UnityMcpCliModule>;

type NativeImport = <T>(specifier: string) => Promise<T>;

interface UnityMcpCliModule {
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

async function defaultLoader(): Promise<UnityMcpCliModule> {
  // Keep native ESM loading at runtime. TypeScript rewrites plain
  // `import()` into `require()` for CommonJS output, which breaks on
  // packages like `unity-mcp-cli` that only expose ESM import exports.
  return nativeImport<UnityMcpCliModule>('unity-mcp-cli');
}

const nativeImport = new Function(
  'specifier',
  'return import(specifier);',
) as NativeImport;

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export interface ConfigureVscodeProjectOptions {
  workspacePath: string;
  transport: 'http' | 'stdio';
  onProgress?: (event: ProgressEvent) => void;
}
