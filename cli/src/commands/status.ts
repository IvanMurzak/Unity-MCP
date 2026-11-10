import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { probe, type ProbeResult } from '../utils/probe.js';
import { listEditorDialogs, type EditorDialog } from '../utils/editor-dialogs.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { awaitingInputError } from './run-tool-builder.js';

interface StatusOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
  format?: string;
}

export const statusCommand = new Command('status')
  .description('Check Unity Editor and MCP server connection status')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--timeout <ms>', 'Probe timeout in milliseconds (default: 5000)', '5000')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .addHelpText('after', '\nExit codes: 0 the MCP server answers, 7 it does not, 18 the editor is blocked by a modal dialog (Windows).')
  .action(async (positionalPath: string | undefined, options: StatusOptions) => {
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('status', parseFormat(options.format));
    const human = !out.machine;
    const timeoutMs = parseInt(options.timeout ?? '5000', 10);
    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
      out.fail(ExitCode.Usage, { code: 'USAGE', message: `Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).` });
    }

    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url: configUrl, token } = await resolveConnection(projectPath, options);
    const localPort = generatePortFromDirectory(projectPath);
    const localUrl = `http://localhost:${localPort}`;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    if (human) {
      ui.heading('Unity-MCP Status');
      ui.label('Project', projectPath);
      ui.divider();
    }

    // 1. Unity process detection
    const proc = findUnityProcess(projectPath);
    if (human) {
      ui.heading('Unity Editor Process');
      if (proc) ui.success(`Unity is running (PID: ${proc.pid})`);
      else ui.warn('Unity is not running with this project');
    }

    // 2. Modal dialogs: a dialog blocks every tool call until a human answers it.
    const listed = proc ? await listEditorDialogs([proc.pid]) : undefined;
    const dialogs: EditorDialog[] = listed?.kind === 'ok' ? listed.dialogs : [];
    if (human && dialogs.length > 0) {
      ui.heading('Modal Dialogs');
      for (const d of dialogs) ui.warn(`"${d.title}" [${d.buttons.join(' | ')}]${d.message ? ` ${d.message.replace(/\s+/g, ' ')}` : ''}`);
    }

    // 3. Local server check
    let localSpinner: ReturnType<typeof ui.startSpinner> | null = null;
    if (human) {
      ui.heading('Local MCP Server');
      ui.label('URL', localUrl);
      localSpinner = ui.startSpinner(`Probing ${localUrl}...`);
    }
    const localResult = await probe(localUrl, headers, timeoutMs);
    if (localResult.ok) {
      localSpinner?.success('Connected');
      verbose(`Local server response: ${JSON.stringify(localResult.data)}`);
    } else {
      localSpinner?.error(`Not available (${localResult.reason})`);
    }

    // 4. Config-resolved server check (if different from local)
    let configResult: ProbeResult | null = null;
    if (configUrl !== localUrl) {
      let configSpinner: ReturnType<typeof ui.startSpinner> | null = null;
      if (human) {
        ui.heading('Config Server');
        ui.label('URL', configUrl);
        configSpinner = ui.startSpinner(`Probing ${configUrl}...`);
      }
      configResult = await probe(configUrl, headers, timeoutMs);
      if (configResult.ok) {
        configSpinner?.success('Connected');
        verbose(`Config server response: ${JSON.stringify(configResult.data)}`);
      } else {
        configSpinner?.error(`Not available (${configResult.reason})`);
      }
    }

    if (human) ui.divider();

    // Summary — reuse probe results, no duplicate calls
    const localOk = localResult.ok;
    const configOk = configResult ? configResult.ok : localOk;
    const data = {
      projectPath,
      editor: proc ? { running: true, pid: proc.pid } : { running: false },
      dialogs: listed === undefined ? [] : listed.kind === 'ok' ? dialogs : null,
      ...(listed && listed.kind !== 'ok' ? { dialogDetection: listed.kind === 'unsupported' ? 'unsupported-platform' : listed.message } : {}),
      servers: [
        { kind: 'local', url: localUrl, ok: localResult.ok, ...(localResult.ok ? {} : { reason: localResult.reason }) },
        ...(configResult ? [{ kind: 'config', url: configUrl, ok: configResult.ok, ...(configResult.ok ? {} : { reason: configResult.reason }) }] : []),
      ],
      ready: localOk || configOk,
    };

    if (dialogs.length > 0) {
      // Reachable or not, calls wait for the dialog: say so instead of "ready".
      out.fail(ExitCode.AwaitingInput, awaitingInputError(dialogs), data);
    }
    if (localOk || configOk) {
      if (human) ui.success('MCP server is reachable — ready for tool calls');
      out.succeed(data);
      process.exit(ExitCode.Success);
    }
    out.fail(ExitCode.Unreachable, {
      code: 'UNREACHABLE',
      message: proc ? 'Unity is running but MCP server is not responding yet' : 'Unity is not running and MCP server is not reachable',
    }, data);
  });
