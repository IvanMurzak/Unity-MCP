// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
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
    expect(serverZipUrl('win-x64', '9.0.0')).toBe(
      'https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v9.0.0/gamedev-mcp-server-win-x64.zip',
    );
    expect(serverShaSumsUrl('9.0.0')).toBe(
      'https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v9.0.0/SHA256SUMS',
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
        version: '9.0.0',
        homeDir: home,
        fetchImpl: makeFetch(zipBytes, sums),
        extractImpl: fakeExtract('gamedev-mcp-server'),
      });

      expect(result.verified).toBe(true);
      expect(result.rid).toBe(RID);
      expect(result.binaryPath).toBe(managedServerBinaryPath(RID, home));
      expect(fs.existsSync(result.binaryPath)).toBe(true);
      expect(fs.readFileSync(managedServerVersionPath(RID, home), 'utf-8')).toBe('9.0.0');
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
          version: '9.0.0',
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
          version: '9.0.0',
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
        version: '9.0.0',
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
