// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as readline from 'readline/promises';
import * as ui from './ui.js';
import { CLOUD_SERVER_BASE_URL } from './config.js';
import { openBrowser } from './browser.js';
import { MachineCredentialStore } from './machine-credentials.js';
import {
  deviceLogin,
  unityAdapter,
  commitAgentLogin,
  commitToolsOnlyLogin,
  derivePluginFamily,
  HttpTokenExchangeClient,
  DEFAULT_PLUGIN_SCOPE,
  MCP_AGENT_SCOPE,
  type DeviceLoginResult,
  type DeviceLoginOptions,
  type MachineCredentials,
  type RevokeTokenFn,
  type TokenExchangeClient,
} from '@baizor/gamedev-cli-core';

/**
 * The cloud sign-in flow (unified-machine-auth 03 F1/F7/F10, task d2). The device grant
 * (RFC 8628, client_id `unity-mcp-cli`) now mints at **agent scope** (`mcp:agent`) by default and
 * the commit goes through cli-core's login-commit machinery — the two-lock-hold sequence: agent
 * family under the first hold, RFC 8693 token exchange with the lock released, derived plugin
 * family (+ v1 mirror) under the second hold. A failed exchange leaves a valid committed agent
 * family (`partial`) and the derivation alone is retried.
 *
 * `--tools-only` (O10/F10) mints at `mcp:plugin` scope and commits a plugin family ONLY — the
 * store then holds no agent family, so App pickup is impossible by design and the runner appears
 * as its own revocable device group.
 *
 * The D6/F7 account-switch guard runs before ANY write: a subject mismatch prompts
 * (`--yes`-gated); decline revokes the just-minted family (best effort, RFC 7009) and aborts with
 * the store untouched.
 */

/** How many times the F1 `partial` state retries the derivation leg within one login run. */
const DERIVE_RETRY_ATTEMPTS = 3;
/** Base backoff between derivation retries (doubles per attempt). */
const DERIVE_RETRY_BASE_MS = 500;

/** Injection seams so the login flow can be exercised offline in tests without the network. */
export interface RunCloudLoginOptions {
  /** O10/F10: mint `scope=mcp:plugin` and commit a plugin-only store (no agent family). */
  toolsOnly?: boolean;
  /** F7: auto-confirm the account-switch prompt (the `--yes` flag). */
  assumeYes?: boolean;
  /** Authorization-server base; defaults to the hosted `CLOUD_SERVER_BASE_URL`. */
  serverBaseUrl?: string;
  /** The device-login implementation; defaults to cli-core's `deviceLogin`. */
  login?: (options: DeviceLoginOptions) => Promise<DeviceLoginResult>;
  /** The RFC 8693 exchange client; defaults to cli-core's `HttpTokenExchangeClient`. */
  exchangeClient?: TokenExchangeClient;
  /** The D6/F7 confirmation; defaults to an interactive prompt (auto-confirmed by `assumeYes`). */
  confirmAccountSwitch?: (info: {
    storedSubject: string;
    newSubject: string;
  }) => boolean | Promise<boolean>;
  /** Injectable best-effort RFC 7009 revoker (tests). */
  revokeToken?: RevokeTokenFn;
  /** Injectable backoff sleep (tests make it a no-op). */
  sleep?: (ms: number) => Promise<void>;
}

const defaultSleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * The D6/F7 account-switch confirmation used when no callback is injected:
 * - `--yes` ⇒ confirmed without prompting (F7 "`--yes`-gated");
 * - interactive TTY ⇒ y/N prompt (default No);
 * - non-interactive without `--yes` ⇒ DECLINED (fail closed) with an actionable hint.
 */
