// The one source of the agent-facing usage text: the root --help, `rql --help` and the `unity-mcp-cli` agent skill
// (`setup-skills`) all render from these constants, so the three cannot drift apart.
// Example names stay neutral (Forest, Cave, Ball, Tree, Rock, Enemy): never a name a test or evaluation task uses.

/** RQL recipes: [query, what it does]. */
export type RqlRecipes = ReadonlyArray<readonly [string, string]>;
/** A featured command: [name, usage, what it does]. */
export type CommandRow = readonly [string, string, string];

/** The CLI's command name. */
export const CLI_NAME = 'unity-mcp-cli';

/** The recipes `rql --help` and the skill show; the root help shows the first {@link ROOT_RQL_RECIPES}. */
export const RQL_RECIPES: RqlRecipes = [
  ['editor | call OpenScene("Assets/Scenes/Forest.unity")', 'open a scene (writes no file): objects lists the OPEN scenes only'],
  ['editor | call NewScene("Assets/Scenes/Cave.unity")', 'a new scene (camera + light), saved at that path, now open'],
  ['editor | call CreateGameObject({name: "Ball", primitive: "Sphere", position: [1, 0.5, 0]})', 'create (meters, Y up)'],
  ['objects | where name starts-with "Tree" | select name transform.position transform.localScale', 'read'],
  ['objects | where name contains "Enemy" | count', 'count'],
  ['objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5', 'change (vectors compute per axis)'],
  ['objects | get components | where $ is Light | set intensity = 2', 'components'],
  ['objects | where name == "Old" | call Destroy()', 'delete'],
  ['unity.assets("t:Material", "Assets/Materials") | select name shader.name', 'assets of a type under a folder'],
];

/** How many recipes the root help shows. */
export const ROOT_RQL_RECIPES = 6;

/** The RQL language in two lines (`rql --help`, the skill's RQL reference). */
export const RQL_LANGUAGE = [
  'Stages: where, select a b, sort x [desc], first N, count, get <member>, set a = expr, call Method(args).',
  'Operators: == != < <= > >= and or not, contains, starts-with, ends-with, x in [a, b], $ is Light.',
];

/** The editor commands the root help and the skill feature. */
export const EDITOR_COMMANDS: readonly CommandRow[] = [
  ['rql', "rql '<query>'", 'read or change scenes, GameObjects, components, assets (RQL; recipes below)'],
  ['exec', "exec '<C# code>'", 'C# in the editor (the last expression is the result)'],
  ['tools', 'tools [name]', "the editor's AI tools, or one in detail; run-tool <name> --input '<json>' calls one"],
];

/** The code commands. */
export const CODE_COMMANDS: readonly CommandRow[] = [
  ['compile', 'compile', 'check that the scripts compile (file:line errors)'],
  ['test', 'test', 'run the Unity tests (no plugin needed)'],
];

/** How an editor command gets its editor (root help heading, skill opening). */
export const EDITOR_START = 'the first call starts a headless editor and keeps it warm; no open / status needed';

/** How to tell the plugin is missing from a project: `install-plugin` comes first. */
export const PLUGIN_MARKER = '`Packages/manifest.json` without `com.ivanmurzak.unity.mcp`';

/**
 * The rules an agent needs beyond the recipes (the skill's list). Each answers a stumble seen in agent transcripts.
 * Kept terse: the agent re-reads every byte of the skill on every later turn, and a rule worded as a warning invites
 * extra checks.
 */
export const RQL_RULES = [
  '`objects` lists only the open scenes (OpenScene first). A `set` / `call` is saved to disk at once.',
  'The output of `set` / `call` lists each change (old, new) and the files written: that is the check, no re-read needed.',
  'A wrong source or member fails with the valid names.',
];
