// Library-safe `listTools`: the AI tools the project's Unity Editor offers, with their input schemas — read over the
// MCP protocol itself (`tools/list` on the server's `/mcp` endpoint), plus the hidden tools (callable by name) from the
// plugin's `unity-tool-list`. The result is cached in the project's session folder (`tools.json`, git-ignored) so an
// agent can read it while the editor is closed. No console output, no `process.exit`.

import * as fs from 'fs';
import * as path from 'path';
import { ensureAiGameDevGitignore, SESSION_DIR_NAME } from '@baizor/gamedev-cli-core';
import { runTool } from './run-tool.js';
import { readToolResponse } from './exec.js';
import type { RunToolOptions, RunToolResult } from './types.js';

export const TOOL_LIST_TOOL = 'unity-tool-list';

export interface ToolInfo {
  name: string;
  description: string;
  /** JSON Schema of the tool's input (`--input`). */
  inputSchema: Record<string, unknown>;
}

export interface ToolCatalog {
  /** Tools offered to MCP clients, with their schemas. */
  tools: ToolInfo[];
  /** Hidden tools: not offered to MCP clients, still callable by name with `run-tool`. */
  hidden: string[];
  /** ISO time the catalog was read from the editor. */
  readAt: string;
}

const CACHE_FILE_NAME = 'tools.json';

export function toolCachePath(projectPath: string): string {
  return path.join(projectPath, SESSION_DIR_NAME, CACHE_FILE_NAME);
}

