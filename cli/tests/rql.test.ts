import { afterEach, describe, expect, it } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { assetsWrittenSince, isRqlMutation, rqlErrorHint, rqlStages, stripToolName } from '../src/lib/rql.js';

const dirs: string[] = [];
afterEach(() => {
  for (const dir of dirs.splice(0)) fs.rmSync(dir, { recursive: true, force: true });
});

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
    expect(rqlErrorHint("error syntax-error: unexpected 'startsWith' in 'where' stage at 21")).toMatch(/starts-with/);
    expect(rqlErrorHint("error syntax-error: unexpected 'like' in 'where' stage")).toMatch(/no like/);
    expect(rqlErrorHint("error unknown-source: unknown source 'object'")).toBeUndefined();
  });
});

describe('assetsWrittenSince', () => {
  it('lists the scene / asset files under Assets written since a time', () => {
    const project = fs.mkdtempSync(path.join(os.tmpdir(), 'rql-unity-'));
    dirs.push(project);
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
