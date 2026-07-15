// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { execFileSync } from 'child_process';

/**
 * TypeScript client of the shared machine credential store — the same on-disk contract the
 * plugin's C# `MachineCredentialStore` (MCP-Plugin-dotnet, com.IvanMurzak.McpPlugin.AgentConfig)
 * reads and writes. A single ai-game.dev account credential lives once per machine at
 * `~/.ai-game-dev/credentials.json`, so `login` writes it here and every engine plugin/CLI reads
 * it — sign-in happens once per machine, never per project, and the credential is NEVER written
 * into a project file / VCS.
 *
 * At-rest protection matches the C# store byte-for-byte so the plugin can read what the CLI wrote:
 *   - POSIX  — plaintext JSON, file mode 0600, inside a 0700 directory.
 *   - Windows — DPAPI-encrypted (CurrentUser scope, no entropy) via
 *     System.Security.Cryptography.ProtectedData, invoked through PowerShell. This is
 *     interoperable with the C# store's CryptProtectData/CryptUnprotectData (the description
 *     string and CRYPTPROTECT_UI_FORBIDDEN flag do not affect decryptability).
 */

/** Directory name under the user home (or a project root) that holds the store. */
export const MACHINE_STORE_DIR_NAME = '.ai-game-dev';

/** File name of the secret credential document. */
export const CREDENTIALS_FILE_NAME = 'credentials.json';

/**
 * The secret credential material persisted in the store. Mirrors the C# `MachineCredentials`
 * schema (camelCase JSON keys). Unknown fields are preserved on read for forward-compatibility.
 */
export interface MachineCredentials {
  /** Schema version of the persisted document (currently 1). */
  version?: number;
  /** The current short-lived JWT access token (MCP audience). */
  accessToken?: string;
  /** The rotating refresh token used to mint a new access token before `expiresAt`. */
  refreshToken?: string;
  /** ISO-8601 absolute expiry of `accessToken`; used to schedule proactive refresh. */
  expiresAt?: string;
  /** The server target the credential was issued for (hosted https://ai-game.dev or a local URL). */
  serverTarget?: string;
  /** The account id (`sub`) the credential resolves to. Audit/diagnostic only. */
  subject?: string;
  [key: string]: unknown;
}

const isWindows = process.platform === 'win32';

/**
 * The shared machine credential store. Defaults to `~/.ai-game-dev/`; pass an explicit
 * `baseDirectory` for tests or for the `--project` per-project store
 * (`<project>/.ai-game-dev/`).
 */
export class MachineCredentialStore {
  private readonly _baseDirectory: string;

  constructor(baseDirectory?: string) {
    this._baseDirectory = baseDirectory ?? path.join(os.homedir(), MACHINE_STORE_DIR_NAME);
  }

  /** Absolute path of the store directory. */
  get baseDirectory(): string {
    return this._baseDirectory;
  }

  /** Absolute path of the secret credential file. */
  get credentialsPath(): string {
    return path.join(this._baseDirectory, CREDENTIALS_FILE_NAME);
  }

  /** True when a credential file exists in the store. */
  get exists(): boolean {
    return fs.existsSync(this.credentialsPath);
  }

  /**
   * Encrypt (Windows) / restrict (POSIX) and write `credentials` to the store, creating the
   * store directory with owner-only permissions if needed. `version` is always written as 1;
   * undefined fields are omitted (matching the C# `WhenWritingNull` policy).
   */
  write(credentials: MachineCredentials): void {
    this.ensureBaseDirectory();

    const document: MachineCredentials = { ...credentials, version: 1 };
    const json = JSON.stringify(document, null, 2);
    const plaintext = Buffer.from(json, 'utf-8');
    const bytes = isWindows ? dpapiTransform('Protect', plaintext) : plaintext;

    fs.writeFileSync(this.credentialsPath, bytes);
    if (!isWindows) {
      fs.chmodSync(this.credentialsPath, 0o600);
    }
  }

  /** Read and decrypt the stored credentials, or null when none are present. */
  read(): MachineCredentials | null {
    if (!fs.existsSync(this.credentialsPath)) {
      return null;
    }

    const raw = fs.readFileSync(this.credentialsPath);
    if (raw.length === 0) {
      return null;
    }

    const plaintext = isWindows ? dpapiTransform('Unprotect', raw) : raw;
    const json = plaintext.toString('utf-8');
    if (json.trim().length === 0) {
      return null;
    }

    return JSON.parse(json) as MachineCredentials;
  }

  /** Delete the stored credentials (sign-out). No-op when none exist. */
  delete(): void {
    if (fs.existsSync(this.credentialsPath)) {
      fs.rmSync(this.credentialsPath);
    }
  }

  private ensureBaseDirectory(): void {
    fs.mkdirSync(this._baseDirectory, { recursive: true });
    if (!isWindows) {
      fs.chmodSync(this._baseDirectory, 0o700);
    }
  }
}

/**
 * Run a Windows DPAPI Protect/Unprotect round trip through PowerShell's
 * System.Security.Cryptography.ProtectedData (CurrentUser scope, no entropy) — interoperable
 * with the C# store's CryptProtectData/CryptUnprotectData. Input and output are passed as
 * base64 through an environment variable so the plaintext never lands in argv or the process
 * table. Only ever invoked on Windows.
 */
function dpapiTransform(action: 'Protect' | 'Unprotect', input: Buffer): Buffer {
  const script =
    "$ErrorActionPreference='Stop';" +
    'Add-Type -AssemblyName System.Security;' +
    '$in=[Convert]::FromBase64String($env:AIGD_DPAPI_IN);' +
    `$out=[System.Security.Cryptography.ProtectedData]::${action}($in,$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser);` +
    '[Convert]::ToBase64String($out)';

  const stdout = execFileSync(
    'powershell.exe',
    ['-NoProfile', '-NonInteractive', '-Command', script],
    {
      encoding: 'utf-8',
      env: { ...process.env, AIGD_DPAPI_IN: input.toString('base64') },
      timeout: 20000,
      windowsHide: true,
    },
  );

  return Buffer.from(stdout.trim(), 'base64');
}
