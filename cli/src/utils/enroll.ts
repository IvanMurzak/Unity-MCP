// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as path from 'path';
import { CLOUD_SERVER_BASE_URL } from './config.js';
import { deriveProjectPin } from './port.js';
import { agentRegistry, MCP_SERVER_NAME } from './agents.js';
import { writeProjectMarker } from './project-marker.js';
import type { MachineCredentialStore } from './machine-credentials.js';

/**
 * Agent-driven enrollment (D13): redeem a one-time enrollment code — minted by the server's
 * `enroll_engine_plugin` tool from an already-authorized agent session — for a plugin credential,
 * with NO second browser hop. The redeemed credential is planted in the shared machine store, the
 * server target it was minted for is recorded in the project marker, and the D14 project pin is
 * upserted into any existing project-local agent config so the plugin boots pointed at the right
 * hub. Codes are burned server-side on first redeem attempt; a spent/invalid code yields a uniform
 * error surfaced here as an actionable message.
 */

/** Raised on any enrollment-redeem failure. Carries the HTTP status when one was received. */
export class EnrollmentError extends Error {
  readonly status?: number;
  constructor(message: string, status?: number) {
    super(message);
    this.name = 'EnrollmentError';
    this.status = status;
  }
}

/** Credential material returned by a successful `/api/auth/enroll/redeem`. */
export interface RedeemedCredential {
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  serverTarget?: string;
  subject?: string;
}

export interface RedeemOptions {
  /** Authorization Server base; defaults to the hosted `CLOUD_SERVER_BASE_URL`. */
  baseUrl?: string;
  /** `fetch` injection (tests). */
  fetchImpl?: typeof fetch;
  /** Request timeout; defaults to 30s. */
  timeoutMs?: number;
}

function nonEmptyString(value: unknown): string | undefined {
  return typeof value === 'string' && value.length > 0 ? value : undefined;
}

function numberOrUndefined(value: unknown): number | undefined {
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
}

/**
 * Normalize the redeem response. The Authorization Server's JSON key casing is not re-derivable
 * from this repo (the AS lives in a separate service), so both snake_case and camelCase variants
 * of every field are accepted defensively; `expires_in` seconds are converted to an absolute
 * `expiresAt` ISO timestamp when no explicit `expires_at` is present.
 */
export function normalizeRedeemResponse(data: Record<string, unknown>): RedeemedCredential {
  const accessToken = nonEmptyString(data.access_token) ?? nonEmptyString(data.accessToken);
  const refreshToken = nonEmptyString(data.refresh_token) ?? nonEmptyString(data.refreshToken);
  const serverTarget =
    nonEmptyString(data.server_target) ??
    nonEmptyString(data.serverTarget) ??
    nonEmptyString(data.server_target_url) ??
    nonEmptyString(data.serverTargetUrl);
  const subject = nonEmptyString(data.subject) ?? nonEmptyString(data.sub);

  let expiresAt = nonEmptyString(data.expires_at) ?? nonEmptyString(data.expiresAt);
  const expiresIn = numberOrUndefined(data.expires_in) ?? numberOrUndefined(data.expiresIn);
  if (!expiresAt && expiresIn !== undefined) {
    expiresAt = new Date(Date.now() + expiresIn * 1000).toISOString();
  }

  return { accessToken, refreshToken, expiresAt, serverTarget, subject };
}

/**
 * Redeem an enrollment code against `POST <baseUrl>/api/auth/enroll/redeem` with body
 * `{enroll_code}`. The code travels only in the request BODY (never a query string). A non-2xx
 * response is surfaced as an actionable `EnrollmentError` (invalid/expired/already-used codes all
 * return a uniform server error; burn-on-first-attempt is server-side).
 */
