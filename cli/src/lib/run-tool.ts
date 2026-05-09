// Library-safe `runTool` / `runSystemTool` implementations.
//
// Constraints (same contract as the rest of `lib/*.ts`):
// - No commander, no spinners, no process.exit, no console output.
// - Errors are returned in `{ kind: 'failure', success: false, ... }`,
//   never thrown past the public boundary.
// - Connection resolution mirrors the CLI's `resolveConnection` but
//   uses a library-safe variant — the CLI helper calls `process.exit`
//   when the project path is missing or invalid, which would crash a
//   library consumer.

import * as fs from 'fs';
import { readConfig, resolveConnectionFromConfig } from '../utils/config.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { requireProjectPath } from './validation.js';
import type {
  RunToolFailure,
  RunToolFailureReason,
  RunToolOptions,
  RunToolResult,
  RunToolSuccess,
} from './types.js';

const DEFAULT_TIMEOUT_MS = 60_000;

/**
 * Invoke a regular MCP tool over the Unity plugin's HTTP API.
 *
 * Mirrors `unity-mcp-cli run-tool <name>`: same URL/token resolution
 * priority (explicit override → project config → deterministic port)
 * and the same `POST /api/tools/{name}` endpoint shape, but without
 * the CLI's terminal output and `process.exit` paths.
 */
export async function runTool(opts: RunToolOptions): Promise<RunToolResult> {
  return invokeTool('/api/tools', opts);
}

/**
 * Invoke a system tool (internal tool not exposed to MCP clients) over
 * the Unity plugin's HTTP API. Mirrors `unity-mcp-cli run-system-tool
 * <name>` and posts to `/api/system-tools/{name}`.
 */
export async function runSystemTool(opts: RunToolOptions): Promise<RunToolResult> {
  return invokeTool('/api/system-tools', opts);
}

async function invokeTool(routePrefix: string, opts: RunToolOptions): Promise<RunToolResult> {
  const validation = validateOptions(opts);
  if (validation.kind === 'failure') return validation;

  const resolved = resolveConnection(opts);
  if (resolved.kind === 'failure') return resolved;
  const { url, token } = resolved;

  const body = serializeInput(opts.input);
  if ('error' in body) {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: body.error.message,
      error: body.error,
    });
  }

  const endpoint = `${url}${routePrefix}/${encodeURIComponent(opts.toolName)}`;

  const fetchImpl = opts.fetchImpl ?? globalThis.fetch;
  const timeoutMs =
    typeof opts.timeoutMs === 'number' && opts.timeoutMs > 0 ? opts.timeoutMs : DEFAULT_TIMEOUT_MS;

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  const externalAbort = (): void => controller.abort();
  if (opts.signal) {
    if (opts.signal.aborted) controller.abort();
    else opts.signal.addEventListener('abort', externalAbort, { once: true });
  }

  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  try {
    const response = await fetchImpl(endpoint, {
      method: 'POST',
      headers,
      body: body.json,
      signal: controller.signal,
    });

    const text = await safeReadText(response);
    const data = parseJsonOrText(text);

    if (!response.ok) {
      return makeFailure({
        endpoint,
        reason: 'http-error',
        httpStatus: response.status,
        data,
        message: `HTTP ${response.status} ${response.statusText || ''}`.trim(),
      });
    }

    const success: RunToolSuccess = {
      kind: 'success',
      success: true,
      endpoint,
      httpStatus: response.status,
      data,
    };
    return success;
  } catch (err) {
    return classifyFetchError(err, endpoint, timeoutMs);
  } finally {
    clearTimeout(timer);
    opts.signal?.removeEventListener('abort', externalAbort);
  }
}

function validateOptions(opts: RunToolOptions): { kind: 'success' } | RunToolFailure {
  if (!opts || typeof opts !== 'object') {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: 'options object is required.',
    });
  }
  if (typeof opts.toolName !== 'string' || opts.toolName.trim().length === 0) {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: 'toolName is required and must be a non-empty string.',
    });
  }
  if (
    (opts.url === undefined || opts.url.length === 0) &&
    (typeof opts.unityProjectPath !== 'string' || opts.unityProjectPath.trim().length === 0)
  ) {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: 'Either unityProjectPath or url must be provided.',
    });
  }
  return { kind: 'success' };
}

