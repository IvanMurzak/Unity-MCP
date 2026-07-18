// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as ui from './ui.js';
import { CLOUD_SERVER_BASE_URL } from './config.js';
import { openBrowser } from './browser.js';
import { MachineCredentialStore } from './machine-credentials.js';
import {
  deviceLogin,
  unityAdapter,
  DEFAULT_PLUGIN_SCOPE,
  type DeviceLoginResult,
  type DeviceLoginOptions,
} from '@baizor/gamedev-cli-core';

/**
 * The device-flow login now runs on `@baizor/gamedev-cli-core`'s OAuth 2.1 Device Authorization
 * Grant (RFC 8628) — client_id `unity-mcp-cli`, scope `mcp:plugin` — which mints an ES256 hub JWT
 * plus a rotating refresh token and NEVER mints a PAT (auth-fixes T1, closing B2/B3). The FULL
 * credential set (accessToken, refreshToken, expiresAt, serverTarget, subject) is persisted to the
 * shared machine credential store — the legacy flow dropped refresh/expiry/subject on the floor.
 */

/** Injection seam so the login flow can be exercised offline in tests without the network. */
export interface RunCloudLoginDeps {
  /** Authorization-server base; defaults to the hosted `CLOUD_SERVER_BASE_URL`. */
  serverBaseUrl?: string;
  /** The device-login implementation; defaults to cli-core's `deviceLogin`. */
  login?: (options: DeviceLoginOptions) => Promise<DeviceLoginResult>;
}

/**
 * Run the cloud device-auth flow: initiate, display the user code + verification URL, open the
 * browser, poll, and persist the FULL credential to the shared machine credential store.
 *
 * Returns the access token on success, or null on failure (errors are printed).
 */
export async function runCloudLogin(
  store: MachineCredentialStore,
  deps: RunCloudLoginDeps = {},
): Promise<string | null> {
  const serverBaseUrl = deps.serverBaseUrl ?? CLOUD_SERVER_BASE_URL;
  const login = deps.login ?? deviceLogin;
  let spinner: ReturnType<typeof ui.startSpinner> | undefined;

  try {
    const result = await login({
      serverBaseUrl,
      clientId: unityAdapter.clientId, // unity-mcp-cli
      scope: DEFAULT_PLUGIN_SCOPE, // mcp:plugin
      onUserCode: (userCode, verificationUri) => {
        ui.info('Open this URL to authorize:');
        console.log();
        console.log(`  ${verificationUri}`);
        console.log();
        ui.label('Code', userCode);
      },
      onPolling: () => {
        spinner = ui.startSpinner('Waiting for authorization...');
      },
      openBrowser,
    });

    if (result.ok) {
      spinner?.success('Authorized');

      // Persist the FULL credential set (accessToken + refreshToken + expiresAt + serverTarget +
      // subject) — never a project config file. This closes B3 (the legacy flow stored only
      // accessToken + serverTarget, losing the refresh/expiry needed for proactive refresh).
      store.write(result.credentials);

      return result.credentials.accessToken ?? null;
    }

    spinner?.stop();
    ui.error(result.message);
    return null;
  } catch (err) {
    spinner?.stop();
    const message = err instanceof Error ? err.message : String(err);
    if (message.includes('ECONNREFUSED') || message.includes('fetch failed')) {
      ui.error(`Cannot reach cloud server at ${serverBaseUrl}`);
    } else {
      ui.error(`Authentication failed: ${message}`);
    }
    return null;
  }
}