export async function redeemEnrollmentCode(
  code: string,
  opts: RedeemOptions = {},
): Promise<RedeemedCredential> {
  const baseUrl = opts.baseUrl ?? CLOUD_SERVER_BASE_URL;
  const doFetch = opts.fetchImpl ?? fetch;
  const url = `${baseUrl}/api/auth/enroll/redeem`;

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), opts.timeoutMs ?? 30000);

  let response: Response;
  try {
    response = await doFetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ enroll_code: code }),
      signal: controller.signal,
    });
  } catch (err) {
    throw new EnrollmentError(
      `Could not reach the enrollment server at ${url}: ${err instanceof Error ? err.message : String(err)}`,
    );
  } finally {
    clearTimeout(timer);
  }

  if (!response.ok) {
    throw new EnrollmentError(
      `Enrollment failed (HTTP ${response.status}). The enrollment code may be invalid, expired, ` +
        `or already used — ask the agent to issue a fresh code and try again.`,
      response.status,
    );
  }

  let data: Record<string, unknown>;
  try {
    data = (await response.json()) as Record<string, unknown>;
  } catch {
    throw new EnrollmentError('Enrollment server returned a malformed (non-JSON) response.');
  }

  const credential = normalizeRedeemResponse(data);
  if (!credential.accessToken) {
    throw new EnrollmentError('Enrollment response did not contain an access token.');
  }
  return credential;
}

// ---------------------------------------------------------------------------
// Enrollment code resolution (--enroll <code> vs --enroll-stdin)
// ---------------------------------------------------------------------------

/**
 * Resolve the enrollment code from `--enroll <code>` (argv) or `--enroll-stdin` (stdin), enforcing
 * mutual exclusion. `--enroll-stdin` reads via the injected `readStdin` so the code NEVER lands in
 * argv / shell history. `readStdin` is only invoked in the stdin mode.
 */
export function resolveEnrollCode(
  opts: { enroll?: string; enrollStdin?: boolean },
  readStdin: () => string,
): string {
  if (opts.enroll && opts.enrollStdin) {
    throw new Error('Use either --enroll <code> or --enroll-stdin, not both.');
  }
  if (opts.enrollStdin) {
    const code = readStdin().trim();
    if (!code) throw new Error('No enrollment code received on stdin.');
    return code;
  }
  if (opts.enroll) {
    const code = opts.enroll.trim();
    if (!code) throw new Error('Enrollment code (--enroll) is empty.');
    return code;
  }
  throw new Error('An enrollment code is required: pass --enroll <code> or --enroll-stdin.');
}

// ---------------------------------------------------------------------------
// Project pin upsert (D14)
// ---------------------------------------------------------------------------

/**
 * Add (or replace) the `/p/<pin>` routing segment on a config URL so an agent session launched in
 * this project folder routes strictly to this project's engine (design 06 D14). Existing `/p/<pin>`
 * segments are replaced; the port / host / scheme are preserved.
 */
export function pinUrl(rawUrl: string, pin: string): string {
  const stripExistingPin = (segments: string[]): string[] => {
    const idx = segments.findIndex((s) => s === 'p');
    if (idx >= 0 && idx === segments.length - 2 && /^[0-9a-f]{8}$/i.test(segments[idx + 1])) {
      return segments.slice(0, idx);
    }
    return segments;
  };

  try {
    const url = new URL(rawUrl);
    let segments = stripExistingPin(url.pathname.split('/').filter(Boolean));
    segments = [...segments, 'p', pin];
    url.pathname = '/' + segments.join('/');
    return url.toString().replace(/\/$/, '');
  } catch {
    const base = rawUrl.replace(/\/+$/, '').replace(/\/p\/[0-9a-f]{8}$/i, '');
    return `${base}/p/${pin}`;
  }
}

/**
 * The exact project-root STRING to feed into ProjectIdentity so the CLI-derived pin matches the
 * plugin's. The Unity plugin registers with `ProjectIdentity.DerivePin(UnityMcpPluginEditor
 * .ProjectRootPath)`, where `ProjectRootPath => Path.GetDirectoryName(Application.dataPath)`.
 * `Application.dataPath` is forward-slash on EVERY platform (incl. Windows: `C:/proj/Assets`), so
 * the plugin hashes a forward-slash root like `C:/proj`. Node's `path.resolve` yields BACKSLASHES
 * on Windows (`C:\proj`), which the ProjectIdentity golden vectors hash to a DIFFERENT pin — so we
 * convert separators to `/` here (a no-op on POSIX) to route to the same engine instance.
 */
export function projectRootForIdentity(projectPath: string): string {
  return path.resolve(projectPath).replace(/\\/g, '/');
}

