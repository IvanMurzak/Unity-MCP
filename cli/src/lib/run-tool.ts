// Library-safe `runTool` / `runSystemTool` implementations.
//
// Constraints (same contract as the rest of `lib/*.ts`):
// - No commander, no spinners, no process.exit, no console output.
// - Errors are returned in `{ kind: 'failure', success: false, ... }`,
//   never thrown past the public boundary.

import { readConfig, resolveConnectionFromConfig, isCloudMode } from '../utils/config.js';
import { readCloudAccessToken, refreshCloudAccessToken } from '../utils/cloud-credentials.js';
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

interface ErrorCause {
  code?: string;
  message?: string;
}

/**
 * Invoke a regular MCP tool over the Unity plugin's HTTP API.
 *
 * URL/token resolution priority: explicit override → project config →
 * deterministic localhost port. POSTs to `/api/tools/{name}`. No
 * console output, no `process.exit`; errors are returned in the
 * `kind: 'failure'` variant.
 */
export async function runTool(opts: RunToolOptions): Promise<RunToolResult> {
  return invokeTool('/api/tools', opts);
}

/**
 * Invoke a system tool (internal tool not exposed to MCP clients) over
 * the Unity plugin's HTTP API. POSTs to `/api/system-tools/{name}`.
 */
export async function runSystemTool(opts: RunToolOptions): Promise<RunToolResult> {
  return invokeTool('/api/system-tools', opts);
}

async function invokeTool(routePrefix: string, opts: RunToolOptions): Promise<RunToolResult> {
  const validationFailure = validateOptions(opts);
  if (validationFailure) return validationFailure;

  const fetchImpl = opts.fetchImpl ?? globalThis.fetch;
  const timeoutMs =
    typeof opts.timeoutMs === 'number' && opts.timeoutMs > 0 ? opts.timeoutMs : DEFAULT_TIMEOUT_MS;

  // Wire the abort/timeout plumbing BEFORE the first await so an external abort issued right
  // after the call is never lost (connection resolution below may itself await a token refresh).
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  const externalAbort = (): void => controller.abort();
  if (opts.signal) {
    if (opts.signal.aborted) controller.abort();
    else opts.signal.addEventListener('abort', externalAbort, { once: true });
  }

  let endpoint = '';
  try {
    const resolved = await resolveConnection(opts);
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

    endpoint = `${url}${routePrefix}/${encodeURIComponent(opts.toolName)}`;

    // Reactive-refresh loop (unified-machine-auth 04 §3 rule 1): when the Bearer came from the
    // shared machine credential store and the server answers 401 — revocation or clock skew the
    // proactive expiry check could not see — refresh the plugin family once under the
    // cross-process lock and retry the call with the rotated token. At most ONE reactive attempt
    // per invocation; an explicit --token / --url override is never refreshed. Both attempts
    // share one timeout.
    let bearer = token;
    let attemptedReactiveRefresh = false;

    for (;;) {
      // Mirror fetch's contract for an ALREADY-aborted signal (an external abort may land while
      // connection resolution above is awaiting): reject up front instead of relying on the
      // 'abort' event, which has already fired.
      if (controller.signal.aborted) {
        throw new DOMException('The operation was aborted', 'AbortError');
      }

      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (bearer) headers['Authorization'] = `Bearer ${bearer}`;

      const response = await fetchImpl(endpoint, {
        method: 'POST',
        headers,
        body: body.json,
        signal: controller.signal,
      });

      const text = await safeReadText(response);
      const data = parseJsonOrText(text);

      if (!response.ok) {
        if (
          response.status === 401 &&
          resolved.tokenFromCloudStore &&
          !attemptedReactiveRefresh
        ) {
          attemptedReactiveRefresh = true;
          const fresh = await (opts.refreshCloudToken ?? refreshCloudAccessToken)();
          if (fresh) {
            bearer = fresh;
            continue;
          }
        }
        return makeFailure({
          endpoint,
          reason: 'http-error',
          httpStatus: response.status,
          data,
          message: response.statusText || `HTTP ${response.status}`,
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
    }
  } catch (err) {
    return classifyFetchError(err, endpoint, timeoutMs);
  } finally {
    clearTimeout(timer);
    opts.signal?.removeEventListener('abort', externalAbort);
  }
}

function validateOptions(opts: RunToolOptions): RunToolFailure | null {
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
  const hasUrl = typeof opts.url === 'string' && opts.url.length > 0;
  const hasProjectPath =
    typeof opts.unityProjectPath === 'string' && opts.unityProjectPath.trim().length > 0;
  if (!hasUrl && !hasProjectPath) {
    return makeFailure({
      endpoint: '',
      reason: 'invalid-input',
      message: 'Either unityProjectPath or url must be provided.',
    });
  }
  return null;
}

async function resolveConnection(
  opts: RunToolOptions,
): Promise<
  | { kind: 'success'; url: string; token: string | undefined; tokenFromCloudStore: boolean }
  | RunToolFailure
> {
  if (opts.url) {
    return {
      kind: 'success',
      url: opts.url.replace(/\/$/, ''),
      token: opts.token,
      tokenFromCloudStore: false,
    };
  }

  // `unityProjectPath` is library-only — does NOT require an `Assets/`
  // folder, unlike the CLI's `resolveAndValidateProjectPath`. The
  // deterministic-port fallback works against the path string alone.
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

  const config = readConfig(projectPath);
  const fromConfig = config
    ? await resolveConnectionFromConfig(config, {
        readCloudToken: opts.readCloudToken ?? readCloudAccessToken,
      })
    : { url: undefined, token: undefined };

  const url = fromConfig.url
    ? fromConfig.url.replace(/\/$/, '')
    : `http://localhost:${generatePortFromDirectory(projectPath)}`;

  const token = opts.token ?? fromConfig.token;

  // Cloud mode draws its Bearer from the shared machine credential store; when the user is not
  // logged in the token is undefined. Fail actionably rather than issue a silent unauthenticated
  // cloud request (defect E / D11).
  if (config && isCloudMode(config) && !token) {
    return makeFailure({
      endpoint: '',
      reason: 'not-authenticated',
      message:
        'Not logged in to ai-game.dev. Run `unity-mcp-cli login` to sign in, or pass an explicit --token / --url.',
    });
  }

  const tokenFromCloudStore = !!config && isCloudMode(config) && !opts.token && !!token;
  return { kind: 'success', url, token, tokenFromCloudStore };
}

function serializeInput(input: unknown): { json: string } | { error: Error } {
  if (input === undefined || input === null) return { json: '{}' };
  if (typeof input === 'string') {
    // Validate the round-trip so the server never sees malformed bodies.
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

function getCause(err: unknown): ErrorCause | undefined {
  if (!(err instanceof Error) || !('cause' in err)) return undefined;
  return err.cause as ErrorCause | undefined;
}

function classifyFetchError(
  err: unknown,
  endpoint: string,
  timeoutMs: number,
): RunToolFailure {
  if (err instanceof Error && err.name === 'AbortError') {
    return makeFailure({
      endpoint,
      reason: 'timeout',
      message: `Tool call timed out after ${timeoutMs}ms.`,
      error: err,
    });
  }

  const error = err instanceof Error ? err : new Error(String(err));
  const causeCode = getCause(err)?.code;

  let reason: RunToolFailureReason = 'unknown';
  if (causeCode === 'ECONNREFUSED') reason = 'connection-refused';
  else if (causeCode === 'ECONNRESET') reason = 'connection-reset';
  else if (causeCode === 'ENOTFOUND' || causeCode === 'EAI_AGAIN') reason = 'network-error';

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
