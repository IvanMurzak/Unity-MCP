import * as fs from 'fs';
import { Command, Option } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import { RQL_MUTATE_TOOL, assetsWrittenSince, rqlLastVerb, rqlToolCall, saveReceipt, stripToolName } from '../lib/rql.js';
import { runTool } from '../lib/run-tool.js';
import { resolveAndValidateProjectPath } from '../utils/connection.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import type { RqlRecipes } from '../utils/ui.js';
import { parsePositiveIntFlag } from './open.js';
import { runToolAction, type RunToolCall, type RunToolCliOptions } from './run-tool-builder.js';

interface RqlCliOptions extends Omit<RunToolCliOptions, 'input' | 'inputFile' | 'arg'> {
  file?: string;
  schema?: string | boolean;
  dryRun?: boolean;
  maxTargets?: string;
}

/** The recipes `rql --help` shows; the root help shows the first ones. */
export const RQL_RECIPES: RqlRecipes = [
  ['editor | call OpenScene("Assets/Scenes/Forest.unity")', 'open a scene (writes no file): objects lists the OPEN scenes only'],
  ['editor | call NewScene("Assets/Scenes/Cave.unity")', 'a new empty scene, saved at that path, now open'],
  ['editor | call CreateGameObject({name: "Ball", primitive: "Sphere", position: [1, 0.5, 0]})', 'create (meters, Y up)'],
  ['objects | where name starts-with "Tree" | select name transform.position transform.localScale', 'read'],
  ['objects | where name contains "Enemy" | count', 'count'],
  ['objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5', 'change (vectors compute per axis)'],
  ['objects | get components | where $ is Light | set intensity = 2', 'components'],
  ['objects | where name == "Old" | call Destroy()', 'delete'],
  ['unity.assets("t:Material", "Assets/Materials") | select name shader.name', 'assets of a type under a folder'],
];

/** How `rql` calls a tool: as run-tool does. */
const RQL_CALL: RunToolCall = {
  name: 'rql',
  routePrefix: '/api/tools',
  verboseLabel: 'Tool',
  headingLabel: 'RQL',
  errorNoun: 'tool',
  invoke: runTool,
};

const RQL_HELP_FOOTER = [
  'Recipes:',
  ...RQL_RECIPES.map(([query, what]) => `  ${query}  # ${what}`),
  '',
  'Stages: where, select a b, sort x [desc], first N, count, get <member>, set a = expr, call Method(args).',
  'Operators: == != < <= > >= and or not, contains, starts-with, ends-with, x in [a, b], $ is Light.',
  'Quote the query with single quotes; strings inside it take double quotes (a shell that strips them: -f <file> or -f -).',
  'A set / call is saved to disk at once (the files written are listed) and its receipt lists every change, so a',
  'separate check is rarely needed. Sources, types and members: unity-mcp-cli rql --schema [Type]',
  '(full language: unity-mcp-cli tools rql-query)',
].join('\n');

/** The query from the argument or `-f <file>` (`-f -`: stdin; stdin is never read unasked). */
function readQuery(query: string | undefined, file: string | undefined): string | undefined {
  if (query !== undefined && file !== undefined) throw new Error('Pass the query either as an argument or with -f, not both.');
  if (file === undefined) return query;
  return fs.readFileSync(file === '-' ? 0 : file, 'utf-8').trim();
}

export const rqlCommand = new Command('rql')
  .description('Read or change the editor with one RQL query: scenes, GameObjects, components, assets (a query ending in set / call changes the project and saves it, any other only reads); starts a headless editor when none answers')
  .argument('[query]', 'RQL query, e.g. \'objects | where name starts-with "Tree" | count\'')
  .option('--path <path>', 'Unity project path (defaults to cwd)')
  .option('-f, --file <file>', 'Read the query from a file ("-" for stdin)')
  .option('--schema [type]', 'List the RQL sources (or one type\'s members) instead of running a query')
  .option('--dry-run', 'For set / call: show what would change, change nothing')
  .option('--max-targets <n>', 'For set / call: allow more than 200 targets (up to 1000)')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .option('--no-start', 'Do not start a headless editor when none answers')
  .option('--once', 'Close the editor afterwards when this call started it')
  // The connection options of run-tool work here too; hidden to keep this help short.
  .addOption(new Option('--url <url>', 'Direct server URL override').hideHelp())
  .addOption(new Option('--token <token>', 'Bearer token override').hideHelp())
  .addOption(new Option('--raw', 'Output raw JSON').hideHelp())
  .addOption(new Option('--timeout <ms>', 'Request timeout in milliseconds').default('60000').hideHelp())
  .addOption(new Option('--ready-timeout <ms>', 'How long to wait for a started editor to become ready').default(String(DEFAULT_SESSION_READY_TIMEOUT_MS)).hideHelp())
  .addHelpText('after', `\n${RQL_HELP_FOOTER}\n`)
  .action(async (queryArg: string | undefined, options: RqlCliOptions) => {
    const out: CommandOutput = new CommandOutput('rql', parseFormat(options.format));
    let query: string | undefined;
    try {
      const text = readQuery(queryArg, options.file);
      query = text === undefined ? undefined : stripToolName(text);
    } catch (err) {
      out.fail(ExitCode.Usage, { code: 'USAGE', message: err instanceof Error ? err.message : String(err) });
    }
    const maxTargets = options.maxTargets === undefined ? undefined : parsePositiveIntFlag(options.maxTargets, '--max-targets', 200);
    const call = rqlToolCall(query, { schema: options.schema, dryRun: options.dryRun, maxTargets });
    if ('usage' in call) out.fail(ExitCode.Usage, { code: 'USAGE', message: call.usage });

    // Unity saves a set / call to disk; which files it wrote is read from the disk, so nobody re-opens the scene to
    // check. Not for an editor reached by --url: its disk may be another machine's.
    const human = !options.raw && !out.machine;
    const saveCheck = call.toolName === RQL_MUTATE_TOOL && !options.dryRun && human && options.url === undefined
      ? resolveAndValidateProjectPath(undefined, options) : undefined;
    const startedAt = Date.now();
    await runToolAction(RQL_CALL, call.toolName, undefined, { ...options, input: JSON.stringify(call.input) });
    if (saveCheck !== undefined) {
      const receipt = saveReceipt(assetsWrittenSince(saveCheck, startedAt), rqlLastVerb(query!));
      if (receipt) console.log(receipt);
    }
  });
