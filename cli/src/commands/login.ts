// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { CLOUD_SERVER_BASE_URL } from '../utils/config.js';
import { runCloudLogin } from '../utils/cloud-login.js';
import { MachineCredentialStore, MACHINE_STORE_DIR_NAME } from '../utils/machine-credentials.js';

interface LoginOptions {
  project?: string;
  force?: boolean;
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

export const loginCommand = new Command('login')
  .description(
    'Sign in to ai-game.dev and store the credential in the shared machine credential store (~/.ai-game-dev/credentials.json)',
  )
  .option(
    '--project <path>',
    'Store the credential in a project-local store (<path>/.ai-game-dev/) instead of the shared machine store',
  )
  .option('--force', 'Re-authenticate even if already signed in')
  .action(async (options: LoginOptions) => {
    const store = resolveStore(options);
    verbose(`Credential store: ${store.credentialsPath}`);

    if (store.exists && !options.force) {
      ui.success('Already signed in.');
      ui.info(`Credential: ${store.credentialsPath}`);
      ui.info('Use --force to re-authenticate.');
      return;
    }

    ui.heading('Sign in to ai-game.dev');
    ui.label('Server', CLOUD_SERVER_BASE_URL);
    ui.divider();

    const token = await runCloudLogin(store);
    if (token) {
      ui.success(`Signed in. Credential saved to ${store.credentialsPath}`);
    } else {
      process.exit(1);
    }
  });
