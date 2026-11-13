// `rql` — RQL text straight from the command line: which rql-* tool a query goes to, hints for the RQL slips agents
// make most, and which scene / asset files a change wrote (Unity saves every set / call). Never prints, never exits.

import * as fs from 'fs';
import * as path from 'path';

import { RQL_MUTATE_TOOL, RQL_QUERY_TOOL, RQL_SCHEMA_TOOL, decodeQueryFile, isRqlMutation, rqlLastVerb, rqlStages, stripToolName } from '@baizor/gamedev-cli-core';

export { RQL_MUTATE_TOOL, RQL_QUERY_TOOL, RQL_SCHEMA_TOOL, decodeQueryFile, isRqlMutation, rqlLastVerb, rqlStages, stripToolName };

/**
 * The statements of RQL text: split at each `;` outside strings, quoted identifiers and brackets, blank ones dropped,
 * as RQL itself splits them (spec §14). Text without a separator is one statement.
 */
export function rqlStatements(text: string): string[] {
  const statements: string[] = [];
  let depth = 0;
  let quote: string | undefined;
  let start = 0;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quote) {
      if (ch === '\\' && quote !== '`') i++;
      else if (ch === quote) quote = undefined;
    } else if (ch === '"' || ch === "'" || ch === '`') {
      quote = ch;
    } else if (ch === '(' || ch === '[' || ch === '{') {
      depth++;
    } else if (ch === ')' || ch === ']' || ch === '}') {
      depth = Math.max(0, depth - 1);
    } else if (ch === ';' && depth === 0) {
      statements.push(text.slice(start, i).trim());
      start = i + 1;
    }
  }
  statements.push(text.slice(start).trim());
  if (statements.length === 1) return statements;
  const nonBlank = statements.filter((s) => s !== '');
  return nonBlank.length > 0 ? nonBlank : statements.slice(0, 1);
}

/**
 * The text without a leading tool name on any statement (tool texts write "rql-query <source> | ...", and agents paste
 * that, once per statement too). One statement comes back as {@link stripToolName} returns it.
 */
export function stripToolNames(text: string): string {
  const statements = rqlStatements(text);
  return statements.length === 1 ? stripToolName(text) : statements.map(stripToolName).join('; ');
}

/**
 * The write of RQL text for the save receipt: `call` when a statement ends in call (a call may write nothing on
 * purpose), else `set` when one ends in set, else undefined (a read).
 */
export function rqlTextVerb(text: string): 'set' | 'call' | undefined {
  const verbs = rqlStatements(text).map(rqlLastVerb);
  return verbs.includes('call') ? 'call' : verbs.includes('set') ? 'set' : undefined;
}

/**
 * RQL text that changes the editor (rql-mutate): any statement ends in set / call. rql-mutate refuses text in which
 * another statement only reads, so that mix fails with RQL's own message instead of running its reads.
 */
export function isRqlChange(text: string): boolean {
  return rqlTextVerb(text) !== undefined;
}

export interface RqlCallOptions {
  /** `--schema [type]`: true without a type. */
  schema?: string | boolean;
  dryRun?: boolean;
  maxTargets?: number;
}

/** The rql-* tool and its input for a command-line query, or why the command line is wrong. */
export function rqlToolCall(query: string | undefined, options: RqlCallOptions):
  { toolName: string; input: Record<string, unknown> } | { usage: string } {
  const { schema, dryRun, maxTargets } = options;
  if (schema !== undefined) {
    // `rql --schema 'objects | count'` hands the query to --schema as its value.
    if (query !== undefined || (typeof schema === 'string' && /[|\s]/.test(schema.trim()))) {
      return { usage: 'Pass either a query or --schema [type] (a type name), not both.' };
    }
    return { toolName: RQL_SCHEMA_TOOL, input: typeof schema === 'string' ? { type: schema } : {} };
  }
  if (query === undefined || query.trim() === '') {
    return { usage: 'No query given: rql \'objects | where name starts-with "Tree" | count\' (recipes: rql --help)' };
  }
  if (!isRqlChange(query)) {
    if (dryRun || maxTargets !== undefined) return { usage: '--dry-run and --max-targets are for a query ending in set / call.' };
    return { toolName: RQL_QUERY_TOOL, input: { query } };
  }
  return { toolName: RQL_MUTATE_TOOL, input: { query, ...(dryRun ? { dry_run: true } : {}), ...(maxTargets !== undefined ? { max_targets: maxTargets } : {}) } };
}

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
