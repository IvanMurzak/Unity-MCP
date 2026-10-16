// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { CLOUD_SERVER_BASE_URL } from '../utils/config.js';
import { runCloudLogin, completePluginDerivation } from '../utils/cloud-login.js';
import { createCloudCredentialProvider } from '../utils/cloud-credentials.js';
import { MachineCredentialStore, MACHINE_STORE_DIR_NAME } from '../utils/machine-credentials.js';
import { effectiveFamilies, LoginRequiredError } from '@baizor/gamedev-cli-core';

interface LoginOptions {
  project?: string;
  force?: boolean;
  toolsOnly?: boolean;
  yes?: boolean;
}

/**
 * Resolve the credential store: the shared machine store (`~/.ai-game-dev/`) by default, or a
 * project-local store (`<path>/.ai-game-dev/`) when `--project` is given.
 */
function resolveStore(options: LoginOptions): MachineCredentialStore {
  if (options.project) {
    const base = path.join(path.resolve(options.project), MACHINE_STORE_DIR_NAME);
    return new MachineCredentialStore(base);
  }
  return new MachineCredentialStore();
}

/**
 * Finish an interrupted F1 login (agent family committed, plugin derivation missing): mint a
 * fresh agent access token through the credential provider and retry the derivation leg alone.
 */
async function finishPartialAuthorization(store: MachineCredentialStore): Promise<never> {
  ui.info('Finishing a previously interrupted sign-in (deriving the tools credential)...');
  const provider = createCloudCredentialProvider({ store });
  let agentAccessToken: string;
  try {
    agentAccessToken = await provider.getAccessToken({ family: 'agent' });
  } catch (err) {
    if (err instanceof LoginRequiredError) {
      ui.error('The stored sign-in is no longer valid. Re-run with --force to sign in again.');
    } else {
      ui.error(
        `Could not refresh the stored sign-in: ${err instanceof Error ? err.message : String(err)}`,
      );
    }
    process.exit(1);
  }
  const ok = await completePluginDerivation(store, agentAccessToken);
  process.exit(ok ? 0 : 1);
}

export const loginCommand = new Command('login')
  .description(
    'Sign in to ai-game.dev and store the credential in the shared machine credential store (~/.ai-game-dev/credentials.json)',
  )
  .option(
    '--project <path>',
    'Store the credential in a project-local store (<path>/.ai-game-dev/) instead of the shared machine store',
  )
  .option('--force', 'Re-authenticate even if already signed in')
  .option(
    '--tools-only',
    'Authorize engine tools only (mcp:plugin scope): no agent credential is stored, so desktop-app pickup is impossible — intended for CI / automation runners',
  )
  .option('--yes', 'Assume "yes" for prompts (e.g. confirming an account switch)')
  .action(async (options: LoginOptions) => {
    const store = resolveStore(options);
    verbose(`Credential store: ${store.credentialsPath}`);

    if (store.exists && !options.force) {
      // The sign-in gate consults ONLY the credential store (never a project config). When the
      // store is readable, also detect the F1 `partial` state — an agent family committed with
      // no plugin-plane family — and finish the derivation leg alone instead of short-circuiting.
      const state = store.readState();
      if (state.status === 'ok') {
        const families = effectiveFamilies(state.credentials);
        const hasPluginPlane = !!(
          families.plugin?.accessToken ?? families.legacy?.accessToken
        );
        const hasAgent = !!families.agent?.accessToken;
        if (hasAgent && !hasPluginPlane && !options.toolsOnly) {
          await finishPartialAuthorization(store);
        }
      }
      ui.success('Already signed in.');
      ui.info(`Credential: ${store.credentialsPath}`);
      ui.info('Use --force to re-authenticate.');
      return;
    }

    ui.heading('Sign in to ai-game.dev');
    ui.label('Server', CLOUD_SERVER_BASE_URL);
    if (options.toolsOnly) {
      ui.label('Mode', 'tools-only (mcp:plugin)');
    }
    ui.divider();

    const token = await runCloudLogin(store, {
      toolsOnly: options.toolsOnly ?? false,
      assumeYes: options.yes ?? false,
    });
    if (token) {
      ui.success(`Signed in. Credential saved to ${store.credentialsPath}`);
    } else {
      process.exit(1);
    }
  });
