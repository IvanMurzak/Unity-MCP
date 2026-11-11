import { Command, Option } from 'commander';
import { unityAccountToolCall } from '../lib/rql.js';
import { FORMAT_OPTION_DESCRIPTION } from '../utils/output.js';
import { RQL_CALL } from './rql.js';
import { runToolAction, type RunToolCall, type RunToolCliOptions } from './run-tool-builder.js';

interface UnityAccountCliOptions extends Omit<RunToolCliOptions, 'input' | 'inputFile' | 'arg' | 'start' | 'once' | 'readyTimeout'> {
  signIn?: boolean;
}

/** How `unity-account` calls the editor: as `rql` does. */
const UNITY_ACCOUNT_CALL: RunToolCall = { ...RQL_CALL, name: 'unity-account' };

export const unityAccountCommand = new Command('unity-account')
  .description(
    'Show the Unity account the running Unity Editor is signed in to (Asset Store "My Assets" needs one); --sign-in opens ' +
    "Unity's sign-in in that editor for the user to complete. Not the ai-game.dev account: that is login",
  )
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--path <path>', 'Unity project path (defaults to cwd)')
  .option('--sign-in', "Open Unity's sign-in in the editor when it is not signed in; the user signs in there")
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .addOption(new Option('--url <url>', 'Direct server URL override').hideHelp())
  .addOption(new Option('--token <token>', 'Bearer token override').hideHelp())
  .addOption(new Option('--raw', 'Output raw JSON').hideHelp())
  .addOption(new Option('--timeout <ms>', 'Request timeout in milliseconds').default('60000').hideHelp())
  .addHelpText('after', [
    '',
    'Needs the project open in a Unity Editor with a window (unity-mcp-cli open <path>): it never starts a headless one,',
    'which cannot show the sign-in. The same from an agent: rql \'assetStore | select signedIn userName\' and',
    "rql 'assetStore | call SignIn()'. The agent never types the credentials.",
    '',
  ].join('\n'))
  .action(async (positionalPath: string | undefined, options: UnityAccountCliOptions) => {
    const call = unityAccountToolCall(options.signIn === true);
    // Never start an editor: a headless one cannot show Unity's sign-in, and its account says nothing about the user's.
    await runToolAction(UNITY_ACCOUNT_CALL, call.toolName, positionalPath, { ...options, start: false, input: JSON.stringify(call.input) });
  });
