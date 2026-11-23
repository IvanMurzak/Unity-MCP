// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import fsMutable from 'fs';
import { syncBuiltinESMExports } from 'module';
import * as os from 'os';
import * as path from 'path';
import { createHash } from 'crypto';
import {
  parseSha256Sums,
  sha256Hex,
  serverZipName,
  serverZipUrl,
  serverShaSumsUrl,
  managedServerBinaryPath,
  managedServerDir,
  managedServerVersionPath,
  downloadServerBinary,
  proxyConfigure,
} from '../src/utils/managed-server.js';

const RID = 'linux-x64'; // binary name has no `.exe` on any host — deterministic across CI + Windows

function tmpDir(prefix: string): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), prefix));
}

/** A fetch double that serves `.zip` bytes and the `SHA256SUMS` manifest by URL suffix. */
function makeFetch(zipBytes: Buffer, sumsText: string | null): typeof fetch {
  return (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('.zip')) {
      return new Response(zipBytes, { status: 200 });
    }
    if (url.endsWith('/SHA256SUMS')) {
      if (sumsText === null) return new Response('not found', { status: 404 });
      return new Response(sumsText, { status: 200 });
    }
    return new Response('not found', { status: 404 });
  }) as unknown as typeof fetch;
}

/** An extraction double that drops a fake server binary into the staging dir. */
function fakeExtract(binaryName: string): (zipPath: string, destDir: string) => void {
  return (_zipPath, destDir) => {
    fs.mkdirSync(destDir, { recursive: true });
    fs.writeFileSync(path.join(destDir, binaryName), 'fake-binary');
    fs.writeFileSync(path.join(destDir, 'sidecar.dll'), 'fake-sidecar');
  };
}

describe('parseSha256Sums', () => {
  const sums =
    'aaaa000000000000000000000000000000000000000000000000000000000000  gamedev-mcp-server-win-x64.zip\n' +
    'bbbb111111111111111111111111111111111111111111111111111111111111  gamedev-mcp-server-linux-x64.zip\n';

  it('finds the hash for an exact file name', () => {
    expect(parseSha256Sums(sums, 'gamedev-mcp-server-linux-x64.zip')).toBe(
      'bbbb111111111111111111111111111111111111111111111111111111111111',
    );
  });
  it('returns null when the file name is absent', () => {
    expect(parseSha256Sums(sums, 'gamedev-mcp-server-osx-arm64.zip')).toBeNull();
  });
  it('handles the binary-mode "hex *name" variant and lowercases the hash', () => {
    const upper = 'CCCC222222222222222222222222222222222222222222222222222222222222 *file.zip';
    expect(parseSha256Sums(upper, 'file.zip')).toBe(
      'cccc222222222222222222222222222222222222222222222222222222222222',
    );
  });
});

describe('URL + name helpers', () => {
  it('builds the zip name / url / SHA256SUMS url from RID + version', () => {
    expect(serverZipName('win-x64')).toBe('gamedev-mcp-server-win-x64.zip');
    expect(serverZipUrl('win-x64', '9.1.0')).toBe(
      'https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v9.1.0/gamedev-mcp-server-win-x64.zip',
    );
    expect(serverShaSumsUrl('9.1.0')).toBe(
      'https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v9.1.0/SHA256SUMS',
    );
  });
});

