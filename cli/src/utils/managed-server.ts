// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { createHash, randomUUID } from 'crypto';
import { execFileSync } from 'child_process';
import { MACHINE_STORE_DIR_NAME } from './machine-credentials.js';
import { resolveHostRid, type Rid } from './rid.js';
import {
  DEFAULT_SERVER_VERSION,
  SERVER_EXECUTABLE_NAME,
  SERVER_RELEASE_REPO,
  serverReleaseTag,
} from './server-version.js';

/**
 * The CLI's MANAGED server directory — the machine-shared home the `install-plugin --with-server`
 * download lands in, and the directory `configure --agent` proxies to. Lives beside the shared
 * machine credential store (`~/.ai-game-dev/`) so the server binary is fetched once per machine,
 * not once per project, and is NEVER on PATH (design 06/09: the binary lives in the CLI's managed
 * dir). Layout mirrors the plugin's `Library/mcp-server/<rid>/` — one folder per RID + a `version`
 * marker.
 */
export function managedServerRootDir(homeDir: string = os.homedir()): string {
  return path.join(homeDir, MACHINE_STORE_DIR_NAME, 'server');
}

export function managedServerDir(rid: string, homeDir: string = os.homedir()): string {
  return path.join(managedServerRootDir(homeDir), rid);
}

/** The server executable file name for a RID (`.exe` suffix on Windows RIDs). */
export function managedServerBinaryName(rid: string): string {
  return rid.startsWith('win-') ? `${SERVER_EXECUTABLE_NAME}.exe` : SERVER_EXECUTABLE_NAME;
}

export function managedServerBinaryPath(rid: string, homeDir: string = os.homedir()): string {
  return path.join(managedServerDir(rid, homeDir), managedServerBinaryName(rid));
}

/** Absolute path of the `version` marker recording which server version is staged for a RID. */
export function managedServerVersionPath(rid: string, homeDir: string = os.homedir()): string {
  return path.join(managedServerDir(rid, homeDir), 'version');
}

/** The release-asset zip NAME for a RID (`gamedev-mcp-server-<rid>.zip`) — the SHA256SUMS key. */
export function serverZipName(rid: string): string {
  return `${SERVER_EXECUTABLE_NAME}-${rid}.zip`;
}

/** The GitHub release download URL of the per-RID server zip, pinned to `version`. */
export function serverZipUrl(rid: string, version: string): string {
  return `https://github.com/${SERVER_RELEASE_REPO}/releases/download/${serverReleaseTag(version)}/${serverZipName(rid)}`;
}

/** The GitHub release download URL of the `SHA256SUMS` integrity manifest, pinned to `version`. */
export function serverShaSumsUrl(version: string): string {
  return `https://github.com/${SERVER_RELEASE_REPO}/releases/download/${serverReleaseTag(version)}/SHA256SUMS`;
}

/**
 * Look up the expected SHA-256 (lowercase hex) for `fileName` in a `SHA256SUMS` manifest.
 * Accepts the canonical `<hex>  <name>` line shape and the `<hex> *<name>` binary-mode variant.
 * Returns null when no line names `fileName` (exact match — mirrors the C# exact-key Ordinal
 * lookup), so a missing entry fails closed at the call site.
 */
export function parseSha256Sums(manifest: string, fileName: string): string | null {
  for (const rawLine of manifest.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line) continue;
    const match = line.match(/^([0-9a-fA-F]{64})\s+\*?(.+)$/);
    if (!match) continue;
    const [, hash, name] = match;
    if (name.trim() === fileName) return hash.toLowerCase();
  }
  return null;
}

/** SHA-256 of a buffer as lowercase hex. */
export function sha256Hex(buffer: Buffer): string {
  return createHash('sha256').update(buffer).digest('hex');
}

// ---------------------------------------------------------------------------
// Archive extraction (runtime side; not exercised by the mocked unit gate)
// ---------------------------------------------------------------------------

/**
 * Extract a `.zip` into `destDir` using the best available platform archive tool. Node ships no
 * zip reader, so this shells out: bsdtar (`tar -xf`, present on Windows 10+ and macOS) and `unzip`
 * (POSIX) cover every supported host, with a PowerShell `Expand-Archive` fallback on Windows. The
 * caller verifies the extracted binary exists afterwards, so a silent no-op tool is still caught.
 */
