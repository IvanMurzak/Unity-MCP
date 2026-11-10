import { Command, Option } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import { RQL_MUTATE_TOOL, RQL_QUERY_TOOL, RQL_SCHEMA_TOOL, assetsWrittenSince, isRqlMutation, stripToolName } from '../lib/rql.js';
import { runTool } from '../lib/run-tool.js';
import { resolveAndValidateProjectPath } from '../utils/connection.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { runToolAction, type BuilderOptions, type RunToolCliOptions } from './run-tool-builder.js';

interface RqlCliOptions extends Omit<RunToolCliOptions, 'input' | 'inputFile' | 'arg'> {
  schema?: string | boolean;
  dryRun?: boolean;
  maxTargets?: string;
}

/** The recipes `rql --help` shows; the root help shows the first ones. */
export const RQL_RECIPES: ReadonlyArray<readonly [string, string]> = [
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
const RQL_CALL: BuilderOptions = {
  name: 'rql',
  description: '',
  argDescription: '',
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
  'Quote the query with single quotes; strings inside it take double quotes. A set / call is saved to disk at once (the',
  'files written are listed) and its receipt lists every change, so a separate check is rarely needed.',
  'Sources, types and members: unity-mcp-cli rql --schema [Type]  (full language: unity-mcp-cli tools rql-query)',
].join('\n');

export const rqlCommand = new Command('rql')
  .description('Read or change the editor with one RQL query: scenes, GameObjects, components, assets (a query ending in set / call changes the project and saves it, any other only reads); starts a headless editor when none answers')
  .argument('[query]', 'RQL query, e.g. \'objects | where name starts-with "Tree" | count\'')
  .option('--path <path>', 'Unity project path (defaults to cwd)')
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
    const query = queryArg === undefined ? undefined : stripToolName(queryArg);
    let toolName: string;
    let input: Record<string, unknown>;
    if (options.schema !== undefined) {
      toolName = RQL_SCHEMA_TOOL;
      input = typeof options.schema === 'string' ? { type: options.schema } : {};
    } else if (query === undefined || query.trim() === '') {
      const out: CommandOutput = new CommandOutput('rql', parseFormat(options.format));
      out.fail(ExitCode.Usage, { code: 'USAGE', message: 'No query given: unity-mcp-cli rql \'objects | where name starts-with "Tree" | count\' (recipes: rql --help)' });
    } else if (isRqlMutation(query)) {
      toolName = RQL_MUTATE_TOOL;
      input = {
        query,
        ...(options.dryRun ? { dry_run: true } : {}),
        ...(options.maxTargets !== undefined ? { max_targets: Number(options.maxTargets) } : {}),
      };
    } else {
      toolName = RQL_QUERY_TOOL;
      input = { query };
    }

    // A set / call is saved to disk by the editor; which files it wrote is read from the disk, so nobody re-opens the
    // scene to check.
    const human = !options.raw && parseFormat(options.format) === 'text';
    const saveCheck = toolName! === RQL_MUTATE_TOOL && !options.dryRun && human ? resolveAndValidateProjectPath(undefined, options) : undefined;
    const startedAt = Date.now();
    await runToolAction(RQL_CALL, toolName!, undefined, { ...options, input: JSON.stringify(input!) });
    if (saveCheck !== undefined) {
      const written = assetsWrittenSince(saveCheck, startedAt);
      console.log(written.length > 0 ? `Saved to disk: ${written.join(', ')}` : 'No scene or asset file changed on disk.');
    }
  });
