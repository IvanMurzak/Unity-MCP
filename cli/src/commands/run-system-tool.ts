import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { parseInput } from '../utils/input.js';

interface RunSystemToolOptions {
  path?: string;
  url?: string;
  token?: string;
  input?: string;
  inputFile?: string;
  raw?: boolean;
  timeout?: string;
}

export const runSystemToolCommand = new Command('run-system-tool')
  .description('Execute a system tool via the HTTP API (not exposed to MCP clients)')
  .argument('<tool-name>', 'Name of the system tool to execute')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--input <json>', 'JSON string of tool arguments')
  .option('--input-file <file>', 'Read JSON arguments from file')
  .option('--raw', 'Output raw JSON (no formatting)')
  .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
  .action(async (toolName: string, positionalPath: string | undefined, options: RunSystemToolOptions) => {
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url: baseUrl, token } = resolveConnection(projectPath, options);
    const body = parseInput(options);
    const endpoint = `${baseUrl}/api/system-tools/${encodeURIComponent(toolName)}`;

    verbose(`System tool: ${toolName}`);
    verbose(`Endpoint: ${endpoint}`);
    verbose(`Body: ${body}`);

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
      verbose(`Authorization header set (source: ${options.token ? '--token flag' : 'config'})`);
    }

    const authSource = options.token ? '--token flag' : 'config';

    if (!options.raw) {
      ui.heading('Run System Tool');
      ui.label('Tool', toolName);
      ui.label('URL', endpoint);
      if (token) {
        ui.label('Auth', `from ${authSource}`);
      }
      ui.divider();
    }

    const spinner = options.raw ? null : ui.startSpinner(`Calling ${toolName}...`);

    const timeoutMs = parseInt(options.timeout ?? '60000', 10);
    const controller = new AbortController();
    const fetchTimeout = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers,
        body,
        signal: controller.signal,
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
          process.stdout.write(responseText);
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
        process.stdout.write(responseText);
      } else {
        ui.success('Response:');
        console.log(typeof responseData === 'string'
          ? responseData
          : JSON.stringify(responseData, null, 2));
      }
    } catch (err) {
      spinner?.stop();
      const isTimeout = err instanceof Error && err.name === 'AbortError';
      const cause = err instanceof Error && 'cause' in err ? (err.cause as Error & { code?: string }) : null;
      const causeCode = cause?.code ?? '';
      const rootMessage = cause?.message || causeCode || (err instanceof Error ? err.message : String(err));
      const errorSignature = `${rootMessage} ${causeCode}`;
      const isConnectionRefused = errorSignature.includes('ECONNREFUSED');
      const isConnectionReset = errorSignature.includes('ECONNRESET');
      const isNetworkError = errorSignature.includes('EAI_AGAIN') || errorSignature.includes('ENOTFOUND');

      let displayMessage: string;
      if (isTimeout) {
        displayMessage = `System tool call timed out after ${timeoutMs / 1000} seconds: ${toolName}`;
      } else if (isConnectionRefused) {
        displayMessage = `Connection refused at ${endpoint}. Is the MCP server running? Start Unity Editor with the MCP plugin first.`;
      } else if (isConnectionReset) {
        displayMessage = `Connection was reset by the server at ${endpoint}. The server may have crashed or restarted.`;
      } else if (isNetworkError) {
        displayMessage = `Cannot reach ${endpoint}. Check your network connection and server URL.`;
      } else {
        displayMessage = `${rootMessage}`;
        if (cause && cause.message !== rootMessage) {
          displayMessage += ` (${cause.message})`;
        }
      }

      if (options.raw) {
        process.stderr.write(displayMessage + '\n');
      } else {
        ui.error(`Failed to call system tool: ${displayMessage}`);
      }
      process.exit(1);
    } finally {
      clearTimeout(fetchTimeout);
    }
  });
