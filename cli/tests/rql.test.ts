import { afterEach, describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { makeTempDir, removeTempDirs } from './helpers/temp-dir.js';
import {
  RQL_NO_QUERY_USAGE,
  assetsWrittenSince,
  decodeQueryFile,
  isRqlMutation,
  rqlErrorHint,
  rqlLastVerb,
  rqlStages,
  rqlTextVerb,
  rqlToolCall as coreRqlToolCall,
  rqlToolFor,
  saveReceipt,
  splitRqlStatements,
  stripRqlToolNames,
  stripToolName,
} from '../src/lib/rql.js';

/** The call `rql` makes: cli-core's routing with this CLI's own usage line. */
const rqlToolCall = (query: string | undefined, options: Parameters<typeof coreRqlToolCall>[1]) =>
  coreRqlToolCall(query, options, RQL_NO_QUERY_USAGE);

afterEach(removeTempDirs);

describe('rql query routing', () => {
  it('splits stages only on top-level pipes', () => {
    expect(rqlStages('objects | where name == "a|b" | select [1, (2 | 3)]')).toEqual(['objects', 'where name == "a|b"', 'select [1, (2 | 3)]']);
  });

  it('sends a query ending in set / call to rql-mutate, any other to rql-query', () => {
    expect(isRqlMutation('objects | where name starts-with "Tree" | set active = false')).toBe(true);
    expect(isRqlMutation('editor | call OpenScene("Assets/Scenes/A.unity")')).toBe(true);
    expect(isRqlMutation('objects | where name starts-with "Tree" | count')).toBe(false);
    expect(isRqlMutation('objects | where name == "set | call" | select name')).toBe(false);
  });

  it('drops a pasted tool name only at the start', () => {
    expect(stripToolName('rql-query console | count')).toBe('console | count');
    expect(stripToolName('objects | where name == "rql-query"')).toBe('objects | where name == "rql-query"');
  });
});

describe('rqlErrorHint', () => {
  it('names the RQL spelling of an operator from another language', () => {
    expect(rqlErrorHint('rql-query', "error syntax-error: unexpected 'startsWith' in 'where' stage at 21")).toMatch(/starts-with/);
    expect(rqlErrorHint('rql-query', "error syntax-error: unexpected 'like' in 'where' stage")).toMatch(/no like/);
    expect(rqlErrorHint('rql-query', "error unknown-source: unknown source 'object'")).toBeUndefined();
    expect(rqlErrorHint('script-execute', "unexpected 'startsWith'")).toBeUndefined();
  });
});

describe('assetsWrittenSince', () => {
  it('lists the scene / asset files under Assets written since a time', () => {
    const project = makeTempDir('rql-unity-');
    const scenes = path.join(project, 'Assets', 'Scenes');
    fs.mkdirSync(scenes, { recursive: true });
    const old = path.join(scenes, 'Old.unity');
    fs.writeFileSync(old, '');
    const past = new Date(Date.now() - 60_000);
    fs.utimesSync(old, past, past);
    const since = Date.now() - 1000;
    fs.writeFileSync(path.join(scenes, 'Arena.unity'), '');
    fs.writeFileSync(path.join(project, 'Assets', 'Red.mat'), '');
    fs.writeFileSync(path.join(scenes, 'Arena.unity.meta'), '');
    expect(assetsWrittenSince(project, since)).toEqual(['Assets/Red.mat', 'Assets/Scenes/Arena.unity']);
  });
});

describe('rqlToolCall', () => {
  it('routes a query, and refuses a query with --schema or a read-only query with mutate flags', () => {
    expect(rqlToolCall('objects | count', {})).toEqual({ toolName: 'rql-query', input: { query: 'objects | count' } });
    expect(rqlToolCall('objects | set active = false', { dryRun: true, maxTargets: 300 }))
      .toEqual({ toolName: 'rql-mutate', input: { query: 'objects | set active = false', dry_run: true, max_targets: 300 } });
    expect(rqlToolCall(undefined, { schema: 'Light' })).toEqual({ toolName: 'rql-schema', input: { type: 'Light' } });
    expect(rqlToolCall('objects | count', { schema: true })).toHaveProperty('usage');
    expect(rqlToolCall(undefined, { schema: 'objects | count' })).toHaveProperty('usage');
    expect(rqlToolCall('objects | count', { dryRun: true })).toHaveProperty('usage');
    expect(rqlToolCall(undefined, {})).toHaveProperty('usage');
  });

  it('reads || as or and decodes a UTF-16 query file', () => {
    expect(rqlLastVerb('objects | where name == "a" || name == "b" | set active = false')).toBe('set');
    const text = 'objects | count';
    expect(decodeQueryFile(Buffer.concat([Buffer.from([0xff, 0xfe]), Buffer.from(text, 'utf16le')]))).toBe(text);
  });

  it('treats backtick identifiers as opaque', () => {
    expect(rqlLastVerb("objects | where `Bob's` == 1 | set active = false")).toBe('set');
  });
});

describe('several statements (;)', () => {
  it('splits at ; outside strings, quoted identifiers and brackets, dropping blank statements', () => {
    expect(splitRqlStatements('objects | count; roots | count')).toEqual(['objects | count', 'roots | count']);
    expect(splitRqlStatements('objects | where name == "a;b" | count;')).toEqual(['objects | where name == "a;b" | count']);
    expect(splitRqlStatements("objects | where `x;y` == 1 | count; ;")).toEqual(['objects | where `x;y` == 1 | count']);
    expect(splitRqlStatements('editor | call F({a: "1;2"}, [1; 2]); objects | count')).toEqual(['editor | call F({a: "1;2"}, [1; 2])', 'objects | count']);
    expect(splitRqlStatements(' ; ')).toEqual(['']);   // as RQL: the first (blank) segment, reported as an empty query
  });

  it('routes to rql-mutate when any statement ends in set / call', () => {
    expect(rqlToolCall('objects | where name == "A" | set active = false; objects | count', {}))
      .toEqual({ toolName: 'rql-mutate', input: { query: 'objects | where name == "A" | set active = false; objects | count' } });
    expect(rqlToolCall('editor | call CreateGameObject("A"); editor | call CreateGameObject("B")', { dryRun: true }))
      .toEqual({ toolName: 'rql-mutate', input: { query: 'editor | call CreateGameObject("A"); editor | call CreateGameObject("B")', dry_run: true } });
    expect(rqlToolCall('objects | count; roots | count', {})).toEqual({ toolName: 'rql-query', input: { query: 'objects | count; roots | count' } });
    expect(rqlToolCall('objects | count; roots | count', { dryRun: true })).toHaveProperty('usage');
    expect(rqlToolFor('objects | where name == "set; call" | count')).toBe('rql-query');
  });

  it('strips a pasted tool name from every statement', () => {
    expect(stripRqlToolNames('rql-query objects | count; rql-query roots | count')).toBe('objects | count; roots | count');
    expect(stripRqlToolNames('rql-mutate objects | where name == "a; b" | set active = false')).toBe('objects | where name == "a; b" | set active = false');
  });

  it('reports call before set for the save receipt', () => {
    expect(rqlTextVerb('objects | set active = false; editor | call SaveScenes()')).toBe('call');
    expect(rqlTextVerb('objects | count; objects | set active = false')).toBe('set');
    expect(rqlTextVerb('objects | count; roots | count')).toBeUndefined();
  });
});

describe('saveReceipt', () => {
  it('names the files written; a call that wrote nothing says nothing, a set says so', () => {
    expect(saveReceipt(['Assets/A.unity'], 'set')).toBe('Saved to disk: Assets/A.unity');
    expect(saveReceipt(['a', 'b', 'c'], 'set', 2)).toBe('Saved to disk: a, b, +1 more');
    expect(saveReceipt([], 'call')).toBeUndefined();
    expect(saveReceipt([], 'set')).toMatch(/No scene, asset or settings file/);
  });
});
