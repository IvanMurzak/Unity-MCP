import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { readConfig, resolveConnectionFromConfig } from '../utils/config.js';

interface RunToolOptions {
  path?: string;
  url?: string;
  input?: string;
  inputFile?: string;
  raw?: boolean;
}

/**
 * Resolve the project path from positional arg, --path option, or cwd.
 */
function resolveProjectPath(positionalPath: string | undefined, options: RunToolOptions): string {
  return path.resolve(positionalPath ?? options.path ?? process.cwd());
}

/**
 * Resolve the server URL and auth token.
 *
 * URL priority:
 *   1. --url flag (explicit override)
 *   2. Config file connectionMode → Custom: host, Cloud: cloudServerUrl
 *   3. Deterministic port from project path
 *
 * Token: always read from config (Custom: token, Cloud: cloudToken).
 */
function resolveConnection(
  projectPath: string,
  options: RunToolOptions
): { url: string; token: string | undefined } {
  const config = readConfig(projectPath);
  const fromConfig = config ? resolveConnectionFromConfig(config) : { url: undefined, token: undefined };

  verbose(`Config loaded: connectionMode=${config?.connectionMode ?? 'N/A'}, configUrl=${fromConfig.url ?? 'N/A'}, hasToken=${!!fromConfig.token}`);

  let url: string;
  if (options.url) {
    url = options.url.replace(/\/$/, '');
    verbose(`Using explicit --url: ${url}`);
  } else if (fromConfig.url) {
    url = fromConfig.url.replace(/\/$/, '');
    verbose(`Using URL from config (${config?.connectionMode} mode): ${url}`);
  } else {
    const port = generatePortFromDirectory(projectPath);
    url = `http://localhost:${port}`;
    verbose(`Using deterministic port URL: ${url}`);
  }

  return { url, token: fromConfig.token };
}

function parseInput(options: RunToolOptions): string {
  if (options.inputFile) {
    const filePath = path.resolve(options.inputFile);
    if (!fs.existsSync(filePath)) {
      ui.error(`Input file does not exist: ${filePath}`);
      process.exit(1);
    }
    const content = fs.readFileSync(filePath, 'utf-8');
    try {
      JSON.parse(content);
    } catch {
      ui.error(`Input file does not contain valid JSON: ${filePath}`);
      process.exit(1);
    }
    return content;
  }

  if (options.input) {
    try {
      JSON.parse(options.input);
    } catch {
      ui.error('--input must be valid JSON');
      process.exit(1);
    }
    return options.input;
  }

  return '{}';
}

export const runToolCommand = new Command('run-tool')
  .description('Execute an MCP tool via the HTTP API')
  .argument('<tool-name>', 'Name of the MCP tool to execute')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--input <json>', 'JSON string of tool arguments')
  .option('--input-file <file>', 'Read JSON arguments from file')
  .option('--raw', 'Output raw JSON (no formatting)')
  .action(async (toolName: string, positionalPath: string | undefined, options: RunToolOptions) => {
    const projectPath = resolveProjectPath(positionalPath, options);
    const { url: baseUrl, token } = resolveConnection(projectPath, options);
    const body = parseInput(options);
    const endpoint = `${baseUrl}/api/tools/${toolName}`;

    verbose(`Tool: ${toolName}`);
    verbose(`Endpoint: ${endpoint}`);
    verbose(`Body: ${body}`);

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
      verbose('Authorization header set from config');
    }

    if (!options.raw) {
      ui.heading('Run Tool');
      ui.label('Tool', toolName);
      ui.label('URL', endpoint);
      if (token) {
        ui.label('Auth', 'from config');
      }
      ui.divider();
    }

    const spinner = options.raw ? null : ui.startSpinner(`Calling ${toolName}...`);

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers,
        body,
      });

      const responseText = await response.text();
      let responseData: unknown;
      try {
        responseData = JSON.parse(responseText);
      } catch {
        responseData = responseText;
      }

      if (!response.ok) {
        spinner?.stop();
        if (options.raw) {
          process.stdout.write(typeof responseData === 'string'
            ? responseData
            : JSON.stringify(responseData, null, 2));
        } else {
          ui.error(`HTTP ${response.status}: ${response.statusText}`);
          if (responseData) {
            ui.info(typeof responseData === 'string'
              ? responseData
              : JSON.stringify(responseData, null, 2));
          }
        }
        process.exit(1);
      }

      spinner?.success(`${toolName} completed`);

      if (options.raw) {
        process.stdout.write(typeof responseData === 'string'
          ? responseData
          : JSON.stringify(responseData));
      } else {
        ui.success('Response:');
        console.log(typeof responseData === 'string'
          ? responseData
          : JSON.stringify(responseData, null, 2));
      }
    } catch (err) {
      spinner?.stop();
      const message = err instanceof Error ? err.message : String(err);
      if (options.raw) {
        process.stderr.write(message);
      } else {
        ui.error(`Failed to call tool: ${message}`);
      }
      process.exit(1);
    }
  });
