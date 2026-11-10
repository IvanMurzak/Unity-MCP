// `rql` — RQL text straight from the command line: which rql-* tool a query goes to, hints for the RQL slips agents
// make most, and which scene / asset files a change wrote (Unity saves every set / call). Never prints, never exits.

import * as fs from 'fs';
import * as path from 'path';

export const RQL_QUERY_TOOL = 'rql-query';
export const RQL_MUTATE_TOOL = 'rql-mutate';
export const RQL_SCHEMA_TOOL = 'rql-schema';

/**
 * The query's top-level stages (split on `|` outside strings, `backtick` identifiers, parentheses, brackets and
 * braces; `||` is RQL's or, not a stage break), trimmed.
 */
export function rqlStages(query: string): string[] {
  const stages: string[] = [];
  let depth = 0;
  let quote: string | undefined;
  let start = 0;
  for (let i = 0; i < query.length; i++) {
    const ch = query[i];
    if (quote) {
      if (ch === '\\' && quote !== '`') i++;
      else if (ch === quote) quote = undefined;
    } else if (ch === '"' || ch === '\'' || ch === '`') {
      quote = ch;
    } else if (ch === '(' || ch === '[' || ch === '{') {
      depth++;
    } else if (ch === ')' || ch === ']' || ch === '}') {
      depth = Math.max(0, depth - 1);
    } else if (ch === '|' && query[i + 1] === '|') {
      i++;
    } else if (ch === '|' && depth === 0) {
      stages.push(query.slice(start, i).trim());
      start = i + 1;
    }
  }
  stages.push(query.slice(start).trim());
  return stages;
}

/** `set` or `call` when the query's last stage is one (it changes the editor), else `undefined` (it only reads). */
export function rqlLastVerb(query: string): 'set' | 'call' | undefined {
  const stages = rqlStages(query);
  return /^(set|call)\b/i.exec(stages[stages.length - 1] ?? '')?.[1].toLowerCase() as 'set' | 'call' | undefined;
}

/** A query whose last stage is `set` or `call` changes the editor (rql-mutate); any other only reads (rql-query). */
export function isRqlMutation(query: string): boolean {
  return rqlLastVerb(query) !== undefined;
}

/**
 * The query without a leading tool name: tool texts write "rql-query console | ...", and agents paste that whole
 * into `rql`.
 */
export function stripToolName(query: string): string {
  return query.replace(/^\s*rql-(query|mutate)\s+/, '');
}

/** A query file's text: UTF-8, or UTF-16 with a byte-order mark (Windows PowerShell 5.1 writes `>` files so). */
export function decodeQueryFile(bytes: Buffer): string {
  if (bytes[0] === 0xff && bytes[1] === 0xfe) return bytes.subarray(2).toString('utf16le');
  if (bytes[0] === 0xfe && bytes[1] === 0xff) return Buffer.from(bytes.subarray(2)).swap16().toString('utf16le');
  return bytes.toString('utf-8').replace(/^﻿/, '');
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
  if (!isRqlMutation(query)) {
    if (dryRun || maxTargets !== undefined) return { usage: '--dry-run and --max-targets are for a query ending in set / call.' };
    return { toolName: RQL_QUERY_TOOL, input: { query } };
  }
  return { toolName: RQL_MUTATE_TOOL, input: { query, ...(dryRun ? { dry_run: true } : {}), ...(maxTargets !== undefined ? { max_targets: maxTargets } : {}) } };
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
