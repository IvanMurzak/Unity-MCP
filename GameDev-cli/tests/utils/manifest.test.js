import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';

vi.mock('fs');

import fs from 'fs';
import {
  fetchLatestVersion,
  installUnityMcp,
  PACKAGE_ID,
  REGISTRY_NAME,
  REGISTRY_URL,
  REQUIRED_SCOPES,
  MANIFEST_RELATIVE_PATH,
} from '../../src/utils/manifest.js';

const FAKE_PROJECT = '/fake/unity/project';
const MANIFEST_PATH = path.join(FAKE_PROJECT, MANIFEST_RELATIVE_PATH);

// ---------------------------------------------------------------------------
// fetchLatestVersion
// ---------------------------------------------------------------------------
describe('fetchLatestVersion', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns the "latest" dist-tag version from OpenUPM', async () => {
    const mockResponse = {
      ok: true,
      json: async () => ({
        'dist-tags': { latest: '1.2.3' },
        versions: { '1.2.3': {}, '1.1.0': {} },
      }),
    };
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(mockResponse));

    const version = await fetchLatestVersion();
    expect(version).toBe('1.2.3');
  });

  it('falls back to the highest version key when dist-tags is absent', async () => {
    const mockResponse = {
      ok: true,
      json: async () => ({
        versions: { '0.5.0': {}, '0.10.0': {}, '0.9.0': {} },
      }),
    };
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(mockResponse));

    const version = await fetchLatestVersion();
    expect(version).toBe('0.10.0');
  });

  it('falls back to default version on HTTP error', async () => {
    const mockResponse = { ok: false, status: 503 };
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(mockResponse));
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const version = await fetchLatestVersion();

    expect(version).toBe('0.48.1');
    expect(warnSpy).toHaveBeenCalled();
    warnSpy.mockRestore();
  });

  it('falls back to default version when fetch throws a network error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('ENOTFOUND')));
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const version = await fetchLatestVersion();

    expect(version).toBe('0.48.1');
    warnSpy.mockRestore();
  });

  it('falls back to default version when response has no versions', async () => {
    const mockResponse = {
      ok: true,
      json: async () => ({ versions: {} }),
    };
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(mockResponse));
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const version = await fetchLatestVersion();

    expect(version).toBe('0.48.1');
    warnSpy.mockRestore();
  });
});

// ---------------------------------------------------------------------------
// installUnityMcp
// ---------------------------------------------------------------------------
describe('installUnityMcp', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(console, 'log').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const baseManifest = () => ({
    dependencies: {},
    scopedRegistries: [],
  });

  it('throws when manifest.json does not exist', () => {
    fs.existsSync.mockReturnValue(false);

    expect(() => installUnityMcp(FAKE_PROJECT, '1.0.0')).toThrow(
      /manifest\.json not found/
    );
  });

  it('throws when manifest.json is invalid JSON', () => {
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue('not json');

    expect(() => installUnityMcp(FAKE_PROJECT, '1.0.0')).toThrow(
      /Failed to parse/
    );
  });

  it('adds the OpenUPM scoped registry when not present', () => {
    const manifest = baseManifest();
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    const registry = parsed.scopedRegistries.find(r => r.url === REGISTRY_URL);
    expect(registry).toBeDefined();
    expect(registry.name).toBe(REGISTRY_NAME);
  });

  it('adds all required scopes to the registry', () => {
    const manifest = baseManifest();
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    const registry = parsed.scopedRegistries.find(r => r.url === REGISTRY_URL);
    for (const scope of REQUIRED_SCOPES) {
      expect(registry.scopes).toContain(scope);
    }
  });

  it('does not duplicate scopes that already exist', () => {
    const manifest = {
      ...baseManifest(),
      scopedRegistries: [{
        name: REGISTRY_NAME,
        url: REGISTRY_URL,
        scopes: [REQUIRED_SCOPES[0]],
      }],
    };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    const registry = parsed.scopedRegistries.find(r => r.url === REGISTRY_URL);
    const firstScope = REQUIRED_SCOPES[0];
    const count = registry.scopes.filter(s => s === firstScope).length;
    expect(count).toBe(1);
  });

  it('adds the package dependency at the specified version', () => {
    const manifest = baseManifest();
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '0.48.1');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    expect(parsed.dependencies[PACKAGE_ID]).toBe('0.48.1');
  });

  it('updates an existing dependency to the new version', () => {
    const manifest = {
      ...baseManifest(),
      dependencies: { [PACKAGE_ID]: '0.40.0' },
    };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '0.48.1');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    expect(parsed.dependencies[PACKAGE_ID]).toBe('0.48.1');
  });

  it('logs a message when the package is already at the requested version', () => {
    const manifest = {
      ...baseManifest(),
      dependencies: { [PACKAGE_ID]: '0.48.1' },
    };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});
    const logSpy = vi.spyOn(console, 'log');

    installUnityMcp(FAKE_PROJECT, '0.48.1');

    expect(logSpy).toHaveBeenCalledWith(
      expect.stringContaining('already present')
    );
  });

  it('writes the manifest with 2-space indentation and trailing newline', () => {
    const manifest = baseManifest();
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [writePath, content, encoding] = fs.writeFileSync.mock.calls[0];
    expect(writePath).toBe(MANIFEST_PATH);
    expect(content).toMatch(/^\{/);
    expect(content.endsWith('\n')).toBe(true);
    expect(content).toContain('  '); // 2-space indent present
    expect(encoding).toBe('utf-8');
  });

  it('creates a dependencies object if manifest had none', () => {
    const manifest = { scopedRegistries: [] }; // no dependencies key
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    expect(parsed.dependencies).toBeDefined();
    expect(parsed.dependencies[PACKAGE_ID]).toBe('1.0.0');
  });

  it('reuses existing registry matched by URL regardless of name', () => {
    const manifest = {
      ...baseManifest(),
      scopedRegistries: [{
        name: 'some-other-name',
        url: REGISTRY_URL,
        scopes: [],
      }],
    };
    fs.existsSync.mockReturnValue(true);
    fs.readFileSync.mockReturnValue(JSON.stringify(manifest));
    fs.writeFileSync.mockImplementation(() => {});

    installUnityMcp(FAKE_PROJECT, '1.0.0');

    const [, written] = fs.writeFileSync.mock.calls[0];
    const parsed = JSON.parse(written);
    expect(parsed.scopedRegistries.length).toBe(1);
  });
});
