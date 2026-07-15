// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  MachineCredentialStore,
  CREDENTIALS_FILE_NAME,
  MACHINE_STORE_DIR_NAME,
} from '../src/utils/machine-credentials.js';

function tmpStore(): { dir: string; store: MachineCredentialStore } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-store-'));
  return { dir, store: new MachineCredentialStore(dir) };
}

describe('MachineCredentialStore', () => {
  it('defaults to ~/.ai-game-dev/credentials.json', () => {
    const store = new MachineCredentialStore();
    expect(store.baseDirectory).toBe(path.join(os.homedir(), MACHINE_STORE_DIR_NAME));
    expect(store.credentialsPath).toBe(
      path.join(os.homedir(), MACHINE_STORE_DIR_NAME, CREDENTIALS_FILE_NAME),
    );
  });

  it('reports the credential path inside the store directory and no credential initially', () => {
    const { dir, store } = tmpStore();
    try {
      expect(store.credentialsPath).toBe(path.join(dir, CREDENTIALS_FILE_NAME));
      expect(store.exists).toBe(false);
      expect(store.read()).toBeNull();
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('write then read round-trips the credential and stamps version 1', () => {
    const { dir, store } = tmpStore();
    try {
      store.write({ accessToken: 'tok-123', serverTarget: 'https://ai-game.dev' });
      expect(store.exists).toBe(true);
      const creds = store.read();
      expect(creds?.accessToken).toBe('tok-123');
      expect(creds?.serverTarget).toBe('https://ai-game.dev');
      expect(creds?.version).toBe(1);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('omits undefined fields on write (WhenWritingNull parity)', () => {
    const { dir, store } = tmpStore();
    try {
      store.write({ accessToken: 'only-access' });
      const creds = store.read();
      expect(creds).not.toBeNull();
      expect(Object.prototype.hasOwnProperty.call(creds, 'refreshToken')).toBe(false);
      expect(Object.prototype.hasOwnProperty.call(creds, 'subject')).toBe(false);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('delete removes the credential file (sign-out)', () => {
    const { dir, store } = tmpStore();
    try {
      store.write({ accessToken: 'x' });
      expect(store.exists).toBe(true);
      store.delete();
      expect(store.exists).toBe(false);
      store.delete(); // idempotent no-op
      expect(store.exists).toBe(false);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  // POSIX: plaintext JSON, file mode 0600 inside a 0700 directory (matches the C# store).
  it.runIf(process.platform !== 'win32')(
    'writes plaintext JSON with 0600 file / 0700 dir perms on POSIX',
    () => {
      const { dir, store } = tmpStore();
      try {
        store.write({ accessToken: 'secret-token' });
        const raw = fs.readFileSync(store.credentialsPath, 'utf-8');
        expect(JSON.parse(raw).accessToken).toBe('secret-token');
        expect(fs.statSync(store.credentialsPath).mode & 0o777).toBe(0o600);
        expect(fs.statSync(dir).mode & 0o777).toBe(0o700);
      } finally {
        fs.rmSync(dir, { recursive: true, force: true });
      }
    },
  );

  // Windows: on-disk bytes are DPAPI-encrypted (not plaintext) yet read() round-trips.
  it.runIf(process.platform === 'win32')(
    'DPAPI-encrypts the on-disk bytes on Windows but round-trips via read()',
    () => {
      const { dir, store } = tmpStore();
      try {
        store.write({ accessToken: 'secret-token' });
        const raw = fs.readFileSync(store.credentialsPath, 'utf-8');
        expect(raw.includes('secret-token')).toBe(false);
        expect(store.read()?.accessToken).toBe('secret-token');
      } finally {
        fs.rmSync(dir, { recursive: true, force: true });
      }
    },
  );
});