export function extractZip(zipPath: string, destDir: string): void {
  fs.mkdirSync(destDir, { recursive: true });

  const strategies: Array<{ cmd: string; args: string[] }> =
    process.platform === 'win32'
      ? [
          { cmd: 'tar', args: ['-xf', zipPath, '-C', destDir] },
          {
            cmd: 'powershell.exe',
            args: [
              '-NoProfile',
              '-NonInteractive',
              '-Command',
              `Expand-Archive -LiteralPath ${JSON.stringify(zipPath)} -DestinationPath ${JSON.stringify(destDir)} -Force`,
            ],
          },
        ]
      : [
          { cmd: 'unzip', args: ['-o', zipPath, '-d', destDir] },
          { cmd: 'tar', args: ['-xf', zipPath, '-C', destDir] },
        ];

  let lastError: unknown;
  for (const strategy of strategies) {
    try {
      execFileSync(strategy.cmd, strategy.args, { stdio: 'ignore' });
      return;
    } catch (err) {
      lastError = err;
    }
  }
  throw new Error(
    `Failed to extract ${zipPath} into ${destDir}: no working archive tool ` +
      `(tried ${strategies.map((s) => s.cmd).join(', ')}). ` +
      (lastError instanceof Error ? lastError.message : String(lastError)),
  );
}