describe('downloadServerBinary (verify-before-extract)', () => {
  it('downloads, verifies the checksum, extracts, and publishes the binary', async () => {
    const home = tmpDir('mgs-ok-');
    try {
      const zipBytes = Buffer.from('the-real-zip-bytes');
      const hash = createHash('sha256').update(zipBytes).digest('hex');
      const sums = `${hash}  ${serverZipName(RID)}\n`;

      const result = await downloadServerBinary({
        rid: RID,
        version: '9.1.0',
        homeDir: home,
        fetchImpl: makeFetch(zipBytes, sums),
        extractImpl: fakeExtract('gamedev-mcp-server'),
      });

      expect(result.verified).toBe(true);
      expect(result.rid).toBe(RID);
      expect(result.binaryPath).toBe(managedServerBinaryPath(RID, home));
      expect(fs.existsSync(result.binaryPath)).toBe(true);
      expect(fs.readFileSync(managedServerVersionPath(RID, home), 'utf-8')).toBe('9.1.0');
      // Sidecars are published alongside the binary.
      expect(fs.existsSync(path.join(managedServerDir(RID, home), 'sidecar.dll'))).toBe(true);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
    }
  });

  it('FAILS CLOSED on a checksum mismatch — nothing is published', async () => {
    const home = tmpDir('mgs-bad-');
    try {
      const zipBytes = Buffer.from('tampered-bytes');
      // A hash for DIFFERENT content — the downloaded bytes will not match.
      const wrongHash = createHash('sha256').update('other-content').digest('hex');
      const sums = `${wrongHash}  ${serverZipName(RID)}\n`;

      await expect(
        downloadServerBinary({
          rid: RID,
          version: '9.1.0',
          homeDir: home,
          fetchImpl: makeFetch(zipBytes, sums),
          extractImpl: fakeExtract('gamedev-mcp-server'),
        }),
      ).rejects.toThrow(/Integrity check FAILED/);

      expect(fs.existsSync(managedServerBinaryPath(RID, home))).toBe(false);
      expect(fs.existsSync(managedServerDir(RID, home))).toBe(false);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
    }
  });

  it('FAILS CLOSED when the SHA256SUMS manifest has no entry for the RID', async () => {
    const home = tmpDir('mgs-noentry-');
    try {
      const zipBytes = Buffer.from('bytes');
      const sums = 'deadbeef00000000000000000000000000000000000000000000000000000000  some-other-file.zip\n';

      await expect(
        downloadServerBinary({
          rid: RID,
          version: '9.1.0',
          homeDir: home,
          fetchImpl: makeFetch(zipBytes, sums),
          extractImpl: fakeExtract('gamedev-mcp-server'),
        }),
      ).rejects.toThrow(/no SHA256SUMS entry/);

      expect(fs.existsSync(managedServerDir(RID, home))).toBe(false);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
    }
  });

  it('honours a local --server-source override (skips checksum verification)', async () => {
    const home = tmpDir('mgs-src-');
    const srcDir = tmpDir('mgs-srczip-');
    try {
      const localZip = path.join(srcDir, 'local-server.zip');
      fs.writeFileSync(localZip, 'local-zip-bytes');

      const result = await downloadServerBinary({
        rid: RID,
        version: '9.1.0',
        homeDir: home,
        source: localZip,
        // fetch must never be called for a local source — make it throw if it is.
        fetchImpl: (() => {
          throw new Error('network should not be used for a local --server-source');
        }) as unknown as typeof fetch,
        extractImpl: fakeExtract('gamedev-mcp-server'),
      });

      expect(result.verified).toBe(false);
      expect(fs.existsSync(result.binaryPath)).toBe(true);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
      fs.rmSync(srcDir, { recursive: true, force: true });
    }
  });
});