function resolveConnection(
  opts: RunToolOptions,
): { kind: 'success'; url: string; token: string | undefined } | RunToolFailure {
  if (opts.url) {
    const url = opts.url.replace(/\/$/, '');
    return { kind: 'success', url, token: opts.token };
  }

  // Validate the project path. Library-safe variant of the CLI's
  // `resolveAndValidateProjectPath` — returns a structured failure
  // instead of calling `process.exit` when the path is missing.
  const validated = requireProjectPath(opts.unityProjectPath);
  if (!validated.ok) {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: validated.error.message,
      error: validated.error,
    });
  }
  const projectPath = validated.projectPath;

  // Match the CLI's flow: read config, fall back to deterministic port.
  // We tolerate a missing on-disk path here (the deterministic port
  // works without the directory existing) but require it to exist when
  // reading the config — `readConfig` already returns `null` for
  // non-existent paths, so no extra check needed.
  const exists = fs.existsSync(projectPath);
  const config = exists ? readConfig(projectPath) : null;
  const fromConfig = config
    ? resolveConnectionFromConfig(config)
    : { url: undefined, token: undefined };

  let url: string;
  if (fromConfig.url) {
    url = fromConfig.url.replace(/\/$/, '');
  } else {
    // Mirror the CLI: fall back to deterministic port from path.
    // Use `path.resolve` is already done by `requireProjectPath`.
    const port = generatePortFromDirectory(projectPath);
    url = `http://localhost:${port}`;
  }

  const token = opts.token ?? fromConfig.token;
  return { kind: 'success', url, token };
}

function serializeInput(input: unknown): { json: string } | { error: Error } {
  if (input === undefined || input === null) return { json: '{}' };
  if (typeof input === 'string') {
    // Allow callers to pass a pre-serialized JSON string. Validate it
    // round-trips so the server never sees malformed bodies.
    try {
      JSON.parse(input);
      return { json: input };
    } catch (err) {
      return {
        error: new Error(
          `input string is not valid JSON: ${err instanceof Error ? err.message : String(err)}`,
        ),
      };
    }
  }
  if (typeof input !== 'object') {
    return {
      error: new Error('input must be a plain object, JSON string, undefined, or null.'),
    };
  }
  try {
    return { json: JSON.stringify(input) };
  } catch (err) {
    return {
      error: new Error(
        `input could not be serialized to JSON: ${err instanceof Error ? err.message : String(err)}`,
      ),
    };
  }
}

async function safeReadText(response: Response): Promise<string> {
  try {
    return await response.text();
  } catch {
    return '';
  }
}

function parseJsonOrText(text: string): unknown {
  if (text.length === 0) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function classifyFetchError(
  err: unknown,
  endpoint: string,
  timeoutMs: number,
): RunToolFailure {
  const isAbort = err instanceof Error && err.name === 'AbortError';
  if (isAbort) {
    return makeFailure({
      endpoint,
      reason: 'timeout',
      message: `Tool call timed out after ${timeoutMs}ms.`,
      error: err,
    });
  }

  const error = err instanceof Error ? err : new Error(String(err));
  const cause = err instanceof Error && 'cause' in err ? (err.cause as { code?: string; message?: string } | undefined) : undefined;
  const causeCode = cause?.code ?? '';
  const causeMessage = cause?.message ?? '';
  const signature = `${error.message} ${causeMessage} ${causeCode}`;

  let reason: RunToolFailureReason = 'unknown';
  if (signature.includes('ECONNREFUSED')) reason = 'connection-refused';
  else if (signature.includes('ECONNRESET')) reason = 'connection-reset';
  else if (signature.includes('EAI_AGAIN') || signature.includes('ENOTFOUND')) reason = 'network-error';

  return makeFailure({
    endpoint,
    reason,
    message: error.message,
    error,
  });
}

function makeFailure(
  fields: Omit<RunToolFailure, 'kind' | 'success'>,
): RunToolFailure {
  return { kind: 'failure', success: false, ...fields };
}