/** Shallow-search (root, then one level of subdirs) for `binaryName`; returns its directory. */
function locateBinaryDir(rootDir: string, binaryName: string): string | null {
  if (fs.existsSync(path.join(rootDir, binaryName))) return rootDir;
  for (const entry of fs.readdirSync(rootDir, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const candidate = path.join(rootDir, entry.name, binaryName);
    if (fs.existsSync(candidate)) return path.join(rootDir, entry.name);
  }
  return null;
}

function copyDirContents(srcDir: string, destDir: string): void {
  fs.mkdirSync(destDir, { recursive: true });
  for (const entry of fs.readdirSync(srcDir, { withFileTypes: true })) {
    const src = path.join(srcDir, entry.name);
    const dest = path.join(destDir, entry.name);
    if (entry.isDirectory()) {
      copyDirContents(src, dest);
    } else {
      fs.copyFileSync(src, dest);
    }
  }
}

// ---------------------------------------------------------------------------
// Download + verify + publish
// ---------------------------------------------------------------------------

export interface DownloadServerOptions {
  /** Host RID; defaults to `resolveHostRid()`. */
  rid?: string;
  /** Server version to fetch; defaults to `DEFAULT_SERVER_VERSION`. */
  version?: string;
  /**
   * Offline/CI escape hatch: a local zip path OR a URL to fetch the zip from. When set, the
   * download uses this source and the SHA256SUMS integrity gate is SKIPPED (explicit-trust
   * override — mirrors the addon `--source` pattern). Otherwise the pinned release zip is fetched
   * and verified fail-closed against the release's SHA256SUMS.
   */
  source?: string;
  /** Home directory override (tests). */
  homeDir?: string;
  /** `fetch` injection (tests). */
  fetchImpl?: typeof fetch;
  /** Extraction injection (tests) — `(zipPath, destDir) => void`. */
  extractImpl?: (zipPath: string, destDir: string) => void;
  /** Optional progress reporter. */
  onProgress?: (message: string) => void;
}

export interface DownloadServerResult {
  rid: string;
  version: string;
  /** Absolute path of the published server binary. */
  binaryPath: string;
  /** True when the zip passed SHA256SUMS verification; false for a `--server-source` override. */
  verified: boolean;
}

async function fetchBytes(url: string, doFetch: typeof fetch): Promise<Buffer> {
  const response = await doFetch(url);
  if (!response.ok) {
    throw new Error(`Download failed (HTTP ${response.status}) for ${url}`);
  }
  return Buffer.from(await response.arrayBuffer());
}

async function fetchText(url: string, doFetch: typeof fetch): Promise<string> {
  const response = await doFetch(url);
  if (!response.ok) {
    throw new Error(`Download failed (HTTP ${response.status}) for ${url}`);
  }
  return response.text();
}

/**
 * Download (or copy from `--server-source`), verify against the release SHA256SUMS (fail-closed),
 * extract, and atomically publish the pinned GameDev-MCP-Server binary for the host RID into the
 * CLI's managed directory. Never launches the binary. Returns the published path.
 */
export async function downloadServerBinary(
  opts: DownloadServerOptions = {},
): Promise<DownloadServerResult> {
  const rid: Rid | string = opts.rid ?? resolveHostRid();
  const version = opts.version ?? DEFAULT_SERVER_VERSION;
  const homeDir = opts.homeDir ?? os.homedir();
  const doFetch = opts.fetchImpl ?? fetch;
  const extract = opts.extractImpl ?? extractZip;
  const report = opts.onProgress ?? (() => {});
  const zipName = serverZipName(rid);

  // 1. Obtain the zip bytes.
  let zipBytes: Buffer;
  let verified: boolean;
  if (opts.source) {
    if (fs.existsSync(opts.source)) {
      report(`Using local server source: ${opts.source}`);
      zipBytes = fs.readFileSync(opts.source);
    } else {
      report(`Downloading server from source URL: ${opts.source}`);
      zipBytes = await fetchBytes(opts.source, doFetch);
    }
    verified = false; // explicit-trust override — no release SHA256SUMS to verify against
  } else {
    const zipUrl = serverZipUrl(rid, version);
    report(`Downloading ${zipName} (v${version})...`);
    zipBytes = await fetchBytes(zipUrl, doFetch);

    // FAIL-CLOSED INTEGRITY GATE (verify-before-extract). The zip is UNTRUSTED until its SHA-256
    // matches the release's SHA256SUMS entry for THIS RID. A missing/mismatched/unfetchable
    // manifest aborts WITHOUT extracting — an unverified binary must never be published.
    const sumsUrl = serverShaSumsUrl(version);
    report('Verifying checksum against release SHA256SUMS...');
    const manifest = await fetchText(sumsUrl, doFetch);
    const expected = parseSha256Sums(manifest, zipName);
    if (!expected) {
      throw new Error(
        `Integrity check failed: no SHA256SUMS entry for ${zipName} in the v${version} release. ` +
          `Refusing to install an unverified server binary.`,
      );
    }
    const actual = sha256Hex(zipBytes);
    if (actual !== expected) {
      throw new Error(
        `Integrity check FAILED for ${zipName}: expected ${expected}, got ${actual}. ` +
          `Refusing to install a tampered or corrupt server binary.`,
      );
    }
    verified = true;
  }

  // 2. Stage into a same-root temp dir, extract, locate the binary.
  const rootDir = managedServerRootDir(homeDir);
  fs.mkdirSync(rootDir, { recursive: true });
  const stagingDir = path.join(rootDir, `.staging-${rid}-${randomUUID()}`);
  const tempZip = path.join(rootDir, `.${SERVER_EXECUTABLE_NAME}-${rid}-${randomUUID()}.zip`);
  try {
    fs.writeFileSync(tempZip, zipBytes);
    fs.mkdirSync(stagingDir, { recursive: true });
    report('Extracting server archive...');
    extract(tempZip, stagingDir);

    const binaryName = managedServerBinaryName(rid);
    const binaryDir = locateBinaryDir(stagingDir, binaryName);
    if (!binaryDir) {
      throw new Error(
        `Extracted archive did not contain '${binaryName}'. The '${zipName}' asset layout may have changed.`,
      );
    }

    // 3. Publish: replace the per-RID cache folder with the staged payload, write the version
    //    marker, and set the exec bit on POSIX so the payload is launch-ready.
    const destDir = managedServerDir(rid, homeDir);
    fs.rmSync(destDir, { recursive: true, force: true });
    copyDirContents(binaryDir, destDir);

    const binaryPath = managedServerBinaryPath(rid, homeDir);
    if (process.platform !== 'win32') {
      try {
        fs.chmodSync(binaryPath, 0o755);
      } catch {
        /* best effort */
      }
    }
    fs.writeFileSync(managedServerVersionPath(rid, homeDir), version);

    report(`Server binary ready: ${binaryPath}`);
    return { rid, version, binaryPath, verified };
  } finally {
    fs.rmSync(tempZip, { force: true });
    fs.rmSync(stagingDir, { recursive: true, force: true });
  }
}

// ---------------------------------------------------------------------------
// configure --agent proxy
// ---------------------------------------------------------------------------

export interface ConfigureProxyOptions {
  agentId: string;
  projectPath: string;
  /** Optional explicit server URL forwarded to the binary's `configure --url`. */
  url?: string;
  rid?: string;
  homeDir?: string;
  /** Runner injection (tests) — `(binaryPath, args, cwd) => void`. */
  runImpl?: (binaryPath: string, args: string[], cwd: string) => void;
}

export interface ConfigureProxyResult {
  binaryPath: string;
  args: string[];
  cwd: string;
}

/**
 * Proxy `configure --agent <id>` to the managed GameDev-MCP-Server binary's `configure`
 * subcommand (design 06/09 Phase 3) so the shared C# configurator registry — with the derived
 * `port=` + `project=` pin — is reachable from the terminal. The binary derives the pin/port from
 * its working directory, so we run it with `cwd` = the resolved project path. When no managed
 * binary is installed, throws a clear, actionable error pointing at `install-plugin --with-server`.
 */
export function proxyConfigure(opts: ConfigureProxyOptions): ConfigureProxyResult {
  const rid = opts.rid ?? resolveHostRid();
  const binaryPath = managedServerBinaryPath(rid, opts.homeDir);
  if (!fs.existsSync(binaryPath)) {
    throw new Error(
      `No managed GameDev-MCP-Server binary found at ${binaryPath}. ` +
        `Run 'unity-mcp-cli install-plugin --with-server' first to download it.`,
    );
  }
  const args = ['configure', '--agent', opts.agentId, ...(opts.url ? ['--url', opts.url] : [])];
  const cwd = path.resolve(opts.projectPath);
  const run =
    opts.runImpl ?? ((bin, a, workDir) => execFileSync(bin, a, { cwd: workDir, stdio: 'inherit' }));
  run(binaryPath, args, cwd);
  return { binaryPath, args, cwd };
}