function buildAccountSwitchConfirm(
  assumeYes: boolean,
): (info: { storedSubject: string; newSubject: string }) => Promise<boolean> {
  return async (info) => {
    ui.warn(
      `This machine is currently signed in as "${info.storedSubject}"; you are signing in as "${info.newSubject}".`,
    );
    ui.info('Switching replaces the stored credential and signs the previous account out on this machine.');
    if (assumeYes) {
      ui.info('--yes given: switching accounts.');
      return true;
    }
    if (!process.stdin.isTTY) {
      ui.error('Account switch requires confirmation. Re-run with --yes to switch accounts.');
      return false;
    }
    const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
    try {
      const answer = (await rl.question('Switch this machine to the new account? [y/N] ')).trim();
      return answer.toLowerCase() === 'y' || answer.toLowerCase() === 'yes';
    } finally {
      rl.close();
    }
  };
}

/** The v2 document's plugin-plane access token (v1 mirror first — it IS the plugin family's). */
function pluginPlaneToken(document: MachineCredentials): string | null {
  return (
    document.accessToken ??
    document.families?.plugin?.accessToken ??
    document.families?.legacy?.accessToken ??
    null
  );
}

/**
 * Retry the F1.4 derivation leg alone (the `partial` state): RFC 8693 exchange → plugin family +
 * v1 mirror under one lock hold. Returns the committed document, or null when every attempt
 * failed or the store changed underneath (aborts are terminal — retrying cannot help).
 */
async function retryDerivePluginFamily(params: {
  store: MachineCredentialStore;
  exchangeClient: TokenExchangeClient;
  agentAccessToken: string;
  expectedSubject: string | undefined;
  serverTarget: string | undefined;
  revokeToken: RevokeTokenFn | undefined;
  sleep: (ms: number) => Promise<void>;
  attempts?: number;
}): Promise<MachineCredentials | null> {
  const attempts = params.attempts ?? DERIVE_RETRY_ATTEMPTS;
  for (let attempt = 1; attempt <= attempts; attempt++) {
    const result = await derivePluginFamily({
      store: params.store,
      exchangeClient: params.exchangeClient,
      clientId: unityAdapter.clientId,
      agentAccessToken: params.agentAccessToken,
      ...(params.expectedSubject !== undefined ? { expectedSubject: params.expectedSubject } : {}),
      ...(params.serverTarget !== undefined ? { serverTarget: params.serverTarget } : {}),
      ...(params.revokeToken ? { revokeToken: params.revokeToken } : {}),
      onWarning: ui.warn,
    });
    if (result.status === 'derived') {
      return result.document;
    }
    if (result.status === 'aborted') {
      // Store-missing / subject-changed / store-unreadable: a concurrent flow changed the world;
      // the orphaned derived family was already revoked best-effort by cli-core. Terminal.
      ui.error(`Could not finish authorization: ${result.reason}. Run \`unity-mcp-cli login\` again.`);
      return null;
    }
    ui.warn(`Deriving the tools credential failed (${result.reason}) — attempt ${attempt}/${attempts}.`);
    if (attempt < attempts) {
      await params.sleep(DERIVE_RETRY_BASE_MS * 2 ** (attempt - 1));
    }
  }
  return null;
}

/**
 * Finish a previously interrupted agent login (F1 `partial`: agent family committed, plugin
 * derivation missing) using a FRESH agent access token supplied by the caller. Returns true when
 * the plugin family is committed.
 */
export async function completePluginDerivation(
  store: MachineCredentialStore,
  agentAccessToken: string,
  options: Pick<RunCloudLoginOptions, 'serverBaseUrl' | 'exchangeClient' | 'revokeToken' | 'sleep'> = {},
): Promise<boolean> {
  const serverBaseUrl = options.serverBaseUrl ?? CLOUD_SERVER_BASE_URL;
  const exchangeClient =
    options.exchangeClient ?? new HttpTokenExchangeClient({ defaultServerBaseUrl: serverBaseUrl });
  const stored = safeRead(store);
  const document = await retryDerivePluginFamily({
    store,
    exchangeClient,
    agentAccessToken,
    expectedSubject: stored?.subject,
    serverTarget: stored?.serverTarget,
    revokeToken: options.revokeToken,
    sleep: options.sleep ?? defaultSleep,
  });
  if (document) {
    ui.success('Authorization completed: tools credential derived.');
    return true;
  }
  return false;
}

