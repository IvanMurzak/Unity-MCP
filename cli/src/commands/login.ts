// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveProjectPath } from '../utils/connection.js';
import { getOrCreateConfig, writeConfig, CLOUD_SERVER_BASE_URL } from '../utils/config.js';
import { deviceAuthFlow } from '../utils/auth.js';
import { openBrowser } from '../utils/browser.js';

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

      let spinner: ReturnType<typeof ui.startSpinner> | undefined;

      try {
        const result = await deviceAuthFlow(
          CLOUD_SERVER_BASE_URL,
          'Unity-MCP CLI',
          {
            onUserCode: (userCode, verificationUrl) => {
              ui.info(`Open this URL to authorize:`);
              console.log();
              console.log(`  ${verificationUrl}`);
              console.log();
              ui.label('Code', userCode);
              openBrowser(verificationUrl);
            },
            onPolling: () => {
              spinner = ui.startSpinner('Waiting for authorization...');
            },
          },
        );

        if (result.success) {
          spinner?.success('Authorized');
          const updatedConfig = {
            ...config,
            cloudToken: result.accessToken,
            connectionMode: 'Cloud' as const,
          };
          writeConfig(projectPath, updatedConfig);
          ui.success('Authentication complete. Cloud token saved to project config.');
        } else {
          spinner?.stop();
          ui.error(result.message);
          process.exit(1);
        }
      } catch (err) {
        spinner?.stop();
        const message = err instanceof Error ? err.message : String(err);
        if (message.includes('ECONNREFUSED') || message.includes('fetch failed')) {
          ui.error(`Cannot reach cloud server at ${CLOUD_SERVER_BASE_URL}`);
        } else {
          ui.error(`Authentication failed: ${message}`);
        }
        process.exit(1);
      }
    },
  );
