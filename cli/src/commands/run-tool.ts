import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { generatePortFromDirectory } from '../utils/port.js';

interface RunToolOptions {
  path?: string;
  url?: string;
  input?: string;
  inputFile?: string;
  token?: string;
  raw?: boolean;
}

function resolveUrl(positionalPath: string | undefined, options: RunToolOptions): string {
  if (options.url) {
    verbose(`Using explicit URL: ${options.url}`);
    return options.url.replace(/\/$/, '');
  }

  const dir = positionalPath ?? options.path ?? process.cwd();
  const resolvedDir = path.resolve(dir);
  verbose(`Resolving port from directory: ${resolvedDir}`);

  const port = generatePortFromDirectory(resolvedDir);
  const url = `http://localhost:${port}`;
  verbose(`Resolved URL: ${url}`);
  return url;
}

function parseInput(options: RunToolOptions): string {
  if (options.inputFile) {
    const filePath = path.resolve(options.inputFile);
    if (!fs.existsSync(filePath)) {
      ui.error(`Input file does not exist: ${filePath}`);
      process.exit(1);
    }
    const content = fs.readFileSync(filePath, 'utf-8');
    // Validate JSON
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
  .argument('[path]', 'Unity project path (used for auto port detection)')
  .option('--path <path>', 'Unity project path (auto port detection)')
  .option('--url <url>', 'Direct server URL override')
  .option('--input <json>', 'JSON string of tool arguments')
  .option('--input-file <file>', 'Read JSON arguments from file')
  .option('--token <token>', 'Bearer token for authorization')
  .option('--raw', 'Output raw JSON (no formatting)')
  .action(async (toolName: string, positionalPath: string | undefined, options: RunToolOptions) => {
    const baseUrl = resolveUrl(positionalPath, options);
    const body = parseInput(options);
    const endpoint = `${baseUrl}/api/tools/${toolName}`;

    verbose(`Tool: ${toolName}`);
    verbose(`Endpoint: ${endpoint}`);
    verbose(`Body: ${body}`);

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (options.token) {
      headers['Authorization'] = `Bearer ${options.token}`;
      verbose('Authorization header set');
    }

    if (!options.raw) {
      ui.heading('Run Tool');
      ui.label('Tool', toolName);
      ui.label('URL', endpoint);
      if (options.token) {
        ui.label('Auth', '***');
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
