// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveProjectPath } from '../utils/connection.js';
import { getOrCreateConfig, CLOUD_SERVER_BASE_URL } from '../utils/config.js';
import { runCloudLogin } from '../utils/cloud-login.js';

interface LoginOptions {
  path?: string;
  force?: boolean;
}

export const loginCommand = new Command('login')
  .description('Authenticate with the Unity-MCP cloud server')
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--force', 'Re-authenticate even if already logged in')
  .action(
    async (
      positionalPath: string | undefined,
      options: LoginOptions,
    ) => {
      const projectPath = resolveProjectPath(positionalPath, options);
      verbose(`Project path: ${projectPath}`);

      const config = getOrCreateConfig(projectPath);

      if (config.cloudToken && !options.force) {
        ui.success('Already authenticated with cloud server.');
        ui.info('Use --force to re-authenticate.');
        return;
      }

      ui.heading('Cloud Authentication');
      ui.label('Server', CLOUD_SERVER_BASE_URL);
      ui.divider();

      const token = await runCloudLogin(projectPath);
      if (token) {
        ui.success('Authentication complete. Cloud token saved to project config.');
      } else {
        process.exit(1);
      }
    },
  );