describe('downloadServerBinary (preserve the previous installation)', () => {
  let home: string;
  let dest: string;
  beforeEach(() => {
    home = tmpDir('mgs-update-');
    dest = managedServerDir(RID, home);
    fs.mkdirSync(dest, { recursive: true });
    fs.writeFileSync(managedServerBinaryPath(RID, home), 'old-binary');
    fs.writeFileSync(managedServerVersionPath(RID, home), '9.1.0');
    fs.writeFileSync(path.join(dest, 'sidecar.dll'), 'old-sidecar');
    fs.writeFileSync(path.join(dest, 'obsolete.dll'), 'obsolete');
  });
  afterEach(() => {
    vi.restoreAllMocks();
    syncBuiltinESMExports();
    fs.rmSync(home, { recursive: true, force: true });
  });
  function update(extractImpl = fakeExtract('gamedev-mcp-server')) {
    const zip = Buffer.from('updated-zip');
    return downloadServerBinary({
      rid: RID, version: '9.2.0', homeDir: home, extractImpl,
      fetchImpl: makeFetch(zip, `${sha256Hex(zip)}  ${serverZipName(RID)}\n`),
    });
  }
  function expectOldInstallation(dir = dest) {
    expect(fs.readFileSync(path.join(dir, 'gamedev-mcp-server'), 'utf8')).toBe('old-binary');
    expect(fs.readFileSync(path.join(dir, 'version'), 'utf8')).toBe('9.1.0');
    expect(fs.readFileSync(path.join(dir, 'sidecar.dll'), 'utf8')).toBe('old-sidecar');
    expect(fs.readFileSync(path.join(dir, 'obsolete.dll'), 'utf8')).toBe('obsolete');
  }
  function expectNoTemporaryFiles() {
    expect(fs.readdirSync(path.dirname(dest))).toEqual([RID]);
  }

  it('keeps the old installation if writing the new version marker fails', async () => {
    const write = fsMutable.writeFileSync;
    vi.spyOn(fsMutable, 'writeFileSync').mockImplementation((file, data, options) => {
      if (path.basename(String(file)) === 'version' && data === '9.2.0') {
        throw Object.assign(new Error('disk full'), { code: 'ENOSPC' });
      }
      return write(file, data, options);
    });
    syncBuiltinESMExports();
    await expect(update()).rejects.toThrow('disk full');
    expectOldInstallation();
    expectNoTemporaryFiles();
  });

  it('restores the old installation if publishing the prepared payload fails', async () => {
    const rename = fsMutable.renameSync;
    vi.spyOn(fsMutable, 'renameSync').mockImplementation((from, to) => {
      if (String(to) === dest && String(from).includes('.staging-')) throw new Error('publish denied');
      return rename(from, to);
    });
    syncBuiltinESMExports();
    await expect(update()).rejects.toThrow('publish denied');
    expectOldInstallation();
    expectNoTemporaryFiles();
  });

  it('leaves the old installation untouched when it cannot be renamed', async () => {
    const rename = fsMutable.renameSync;
    vi.spyOn(fsMutable, 'renameSync').mockImplementation((from, to) => {
      if (String(from) === dest) throw new Error('old server is locked');
      return rename(from, to);
    });
    syncBuiltinESMExports();
    await expect(update()).rejects.toThrow('old server is locked');
    expectOldInstallation();
    expectNoTemporaryFiles();
  });

  it('retains the backup and reports its path if rollback also fails', async () => {
    const rename = fsMutable.renameSync;
    vi.spyOn(fsMutable, 'renameSync').mockImplementation((from, to) => {
      if (String(to) === dest) throw new Error('destination unavailable');
      return rename(from, to);
    });
    syncBuiltinESMExports();
    const error = await update().catch(err => err as Error);
    const remaining = fs.readdirSync(path.dirname(dest));
    expect(remaining).toHaveLength(1);
    const backup = path.join(path.dirname(dest), remaining[0]);
    expect(error).toBeInstanceOf(Error);
    expect(String(error)).toContain(backup);
    expectOldInstallation(backup);
  });

  it('publishes a complete nested payload and removes the old files and backup', async () => {
    await update((zip, dir) => fakeExtract('gamedev-mcp-server')(zip, path.join(dir, 'payload')));
    expect(fs.readFileSync(managedServerBinaryPath(RID, home), 'utf8')).toBe('fake-binary');
    expect(fs.readFileSync(managedServerVersionPath(RID, home), 'utf8')).toBe('9.2.0');
    expect(fs.readFileSync(path.join(dest, 'sidecar.dll'), 'utf8')).toBe('fake-sidecar');
    expect(fs.existsSync(path.join(dest, 'obsolete.dll'))).toBe(false);
    expectNoTemporaryFiles();
  });

  it('keeps a successful installation when deleting the old backup fails', async () => {
    const remove = fsMutable.rmSync;
    vi.spyOn(fsMutable, 'rmSync').mockImplementation((file, options) => {
      if (String(file).includes('.backup-')) throw new Error('backup locked');
      return remove(file, options);
    });
    syncBuiltinESMExports();
    await expect(update()).resolves.toMatchObject({ version: '9.2.0' });
    expect(fs.readFileSync(managedServerVersionPath(RID, home), 'utf8')).toBe('9.2.0');
    const backup = fs.readdirSync(path.dirname(dest)).find(name => name.startsWith('.backup-'));
    expect(backup).toBeDefined();
    expectOldInstallation(path.join(path.dirname(dest), backup!));
  });

  it('leaves no partial installation if the first publication fails', async () => {
    fs.rmSync(dest, { recursive: true, force: true });
    const rename = fsMutable.renameSync;
    vi.spyOn(fsMutable, 'renameSync').mockImplementation((from, to) => {
      if (String(to) === dest) throw new Error('first publication denied');
      return rename(from, to);
    });
    syncBuiltinESMExports();
    await expect(update()).rejects.toThrow('first publication denied');
    expect(fs.existsSync(dest)).toBe(false);
    expect(fs.readdirSync(path.dirname(dest))).toEqual([]);
  });

  it.skipIf(process.platform === 'win32')('keeps the old installation if making the new binary executable fails', async () => {
    vi.spyOn(fsMutable, 'chmodSync').mockImplementation(() => { throw new Error('chmod denied'); });
    syncBuiltinESMExports();
    await expect(update()).rejects.toThrow('chmod denied');
    expectOldInstallation();
    expectNoTemporaryFiles();
  });
});

