import { describe, it, expect, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  describeInputs, listTools, mcpEndpoint, parseHiddenToolNames, parseMcpMessage, readToolCache, summary, writeToolCache,
} from '../src/lib/list-tools.js';
import { resolveEditorConnection } from '../src/utils/connection.js';
import type { RunToolResult } from '../src/lib/types.js';

describe('list-tools helpers', () => {
  it('finds the MCP endpoint of a base URL', () => {
    expect(mcpEndpoint('http://localhost:1/')).toBe('http://localhost:1/mcp');
    expect(mcpEndpoint('https://ai-game.dev/mcp')).toBe('https://ai-game.dev/mcp');
  });

  it('reads a JSON-RPC message from JSON or an SSE event', () => {
    expect(parseMcpMessage('{"result":{"a":1}}')).toEqual({ result: { a: 1 } });
    expect(parseMcpMessage('event: message\ndata: {"result":{"b":2}}\n\n')).toEqual({ result: { b: 2 } });
    expect(parseMcpMessage('garbage')).toBeUndefined();
  });

  it('reads hidden tools from both answer shapes of unity-tool-list (as the plugin sent them live)', () => {
    expect(parseHiddenToolNames('19 results\nName|Visible\npackage-add|false\nrql-query|true\nscreenshot-camera|false\n'))
      .toEqual(['package-add', 'screenshot-camera']);
    expect(parseHiddenToolNames('{Name:package-add,Visible:false,Description:"x"}\n{Name:rql-query,Visible:true}')).toEqual(['package-add']);
  });

  it('summarizes descriptions and inputs', () => {
    expect(summary('Change the scene. All-or-nothing: more.')).toBe('Change the scene.');
    expect(describeInputs({ properties: { query: { type: 'string', description: 'The RQL query.' }, n: { type: 'integer' } }, required: ['query'] }))
      .toEqual(['query*: string — The RQL query.', 'n: integer']);
  });

  it('caches the catalog in the project and keeps it out of git', () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-tools-cache-'));
    try {
      writeToolCache(dir, { tools: [{ name: 'a', description: '', inputSchema: {} }], hidden: [], readAt: 'now' });
      expect(readToolCache(dir)?.tools[0].name).toBe('a');
      expect(fs.readdirSync(dir)).toEqual([expect.stringMatching(/^\.ai-game-dev$/)]);
      const ignore = fs.readFileSync(path.join(dir, fs.readdirSync(dir)[0], '.gitignore'), 'utf-8');
      expect(ignore).toContain('tools.json');
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});

describe('listTools', () => {
  it('reads tools/list over MCP and the hidden names from unity-tool-list', async () => {
    const calls: string[] = [];
    const fetchImpl = vi.fn(async (_url: string | URL | Request, init?: RequestInit) => {
      const body = init?.body ? JSON.parse(String(init.body)) as { method?: string } : {};
      calls.push(body.method ?? String(init?.method));
      const headers = new Headers({ 'mcp-session-id': 's1' });
      if (body.method === 'tools/list') {
        return new Response('event: message\ndata: {"result":{"tools":[{"name":"rql-query","description":"Read.","inputSchema":{"type":"object"}}]}}\n', { headers });
      }
      return new Response('{"result":{}}', { headers });
    });
    const invoke = vi.fn(async (): Promise<RunToolResult> => ({
      kind: 'success', success: true, endpoint: 'x', httpStatus: 200,
      data: { status: 'success', content: [{ type: 'text', text: 'Name|Visible\nrql-query|true\npackage-add|false' }] },
    }));
    const result = await listTools({ baseUrl: 'http://localhost:1', fetchImpl: fetchImpl as unknown as typeof fetch, invoke });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') return;
    expect(result.catalog.tools.map(t => t.name)).toEqual(['rql-query']);
    expect(result.catalog.hidden).toEqual(['package-add']);
    expect(calls).toEqual(['initialize', 'notifications/initialized', 'tools/list', 'DELETE']);
  });

  it('reports an unreachable server', async () => {
    const fetchImpl = vi.fn(async () => { throw new Error('ECONNREFUSED'); });
    expect(await listTools({ baseUrl: 'http://localhost:1', fetchImpl: fetchImpl as unknown as typeof fetch })).toMatchObject({ kind: 'failure' });
  });
});

describe('resolveEditorConnection', () => {
  function project(config: object): string {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-editor-conn-'));
    fs.mkdirSync(path.join(dir, 'UserSettings'));
    fs.writeFileSync(path.join(dir, 'UserSettings', 'AI-Game-Developer-Config.json'), JSON.stringify(config));
    return dir;
  }

  it('Cloud mode without a sign-in falls back to the local server (and its config token)', async () => {
    const dir = project({ connectionMode: 'Cloud', token: 'local-tok' });
    try {
      const conn = await resolveEditorConnection(dir, {}, { readCloudToken: () => undefined, credentialState: () => 'missing', userEditorOpen: () => false });
      expect(conn.localFallback).toBe(true);
      expect(conn.url).toMatch(/^http:\/\/localhost:\d+$/);
      expect(conn.overrideUrl).toBe(conn.url);
      expect(conn.token).toBe('local-tok');
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('a project the plugin has not configured yet is in its default Cloud mode: without a sign-in it falls back too', async () => {
    // No config file until the plugin first loads; an older file may lack the field.
    const fresh = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-editor-conn-fresh-'));
    const noMode = project({ token: 'local-tok' });
    try {
      for (const dir of [fresh, noMode]) {
        const conn = await resolveEditorConnection(dir, {}, { readCloudToken: () => undefined, credentialState: () => 'missing', userEditorOpen: () => false });
        expect(conn.localFallback).toBe(true);
        expect(conn.url).toMatch(/^http:\/\/localhost:\d+$/);
        expect(conn.overrideUrl).toBe(conn.url);
      }
      const signedIn = await resolveEditorConnection(fresh, {}, { readCloudToken: () => 'tok', credentialState: () => 'ok' });
      expect(signedIn).toMatchObject({ url: 'https://ai-game.dev/mcp', token: 'tok', localFallback: false });
    } finally {
      fs.rmSync(fresh, { recursive: true, force: true });
      fs.rmSync(noMode, { recursive: true, force: true });
    }
  });

  it('a sign-in or an explicit --url keeps the resolved connection', async () => {
    const dir = project({ connectionMode: 'Cloud' });
    try {
      expect((await resolveEditorConnection(dir, {}, { readCloudToken: () => 'cloud-tok' })).localFallback).toBe(false);
      const explicit = await resolveEditorConnection(dir, { url: 'http://localhost:9' }, { readCloudToken: () => undefined });
      expect(explicit).toMatchObject({ localFallback: false, url: 'http://localhost:9', overrideUrl: 'http://localhost:9' });
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});

describe('run-tool --arg', () => {
  it('merges key=value pairs over the JSON input, parsing JSON values', async () => {
    const { mergeArgs } = await import('../src/utils/input.js');
    expect(JSON.parse(mergeArgs('{"a":1}', ['query=objects | where name == "Crate" | select name', 'dry_run=true', 'n=3'])))
      .toEqual({ a: 1, query: 'objects | where name == "Crate" | select name', dry_run: true, n: 3 });
    expect(JSON.parse(mergeArgs('{}', ['text=a=b']))).toEqual({ text: 'a=b' });
    expect(() => mergeArgs('{}', ['novalue'])).toThrow(/key=value/);
    expect(() => mergeArgs('[1]', ['a=1'])).toThrow(/JSON object/);
  });
});

describe('parseToolRecords', () => {
  it('reads the record lines unity-tool-list sends with descriptions and inputs (sample from a live editor)', async () => {
    const { parseToolRecords } = await import('../src/lib/list-tools.js');
    const text = '19 results\n'
      + '{Name:package-add,Visible:false,Description:"Install a package from the Unity Package Manager registry, Git URL, or local path. Use \'package-search\' first.",Inputs:[{Name:packageId,Description:null}]}\n'
      + '{Name:rql-query,Visible:true,Description:"Run a read-only RQL query. Say \\"hi\\".",Inputs:[{Name:query,Description:"The RQL query."}]}';
    const records = parseToolRecords(text);
    expect(records.map(r => [r.name, r.visible])).toEqual([['package-add', false], ['rql-query', true]]);
    expect(records[1].description).toBe('Run a read-only RQL query. Say "hi".');
    expect(records[1].inputSchema).toEqual({ type: 'object', properties: { query: { description: 'The RQL query.' } } });
    expect(records[0].inputSchema).toEqual({ type: 'object', properties: { packageId: {} } });
  });

  it('through the cloud asks the plugin, not MCP tools/list', async () => {
    const fetchImpl = vi.fn();
    const invoke = vi.fn(async (): Promise<RunToolResult> => ({
      kind: 'success', success: true, endpoint: 'x', httpStatus: 200,
      data: { status: 'success', content: [{ type: 'text', text: '{Name:rql-query,Visible:true,Description:"Q.",Inputs:[]}\n{Name:package-add,Visible:false,Description:"P.",Inputs:[]}' }] },
    }));
    const result = await listTools({ baseUrl: 'https://ai-game.dev/mcp', fetchImpl: fetchImpl as unknown as typeof fetch, invoke });
    expect(fetchImpl).not.toHaveBeenCalled();
    expect(result).toMatchObject({ kind: 'success', catalog: { tools: [{ name: 'rql-query' }], hidden: ['package-add'] } });
  });
});

describe('resolveEditorConnection with an existing sign-in', () => {
  it('never switches a signed-in machine to the local server, even when its token cannot be used now', async () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-editor-conn-signed-'));
    try {
      fs.mkdirSync(path.join(dir, 'UserSettings'));
      fs.writeFileSync(path.join(dir, 'UserSettings', 'AI-Game-Developer-Config.json'), JSON.stringify({ connectionMode: 'Cloud' }));
      for (const state of ['ok', 'unreadable'] as const) {
        // `ok` with no usable token: expired/revoked family or a busy store.
        const conn = await resolveEditorConnection(dir, {}, { readCloudToken: () => undefined, credentialState: () => state, userEditorOpen: () => false });
        expect(conn.localFallback).toBe(false);
        expect(conn.url).toBe('https://ai-game.dev/mcp');
        expect(conn.signInRequired).toMatch(/signed in to ai-game.dev/);
      }
      // A working sign-in goes to the cloud with its token.
      const working = await resolveEditorConnection(dir, {}, { readCloudToken: () => 'tok', credentialState: () => 'ok' });
      expect(working).toMatchObject({ url: 'https://ai-game.dev/mcp', token: 'tok', localFallback: false });
      expect(working.signInRequired).toBeUndefined();
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});

describe('resolveEditorConnection with a user editor open', () => {
  it('requires a sign-in instead of falling back (that editor talks to the cloud)', async () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-editor-conn-open-'));
    try {
      fs.mkdirSync(path.join(dir, 'UserSettings'));
      fs.writeFileSync(path.join(dir, 'UserSettings', 'AI-Game-Developer-Config.json'), JSON.stringify({ connectionMode: 'Cloud' }));
      const conn = await resolveEditorConnection(dir, {}, { readCloudToken: () => undefined, credentialState: () => 'missing', userEditorOpen: () => true });
      expect(conn.localFallback).toBe(false);
      expect(conn.signInRequired).toMatch(/connected to ai-game.dev/);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});