export interface PinUpsertResult {
  updatedFiles: string[];
}

/**
 * Upsert the project pin into every EXISTING project-local (project-scoped) JSON agent config that
 * carries an `ai-game-developer` server entry with a `url` / `serverUrl`. Global client config
 * files (Claude Desktop, Antigravity, Cline, the Copilot CLI) are never touched — the golden path
 * writes no user-global entry. TOML (Codex) configs are left to the server binary's own
 * `configure`. Returns the list of files actually rewritten.
 */
export function upsertProjectPinIntoConfigs(projectPath: string, pin: string): PinUpsertResult {
  const resolvedProject = path.resolve(projectPath);
  const updatedFiles: string[] = [];

  for (const agent of agentRegistry) {
    if (agent.configFormat !== 'json') continue;

    const configPath = agent.getConfigPath(resolvedProject);
    // Project-scoped only: the config path must live inside the project directory.
    const relative = path.relative(resolvedProject, configPath);
    if (relative.startsWith('..') || path.isAbsolute(relative)) continue;
    if (!fs.existsSync(configPath)) continue;

    let root: Record<string, unknown>;
    try {
      const parsed = JSON.parse(fs.readFileSync(configPath, 'utf-8')) as unknown;
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) continue;
      root = parsed as Record<string, unknown>;
    } catch {
      continue;
    }

    const body = root[agent.bodyPath];
    if (!body || typeof body !== 'object' || Array.isArray(body)) continue;
    const entry = (body as Record<string, unknown>)[MCP_SERVER_NAME];
    if (!entry || typeof entry !== 'object' || Array.isArray(entry)) continue;

    const entryRecord = entry as Record<string, unknown>;
    let changed = false;
    for (const key of ['url', 'serverUrl']) {
      const current = entryRecord[key];
      if (typeof current === 'string' && current.length > 0) {
        const pinned = pinUrl(current, pin);
        if (pinned !== current) {
          entryRecord[key] = pinned;
          changed = true;
        }
      }
    }

    if (changed) {
      fs.writeFileSync(configPath, JSON.stringify(root, null, 2) + '\n');
      updatedFiles.push(configPath);
    }
  }

  return { updatedFiles };
}

// ---------------------------------------------------------------------------
// Full enroll flow
// ---------------------------------------------------------------------------

export interface RunEnrollOptions {
  code: string;
  projectPath: string;
  store: MachineCredentialStore;
  baseUrl?: string;
  fetchImpl?: typeof fetch;
}

export interface RunEnrollResult {
  serverTarget: string;
  pin: string;
  credentialPath: string;
  markerPath: string;
  pinnedConfigs: string[];
}

/**
 * Execute the full enrollment side effect: redeem → persist the plugin credential to the SHARED
 * machine store → write the project marker with the server target → upsert the D14 pin into
 * existing project-local configs. NEVER writes a project token file / `cloudToken` config.
 */
export async function runEnroll(opts: RunEnrollOptions): Promise<RunEnrollResult> {
  const credential = await redeemEnrollmentCode(opts.code, {
    baseUrl: opts.baseUrl,
    fetchImpl: opts.fetchImpl,
  });

  const serverTarget = credential.serverTarget ?? opts.baseUrl ?? CLOUD_SERVER_BASE_URL;

  // Persist to the shared machine credential store (0600 / DPAPI) — never a project file.
  opts.store.write({
    accessToken: credential.accessToken,
    refreshToken: credential.refreshToken,
    expiresAt: credential.expiresAt,
    serverTarget,
    subject: credential.subject,
  });

  // Record the enrolled server target in the committable project marker.
  const markerPath = writeProjectMarker(opts.projectPath, { serverTarget });

  // Upsert the D14 pin into existing project-local agent configs. The pin is derived from the
  // forward-slash project root so it matches the plugin's pin (see projectRootForIdentity).
  const pin = deriveProjectPin(projectRootForIdentity(opts.projectPath));
  const { updatedFiles } = upsertProjectPinIntoConfigs(opts.projectPath, pin);

  return {
    serverTarget,
    pin,
    credentialPath: opts.store.credentialsPath,
    markerPath,
    pinnedConfigs: updatedFiles,
  };
}
