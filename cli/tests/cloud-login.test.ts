// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

// Mock the device-auth flow so runCloudLogin never touches the network or opens a browser.
// The mock resolves success WITHOUT invoking the onUserCode / onPolling callbacks, so
// openBrowser and the spinner are never reached.
vi.mock('../src/utils/auth.js', () => ({
  deviceAuthFlow: vi.fn(async () => ({ success: true, accessToken: 'test-access-token' })),
}));

import { runCloudLogin } from '../src/utils/cloud-login.js';
import { MachineCredentialStore } from '../src/utils/machine-credentials.js';

describe('runCloudLogin', () => {
  it('persists the access token to the credential store (never a project config file)', async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cloudlogin-'));
    try {
      const store = new MachineCredentialStore(path.join(tmp, '.ai-game-dev'));

      const token = await runCloudLogin(store);

      expect(token).toBe('test-access-token');
      expect(store.exists).toBe(true);

      const creds = store.read();
      expect(creds?.accessToken).toBe('test-access-token');
      expect(creds?.serverTarget).toBe('https://ai-game.dev');
      expect(creds?.version).toBe(1);

      // The repoint must NOT write the legacy per-project cloudToken config.
      expect(
        fs.existsSync(path.join(tmp, 'UserSettings', 'AI-Game-Developer-Config.json')),
      ).toBe(false);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});
