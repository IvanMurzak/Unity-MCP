// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import {
  MachineCredentialProvider,
  MachineCredentialStore,
  MachineCredentialStoreUnreadableError,
  CredentialLockBusyError,
  HttpTokenRefresher,
  LoginRequiredError,
  unityAdapter,
} from '@baizor/gamedev-cli-core';
import { verbose } from './ui.js';
import { CLOUD_SERVER_BASE_URL } from './config.js';

/**
 * The CLI's single seam onto cli-core's `MachineCredentialProvider` (unified-machine-auth 02/04,
 * task d2 / W2). Every Cloud-mode Bearer the CLI presents comes through here — the provider owns
 * proactive refresh (inside the 60 s expiry skew), reactive refresh (driven by a hub 401), the
 * cross-process credential lock, and the family-aware machine-store view. The CLI never reads
 * `accessToken` raw off disk: a raw read returns a token that may be seconds from expiry (or past
 * it) with nobody refreshing, which is exactly the defect this module replaces
 * (the pre-d2 `readMachineStoreCloudToken`).
 */

/** Injectable construction options — tests point the provider at a temp store + a fake AS. */
export interface CloudCredentialProviderOptions {
  /** The credential store to serve from; defaults to the shared per-machine store. */
  store?: MachineCredentialStore;
  /** Authorization-server base for the refresh endpoint; defaults to the hosted cloud. */
  serverBaseUrl?: string;
  /** Injectable `fetch` for the refresher (tests). */
  fetchImpl?: typeof fetch;
}

/**
 * Build a `MachineCredentialProvider` wired the way this CLI consumes it:
 * `HttpTokenRefresher` against the AS root, and `unity-mcp-cli` as the component-default client
 * id — used ONLY for `families.legacy` (a stored family's own `clientId` always wins, 04 §3).
 */
export function createCloudCredentialProvider(
  options: CloudCredentialProviderOptions = {},
): MachineCredentialProvider {
  const store = options.store ?? new MachineCredentialStore();
  const refresher = new HttpTokenRefresher({
    defaultServerBaseUrl: options.serverBaseUrl ?? CLOUD_SERVER_BASE_URL,
    ...(options.fetchImpl ? { fetchImpl: options.fetchImpl } : {}),
  });
  return new MachineCredentialProvider(store, refresher, {
    defaultClientId: unityAdapter.clientId, // unity-mcp-cli
    onWarning: (message) => verbose(`[credential-provider] ${message}`),
    onTelemetry: (event) => verbose(`[credential-provider] ${event.type}: ${event.family} (${event.reason})`),
  });
}

/** Lazy singleton for the default (real per-machine) store — one provider per CLI process. */
let defaultProvider: MachineCredentialProvider | undefined;

function resolveProvider(options?: CloudCredentialProviderOptions): MachineCredentialProvider {
  if (options?.store || options?.serverBaseUrl || options?.fetchImpl) {
    return createCloudCredentialProvider(options);
  }
  defaultProvider ??= createCloudCredentialProvider();
  return defaultProvider;
}

/** TEST-ONLY: drop the cached default provider (e.g. after re-pointing HOME). */
export function resetCloudCredentialProviderForTests(): void {
  defaultProvider = undefined;
}

/**
 * Read a valid plugin-plane access token for a Cloud-mode call, PROACTIVELY refreshing under the
 * cross-process lock when the stored token is within the expiry skew. Returns `undefined` when the
 * machine is effectively not signed in — no credential, a dead family, an unreadable store, or a
 * lock that stayed contended — so callers surface their actionable "not logged in" error instead
 * of issuing a silent unauthenticated request (defect E / D11). The precise reason is logged via
 * `verbose()` (never token bytes).
 */
export async function readCloudAccessToken(
  options?: CloudCredentialProviderOptions,
): Promise<string | undefined> {
  const provider = resolveProvider(options);
  try {
    return await provider.getAccessToken({ family: 'plugin' });
  } catch (err) {
    return degradeToSignedOut(err, 'read');
  }
}

/**
 * REACTIVELY refresh the plugin-plane family now — the hub answered 401 while the local expiry
 * still looked fine (revocation, clock skew). Runs the same locked critical section as the
 * proactive path. Returns the fresh access token, or `undefined` when the family is dead /
 * signed out (the caller falls back to its "not logged in" error).
 */
export async function refreshCloudAccessToken(
  options?: CloudCredentialProviderOptions,
): Promise<string | undefined> {
  const provider = resolveProvider(options);
  try {
    const document = await provider.refresh({ family: 'plugin' });
    return document.accessToken ?? document.families?.plugin?.accessToken ?? undefined;
  } catch (err) {
    return degradeToSignedOut(err, 'refresh');
  }
}

function degradeToSignedOut(err: unknown, operation: string): undefined {
  if (err instanceof LoginRequiredError) {
    verbose(`Cloud credential ${operation}: login required (${err.message})`);
    return undefined;
  }
  if (err instanceof MachineCredentialStoreUnreadableError) {
    verbose(`Cloud credential ${operation}: store unreadable — sign in again to replace it (${err.message})`);
    return undefined;
  }
  if (err instanceof CredentialLockBusyError) {
    verbose(`Cloud credential ${operation}: credential store busy — retry shortly (${err.message})`);
    return undefined;
  }
  throw err;
}
