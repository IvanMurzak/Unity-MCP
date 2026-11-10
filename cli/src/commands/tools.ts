import { Command } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { localFallbackNote, resolveAndValidateProjectPath, resolveEditorConnection, resolveProbeUrls } from '../utils/connection.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { ensureEditor } from '../lib/ensure-editor.js';
import { describeInputs, listTools, readToolCache, summary, writeToolCache, type ToolCatalog, type ToolInfo } from '../lib/list-tools.js';
import { canStartEditor, classifyEditorFailure } from './run-tool-builder.js';
import { parsePositiveIntFlag } from './open.js';

interface ToolsCliOptions {
  path?: string;
  url?: string;
  token?: string;
  start?: boolean;
  cached?: boolean;
  once?: boolean;
  readyTimeout?: string;
  format?: string;
}

/** An example `--input` for a tool: its required inputs with placeholder values. */
function exampleInput(tool: ToolInfo): string {
  const props = (tool.inputSchema.properties ?? {}) as Record<string, { type?: unknown }>;
  const required = (tool.inputSchema.required as string[] | undefined) ?? Object.keys(props).slice(0, 1);
  const sample: Record<string, unknown> = {};
  for (const name of required) {
    const type = props[name]?.type;
    sample[name] = type === 'number' || type === 'integer' ? 0 : type === 'boolean' ? true : type === 'object' ? {} : type === 'array' ? [] : '...';
  }
  return JSON.stringify(sample);
}

function printCatalog(catalog: ToolCatalog, tools: ToolInfo[], source: string): void {
  console.log(`AI tools of the Unity Editor (${source}).`);
  console.log('Details of one: unity-mcp-cli tools <name>. Call one: unity-mcp-cli run-tool <name> --input \'<json>\'.\n');
  const width = Math.max(...tools.map(t => t.name.length), 8);
  for (const t of tools) {
    console.log(`  ${t.name.padEnd(width)}  ${summary(t.description)}`);
    const inputs = describeInputs(t.inputSchema).map(i => i.split(' — ')[0]);
    if (inputs.length > 0) console.log(`  ${' '.repeat(width)}  inputs: ${inputs.join(', ')}`);
  }
  if (catalog.hidden.length > 0) {
    console.log(`\nHidden (not offered to MCP clients, callable by name): ${catalog.hidden.join(', ')}`);
  }
}

function printTool(tool: ToolInfo): void {
  console.log(`${tool.name}\n`);
  console.log(tool.description.trim());
  const inputs = describeInputs(tool.inputSchema);
  console.log(`\nInputs (* = required):${inputs.length === 0 ? ' none' : ''}`);
  for (const i of inputs) console.log(`  ${i}`);
  console.log(`\nCall it:\n  unity-mcp-cli run-tool ${tool.name} --input '${exampleInput(tool)}'`);
  console.log(`\nFull input schema:\n${JSON.stringify(tool.inputSchema, null, 2)}`);
}