/** The cached catalog, or undefined when absent or unreadable. */
export function readToolCache(projectPath: string): ToolCatalog | undefined {
  try {
    const parsed = JSON.parse(fs.readFileSync(toolCachePath(projectPath), 'utf-8')) as ToolCatalog;
    return Array.isArray(parsed.tools) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

export function writeToolCache(projectPath: string, catalog: ToolCatalog): void {
  try {
    fs.mkdirSync(path.dirname(toolCachePath(projectPath)), { recursive: true });
    fs.writeFileSync(toolCachePath(projectPath), JSON.stringify(catalog, null, 1));
    // Machine-local, like the session files: never committed.
    ensureAiGameDevGitignore(projectPath, [CACHE_FILE_NAME]);
  } catch {
    // A cache is a convenience: a read-only project still gets its live answer.
  }
}

/** The MCP endpoint of a server base URL (the cloud URL already is one). */
export function mcpEndpoint(baseUrl: string): string {
  const trimmed = baseUrl.replace(/\/$/, '');
  return trimmed.endsWith('/mcp') ? trimmed : `${trimmed}/mcp`;
}

/** The JSON-RPC message of a streamable-HTTP answer: plain JSON, or the `data:` line of an SSE event. */
export function parseMcpMessage(text: string): { result?: unknown; error?: { message?: string } } | undefined {
  const trimmed = text.trim();
  const candidates = trimmed.startsWith('{') ? [trimmed] : trimmed.split(/\r?\n/).filter(l => l.startsWith('data:')).map(l => l.slice(5).trim());
  for (const c of candidates) {
    try {
      return JSON.parse(c) as { result?: unknown; error?: { message?: string } };
    } catch {
      // next line
    }
  }
  return undefined;
}

/**
 * Names of hidden tools in `unity-tool-list`'s answer: a `Name|Visible` table (`package-add|false` rows), or
 * `{Name:package-add,Visible:false,...}` records when descriptions are included.
 */
export function parseHiddenToolNames(text: string): string[] {
  const rows = [...text.matchAll(/^([\w.-]+)\|false\s*$/gm)].map(m => m[1]);
  const records = [...text.matchAll(/\{Name:([\w.-]+),Visible:false\b/g)].map(m => m[1]);
  return [...new Set([...rows, ...records])];
}

export interface ListToolsOptions {
  baseUrl: string;
  token?: string;
  timeoutMs?: number;
  fetchImpl?: typeof fetch;
  invoke?: (options: RunToolOptions) => Promise<RunToolResult>;
}

export type ListToolsResult = { kind: 'success'; catalog: ToolCatalog } | { kind: 'failure'; message: string };

/** Read the catalog from a running server. */
export async function listTools(options: ListToolsOptions): Promise<ListToolsResult> {
  const fetchImpl = options.fetchImpl ?? fetch;
  const endpoint = mcpEndpoint(options.baseUrl);
  const headers: Record<string, string> = {
    'content-type': 'application/json',
    accept: 'application/json, text/event-stream',
    ...(options.token ? { authorization: `Bearer ${options.token}` } : {}),
  };
  const timeout = () => AbortSignal.timeout(options.timeoutMs ?? 30_000);
  try {
    const init = await fetchImpl(endpoint, {
      method: 'POST', headers, signal: timeout(),
      body: JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'initialize', params: { protocolVersion: '2025-03-26', capabilities: {}, clientInfo: { name: 'unity-mcp-cli', version: '1' } } }),
    });
    if (!init.ok) return { kind: 'failure', message: `MCP initialize failed: HTTP ${init.status} at ${endpoint}` };
    await init.text();
    const session = init.headers.get('mcp-session-id');
    const sessionHeaders = session ? { ...headers, 'mcp-session-id': session } : headers;
    await fetchImpl(endpoint, { method: 'POST', headers: sessionHeaders, signal: timeout(), body: JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) })
      .then(r => r.text()).catch(() => undefined);
    const listed = await fetchImpl(endpoint, { method: 'POST', headers: sessionHeaders, signal: timeout(), body: JSON.stringify({ jsonrpc: '2.0', id: 2, method: 'tools/list' }) });
    const message = parseMcpMessage(await listed.text());
    const tools = (message?.result as { tools?: Array<{ name: string; description?: string; inputSchema?: Record<string, unknown> }> } | undefined)?.tools;
    if (!listed.ok || !tools) {
      return { kind: 'failure', message: message?.error?.message ?? `MCP tools/list failed: HTTP ${listed.status} at ${endpoint}` };
    }
    if (session) {
      await fetchImpl(endpoint, { method: 'DELETE', headers: sessionHeaders, signal: timeout() }).then(r => r.text()).catch(() => undefined);
    }

    const invoke = options.invoke ?? runTool;
    const all = await invoke({ toolName: TOOL_LIST_TOOL, url: options.baseUrl, ...(options.token ? { token: options.token } : {}), input: {}, timeoutMs: options.timeoutMs ?? 30_000 });
    const hidden = all.kind === 'success' ? parseHiddenToolNames(readToolResponse(all.data).text) : [];

    return {
      kind: 'success',
      catalog: {
        tools: tools.map(t => ({ name: t.name, description: t.description ?? '', inputSchema: t.inputSchema ?? {} })).sort((a, b) => a.name.localeCompare(b.name)),
        hidden: hidden.filter(n => !tools.some(t => t.name === n)).sort(),
        readAt: new Date().toISOString(),
      },
    };
  } catch (err) {
    return { kind: 'failure', message: `Could not read the tool list at ${endpoint}: ${err instanceof Error ? err.message : String(err)}` };
  }
}

/** The first sentence of a description (at most `max` characters), for one-line listings. */
export function summary(description: string, max = 160): string {
  const flat = description.replace(/\s+/g, ' ').trim();
  const sentence = /^(.+?[.!?])(\s|$)/.exec(flat)?.[1] ?? flat;
  return sentence.length > max ? `${sentence.slice(0, max - 1)}…` : sentence;
}

/** `name*: type` for each input property (`*` = required). */
export function describeInputs(schema: Record<string, unknown>): string[] {
  const props = (schema.properties ?? {}) as Record<string, { type?: unknown; description?: string; enum?: unknown[] }>;
  const required = new Set((schema.required as string[] | undefined) ?? []);
  return Object.entries(props).map(([name, p]) => {
    const type = p.enum ? p.enum.map(v => JSON.stringify(v)).join('|') : Array.isArray(p.type) ? p.type.join('|') : String(p.type ?? 'any');
    return `${name}${required.has(name) ? '*' : ''}: ${type}${p.description ? ` — ${summary(p.description, 140)}` : ''}`;
  });
}