function safeRead(store: MachineCredentialStore): MachineCredentials | null {
  try {
    return store.read();
  } catch {
    return null;
  }
}

/**
 * Run the cloud device-auth flow: initiate, display the user code + verification URL, open the
 * browser, poll, then commit through cli-core's login-commit machinery (two-lock-hold agent
 * commit + exchange-derived plugin family, or the tools-only plugin commit — never a raw
 * `store.write`).
 *
 * Returns the plugin-plane access token on success, or null on failure (errors are printed).
 */
export async function runCloudLogin(
  store: MachineCredentialStore,
  options: RunCloudLoginOptions = {},
): Promise<string | null> {
  const serverBaseUrl = options.serverBaseUrl ?? CLOUD_SERVER_BASE_URL;
  const login = options.login ?? deviceLogin;
  const sleep = options.sleep ?? defaultSleep;
  let spinner: ReturnType<typeof ui.startSpinner> | undefined;

  try {
    const result = await login({
      serverBaseUrl,
      clientId: unityAdapter.clientId, // unity-mcp-cli
      // Agent scope by default (03 §F1); plugin scope only for --tools-only (O10/F10).
      scope: options.toolsOnly ? DEFAULT_PLUGIN_SCOPE : MCP_AGENT_SCOPE,
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

    if (!result.ok) {
      spinner?.stop();
      ui.error(result.message);
      return null;
    }
    spinner?.success('Authorized');

    const confirmAccountSwitch =
      options.confirmAccountSwitch ?? buildAccountSwitchConfirm(options.assumeYes ?? false);

    if (options.toolsOnly) {
      const commit = await commitToolsOnlyLogin({
        store,
        clientId: unityAdapter.clientId,
        credentials: result.credentials,
        confirmAccountSwitch,
        ...(options.revokeToken ? { revokeToken: options.revokeToken } : {}),
        onWarning: ui.warn,
      });
      switch (commit.status) {
        case 'committed':
          return pluginPlaneToken(commit.document);
        case 'switch-declined':
          ui.error('Account switch declined — nothing was changed. The new sign-in was revoked.');
          return null;
        case 'aborted':
          ui.error('The credential store changed while signing in. Run `unity-mcp-cli login` again.');
          return null;
      }
    }

    const exchangeClient =
      options.exchangeClient ?? new HttpTokenExchangeClient({ defaultServerBaseUrl: serverBaseUrl });

    const commit = await commitAgentLogin({
      store,
      exchangeClient,
      clientId: unityAdapter.clientId,
      credentials: result.credentials,
      confirmAccountSwitch,
      ...(options.revokeToken ? { revokeToken: options.revokeToken } : {}),
      onWarning: ui.warn,
    });

    switch (commit.status) {
      case 'committed':
        return pluginPlaneToken(commit.document);
      case 'partial': {
        // F1 failure path: the agent family IS committed; retry the derivation leg alone.
        ui.warn(`Partially authorized (${commit.exchangeFailure}). Retrying the tools credential...`);
        const document = await retryDerivePluginFamily({
          store,
          exchangeClient,
          agentAccessToken: result.credentials.accessToken ?? '',
          expectedSubject: result.credentials.subject,
          serverTarget: result.credentials.serverTarget,
          revokeToken: options.revokeToken,
          sleep,
        });
        if (document) {
          return pluginPlaneToken(document);
        }
        ui.error(
          'Signed in, but deriving the tools credential failed. Run `unity-mcp-cli login` again to finish authorization.',
        );
        return null;
      }
      case 'switch-declined':
        ui.error('Account switch declined — nothing was changed. The new sign-in was revoked.');
        return null;
      case 'aborted':
        ui.error('The credential store changed while signing in. Run `unity-mcp-cli login` again.');
        return null;
    }
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
