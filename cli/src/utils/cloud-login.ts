// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as ui from './ui.js';
import { readConfig, writeConfig, CLOUD_SERVER_BASE_URL } from './config.js';
import { deviceAuthFlow } from './auth.js';
import { openBrowser } from './browser.js';

/**
 * Run the cloud device-auth flow: initiate, display code, open browser, poll,
 * and persist the token to the project config.
 *
 * Returns the access token on success, or null on failure (errors are printed).
 */
export async function runCloudLogin(projectPath: string): Promise<string | null> {
  let spinner: ReturnType<typeof ui.startSpinner> | undefined;

  try {
    const result = await deviceAuthFlow(
      CLOUD_SERVER_BASE_URL,
      'Unity-MCP CLI',
      {
        onUserCode: (userCode, verificationUrl) => {
          ui.info('Open this URL to authorize:');
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

      const config = readConfig(projectPath) ?? {};
      const updatedConfig = {
        ...config,
        cloudToken: result.accessToken,
        connectionMode: 'Cloud' as const,
      };
      writeConfig(projectPath, updatedConfig);

      return result.accessToken;
    }

    spinner?.stop();
    ui.error(result.message);
    return null;
  } catch (err) {
    spinner?.stop();
    const message = err instanceof Error ? err.message : String(err);
    if (message.includes('ECONNREFUSED') || message.includes('fetch failed')) {
      ui.error(`Cannot reach cloud server at ${CLOUD_SERVER_BASE_URL}`);
    } else {
      ui.error(`Authentication failed: ${message}`);
    }
    return null;
  }
}