export const toolsCommand = new Command('tools')
  .description('List the AI tools the project\'s Unity Editor offers (name, purpose, inputs), or show one in detail; starts a headless editor when needed')
  .argument('[name]', 'A tool name for its full description and input schema, or a regex to filter the list')
  .option('--path <path>', 'Unity project path (default: current directory)')
  .option('--cached', 'Answer from the list saved in the project by the last live read, without contacting the editor')
  .option('--no-start', 'Never start an editor: use the saved list when no editor answers')
  .option('--once', 'Close the editor afterwards if this call started it')
  .option('--ready-timeout <ms>', 'How long to wait for a started editor to become ready', String(DEFAULT_SESSION_READY_TIMEOUT_MS))
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .addHelpText('after', '\nExamples:\n  unity-mcp-cli tools\n  unity-mcp-cli tools rql-mutate\n  unity-mcp-cli tools screenshot')
  .action(async (name: string | undefined, options: ToolsCliOptions) => {
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('tools', parseFormat(options.format));
    const readyTimeoutMs = parsePositiveIntFlag(options.readyTimeout, '--ready-timeout', DEFAULT_SESSION_READY_TIMEOUT_MS);
    const projectPath = resolveAndValidateProjectPath(undefined, options);
    const cached = readToolCache(projectPath);

    let catalog: ToolCatalog | undefined;
    let source = 'live';
    if (options.cached) {
      if (!cached) out.fail(ExitCode.CommandFailed, { code: 'NOT_FOUND', message: 'No saved tool list yet: run `unity-mcp-cli tools` once without --cached.' });
      catalog = cached;
      source = `saved ${cached.readAt}`;
    } else {
      const conn = await resolveEditorConnection(projectPath, options);
      if (conn.signInRequired) out.fail(ExitCode.CommandFailed, { code: 'NOT_AUTHENTICATED', message: conn.signInRequired });
      if (conn.localFallback) out.warn(localFallbackNote(conn.url));
      const spinner = out.machine ? null : ui.startSpinner('Connecting to the Unity Editor...');
      const mayStart = options.start !== false && canStartEditor(projectPath, options.url);
      let baseUrl: string | undefined = conn.url;
      let closeIfStarted = async (): Promise<number | undefined> => undefined;
      if (mayStart) {
        const ensured = await ensureEditor({
          projectPath,
          probeUrls: resolveProbeUrls(projectPath, conn.url, conn.overrideUrl),
          token: conn.token,
          overrideUrl: conn.overrideUrl,
          overrideToken: options.token,
          readyTimeoutMs,
          closeOnFailure: options.once === true,
          onStatus: (message) => {
            verbose(message);
            out.progress(message);
            if (spinner) spinner.text = message;
          },
        });
        if (ensured.kind === 'failure') {
          baseUrl = undefined;
          if (!cached) {
            spinner?.stop();
            const { exitCode, code } = classifyEditorFailure(ensured);
            out.fail(exitCode, { code, message: ensured.message });
          }
          out.warn(`The editor could not be reached (${ensured.message}); showing the list saved ${cached!.readAt}.`);
        } else {
          baseUrl = ensured.baseUrl;
          closeIfStarted = ensured.closeIfStarted;
        }
      }
      if (baseUrl !== undefined) {
        const listed = await listTools({ baseUrl, token: conn.token });
        if (options.once) await closeIfStarted();
        if (listed.kind === 'success') {
          catalog = listed.catalog;
          writeToolCache(projectPath, catalog);
        } else if (!cached) {
          spinner?.stop();
          out.fail(ExitCode.Unreachable, { code: 'UNREACHABLE', message: listed.message },
            null, 'Start the editor (`unity-mcp-cli open --headless`) or let this command start it (no --no-start).');
        } else {
          out.warn(`${listed.message}; showing the list saved ${cached.readAt}.`);
        }
      }
      spinner?.stop();
      if (!catalog) {
        catalog = cached;
        source = `saved ${cached?.readAt}`;
      }
      if (!catalog) out.fail(ExitCode.Unreachable, { code: 'UNREACHABLE', message: 'No editor answered and no saved tool list exists.' });
    }

    const exact = name !== undefined ? catalog.tools.find(t => t.name === name) : undefined;
    if (exact) {
      if (out.machine) out.succeed({ tool: exact, hidden: false });
      else printTool(exact);
      return;
    }
    if (name !== undefined && catalog.hidden.includes(name)) {
      out.warn(`${name} is a hidden tool: callable by name, but its schema is not published. Try: unity-mcp-cli run-tool ${name} --input '{}'`);
      out.succeed({ tool: { name, hidden: true } });
      return;
    }
    let tools = catalog.tools;
    if (name !== undefined) {
      let filter: RegExp;
      try {
        filter = new RegExp(name, 'i');
      } catch {
        out.fail(ExitCode.Usage, { code: 'USAGE', message: `Not a tool name or a valid regex: ${name}` });
      }
      tools = tools.filter(t => filter.test(t.name) || filter.test(t.description));
      if (tools.length === 0) {
        out.fail(ExitCode.CommandFailed, { code: 'NOT_FOUND', message: `No tool matches "${name}". Available: ${catalog.tools.map(t => t.name).join(', ')}` });
      }
    }
    if (out.machine) {
      out.succeed({ source, tools, hidden: catalog.hidden });
      return;
    }
    printCatalog(catalog, tools, source);
  });
