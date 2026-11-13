// `rql` — RQL text straight from the command line: which rql-* tool a query goes to, hints for the RQL slips agents
// make most, and which scene / asset files a change wrote (Unity saves every set / call). Never prints, never exits.

import * as fs from 'fs';
import * as path from 'path';

import {
  RQL_MUTATE_TOOL,
  RQL_QUERY_TOOL,
  RQL_SCHEMA_TOOL,
  decodeQueryFile,
  isRqlMutation,
  rqlLastVerb,
  rqlStages,
  rqlTextVerb,
  rqlToolCall,
  rqlToolFor,
  splitRqlStatements,
  stripRqlToolNames,
  stripToolName,
} from '@baizor/gamedev-cli-core';

export {
  RQL_MUTATE_TOOL,
  RQL_QUERY_TOOL,
  RQL_SCHEMA_TOOL,
  decodeQueryFile,
  isRqlMutation,
  rqlLastVerb,
  rqlStages,
  rqlTextVerb,
  rqlToolCall,
  rqlToolFor,
  splitRqlStatements,
  stripRqlToolNames,
  stripToolName,
};

/** `rql` without a query (cli-core's {@link rqlToolCall} returns it as the usage error). */
export const RQL_NO_QUERY_USAGE = 'No query given: rql \'objects | where name starts-with "Tree" | count\' (recipes: rql --help)';

/** `unity-account`: the Unity account the editor is signed in to (the plugin's `assetStore` source). */
export const UNITY_ACCOUNT_QUERY = 'assetStore | select signedIn userName';
/** `unity-account --sign-in`: opens Unity's sign-in in the editor; the user completes it there. */
export const UNITY_SIGN_IN_QUERY = 'assetStore | call SignIn()';
/** The answer when the editor's plugin predates the Unity account support. */
export const OLD_PLUGIN_ACCOUNT_HINT =
  "this project's AI Game Developer plugin has no Unity account support (assetStore source) yet: update it with unity-mcp-cli update";

/** The rql-* tool call of `unity-account`: read the account, or (`signIn`) open Unity's sign-in. */
export function unityAccountToolCall(signIn: boolean): { toolName: string; input: { query: string } } {
  return signIn
    ? { toolName: RQL_MUTATE_TOOL, input: { query: UNITY_SIGN_IN_QUERY } }
    : { toolName: RQL_QUERY_TOOL, input: { query: UNITY_ACCOUNT_QUERY } };
}

/** Operators agents bring from other languages, with RQL's spelling. */
const OPERATOR_HINTS: ReadonlyArray<readonly [RegExp, string]> = [
  [/^startswith$/i, 'RQL spells it starts-with: where name starts-with "Tree"'],
  [/^endswith$/i, 'RQL spells it ends-with: where name ends-with "_LOD0"'],
  [/^(like|ilike)$/i, 'RQL has no like: use starts-with / ends-with / contains, or matches "regex"'],
  [/^(includes|has)$/i, 'RQL spells it contains: where name contains "Tree"'],
  [/^(isnull|is_null)$/i, 'RQL spells it x is null / x is not null'],
  [/^(notin|not_in)$/i, 'RQL spells it not (x in [a, b])'],
  [/^(regex|regexp|match|rlike)$/i, 'RQL spells it matches "regex": where name matches "^Tree_[0-9]+$"'],
];

/**
 * A hint for a failed RQL tool (`toolName` rql-*) whose error is a familiar slip (an operator spelled as in another
 * language), else `undefined`.
 */
export function rqlErrorHint(toolName: string, message: string): string | undefined {
  if (!toolName.startsWith('rql-')) return undefined;
  if (/unknown source 'assetStore'|unknown method 'SignIn' on AssetStoreEditorApi/.test(message)) return OLD_PLUGIN_ACCOUNT_HINT;
  const token = /unexpected '([^']+)'|unknown method '([^']+)'/.exec(message);
  const word = token?.[1] ?? token?.[2];
  if (word) {
    for (const [pattern, hint] of OPERATOR_HINTS) if (pattern.test(word)) return hint;
  }
  return undefined;
}

/** The Unity files a save writes: scenes, prefabs and serialized assets. */
const SAVED_FILE = /\.(unity|prefab|asset|mat|controller|anim|overrideController|physicMaterial|lighting)$/i;

/**
 * The scene / asset / settings files under `<project>/Assets` and `<project>/ProjectSettings` modified at or after
 * `sinceMs`, relative to the project with forward slashes, sorted — what a change actually wrote.
 */
export function assetsWrittenSince(projectDir: string, sinceMs: number): string[] {
  const found: string[] = [];
  const walk = (dir: string): void => {
    let entries: fs.Dirent[];
    try {
      entries = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (!entry.name.startsWith('.')) walk(full);
      } else if (SAVED_FILE.test(entry.name)) {
        try {
          if (fs.statSync(full).mtimeMs >= sinceMs) found.push(path.relative(projectDir, full).split(path.sep).join('/'));
        } catch {
          /* removed meanwhile */
        }
      }
    }
  };
  walk(path.join(projectDir, 'Assets'));
  walk(path.join(projectDir, 'ProjectSettings'));
  return found.sort();
}

/**
 * The line a change prints: the files it wrote (the first `limit`, then how many more). Nothing for a `call` that wrote
 * nothing (OpenScene writes nothing on purpose); a `set` that wrote nothing says so.
 */
export function saveReceipt(written: readonly string[], verb: 'set' | 'call' | undefined, limit = 20): string | undefined {
  if (written.length === 0) return verb === 'set' ? 'No scene, asset or settings file of the project changed on disk.' : undefined;
  const more = written.length > limit ? `, +${written.length - limit} more` : '';
  return `Saved to disk: ${written.slice(0, limit).join(', ')}${more}`;
}
