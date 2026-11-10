// `rql` — RQL text straight from the command line: which rql-* tool a query goes to, hints for the RQL slips agents
// make most, and which scene / asset files a change wrote (Unity saves every set / call). Never prints, never exits.

import * as fs from 'fs';
import * as path from 'path';

export const RQL_QUERY_TOOL = 'rql-query';
export const RQL_MUTATE_TOOL = 'rql-mutate';
export const RQL_SCHEMA_TOOL = 'rql-schema';

/** The query's top-level stages (split on `|` outside strings, parentheses, brackets and braces), trimmed. */
export function rqlStages(query: string): string[] {
  const stages: string[] = [];
  let depth = 0;
  let quote: string | undefined;
  let start = 0;
  for (let i = 0; i < query.length; i++) {
    const ch = query[i];
    if (quote) {
      if (ch === '\\') i++;
      else if (ch === quote) quote = undefined;
    } else if (ch === '"' || ch === '\'') {
      quote = ch;
    } else if (ch === '(' || ch === '[' || ch === '{') {
      depth++;
    } else if (ch === ')' || ch === ']' || ch === '}') {
      depth = Math.max(0, depth - 1);
    } else if (ch === '|' && depth === 0) {
      stages.push(query.slice(start, i).trim());
      start = i + 1;
    }
  }
  stages.push(query.slice(start).trim());
  return stages;
}

/** A query whose last stage is `set` or `call` changes the editor (rql-mutate); any other only reads (rql-query). */
export function isRqlMutation(query: string): boolean {
  const stages = rqlStages(query);
  return /^(set|call)\b/i.test(stages[stages.length - 1] ?? '');
}

/**
 * The query without a leading tool name: tool texts write "rql-query console | ...", and agents paste that whole
 * into `rql`.
 */
export function stripToolName(query: string): string {
  return query.replace(/^\s*rql-(query|mutate)\s+/, '');
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

/** A hint for an RQL error whose cause is a familiar slip (a camelCase operator, SQL `like`), else `undefined`. */
export function rqlErrorHint(message: string): string | undefined {
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
 * The scene / asset files under `<project>/Assets` modified at or after `sinceMs`, relative to the project with
 * forward slashes, sorted — what a save call actually wrote. At most `limit`.
 */
export function assetsWrittenSince(projectDir: string, sinceMs: number, limit = 20): string[] {
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
  return found.sort().slice(0, limit);
}