describe('sha256Hex', () => {
  it('matches Node crypto', () => {
    const buf = Buffer.from('hello');
    expect(sha256Hex(buf)).toBe(createHash('sha256').update(buf).digest('hex'));
  });
});

describe('proxyConfigure', () => {
  it('runs the managed binary with the configure args and project cwd', () => {
    const home = tmpDir('mgs-cfg-');
    const project = tmpDir('mgs-proj-');
    try {
      // Plant a fake managed binary at the expected path.
      const dir = managedServerDir(RID, home);
      fs.mkdirSync(dir, { recursive: true });
      const binPath = managedServerBinaryPath(RID, home);
      fs.writeFileSync(binPath, 'fake');

      const calls: Array<{ bin: string; args: string[]; cwd: string }> = [];
      const result = proxyConfigure({
        agentId: 'claude-code',
        projectPath: project,
        url: 'https://ai-game.dev/mcp',
        rid: RID,
        homeDir: home,
        runImpl: (bin, args, cwd) => calls.push({ bin, args, cwd }),
      });

      expect(calls).toHaveLength(1);
      expect(calls[0].bin).toBe(binPath);
      expect(calls[0].args).toEqual(['configure', '--agent', 'claude-code', '--url', 'https://ai-game.dev/mcp']);
      expect(calls[0].cwd).toBe(path.resolve(project));
      expect(result.binaryPath).toBe(binPath);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('omits --url when none is supplied', () => {
    const home = tmpDir('mgs-cfg2-');
    const project = tmpDir('mgs-proj2-');
    try {
      fs.mkdirSync(managedServerDir(RID, home), { recursive: true });
      fs.writeFileSync(managedServerBinaryPath(RID, home), 'fake');

      const calls: string[][] = [];
      proxyConfigure({
        agentId: 'cursor',
        projectPath: project,
        rid: RID,
        homeDir: home,
        runImpl: (_bin, args) => calls.push(args),
      });
      expect(calls[0]).toEqual(['configure', '--agent', 'cursor']);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('throws an actionable error when no managed binary is installed', () => {
    const home = tmpDir('mgs-nobin-');
    const project = tmpDir('mgs-proj3-');
    try {
      let ran = false;
      expect(() =>
        proxyConfigure({
          agentId: 'claude-code',
          projectPath: project,
          rid: RID,
          homeDir: home,
          runImpl: () => {
            ran = true;
          },
        }),
      ).toThrow(/install-plugin --with-server/);
      expect(ran).toBe(false);
    } finally {
      fs.rmSync(home, { recursive: true, force: true });
      fs.rmSync(project, { recursive: true, force: true });
    }
  });
});
