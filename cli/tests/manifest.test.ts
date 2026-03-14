import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { addPluginToManifest, removePluginFromManifest, shouldUpdateVersion } from '../src/utils/manifest.js';

// --- shouldUpdateVersion tests (ported from C# VersionComparisonTests.cs) ---

describe('shouldUpdateVersion', () => {
  it('patch version higher — returns true', () => {
    expect(shouldUpdateVersion('1.5.1', '1.5.2')).toBe(true);
  });

  it('patch version lower — returns false', () => {
    expect(shouldUpdateVersion('1.5.2', '1.5.1')).toBe(false);
  });

  it('minor version higher — returns true', () => {
    expect(shouldUpdateVersion('1.5.0', '1.6.0')).toBe(true);
  });

  it('minor version lower — returns false', () => {
    expect(shouldUpdateVersion('1.6.0', '1.5.0')).toBe(false);
  });

  it('major version higher — returns true', () => {
    expect(shouldUpdateVersion('1.5.0', '2.0.0')).toBe(true);
  });

  it('major version lower — returns false', () => {
    expect(shouldUpdateVersion('2.0.0', '1.5.0')).toBe(false);
  });

  it('same version — returns false', () => {
    expect(shouldUpdateVersion('1.5.2', '1.5.2')).toBe(false);
  });

  it('empty current version — returns true', () => {
    expect(shouldUpdateVersion('', '1.5.2')).toBe(true);
  });

  it('empty new version — returns false', () => {
    expect(shouldUpdateVersion('1.5.2', '')).toBe(false);
  });
});

// --- addPluginToManifest tests (ported from C# ManifestInstallerTests.cs) ---

describe('addPluginToManifest', () => {
  let tmpDir: string;

  const PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
  const TEST_VERSION = '0.51.6';
  const REQUIRED_SCOPES = [
    'com.ivanmurzak',
    'extensions.unity',
    'org.nuget.com.ivanmurzak',
    'org.nuget.microsoft',
    'org.nuget.system',
    'org.nuget.r3',
  ];

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-test-'));
    fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  function writeManifest(manifest: object): void {
    fs.writeFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      JSON.stringify(manifest, null, 2)
    );
  }

  function readManifest(): Record<string, unknown> {
    const content = fs.readFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      'utf-8'
    );
    return JSON.parse(content);
  }

  function assertHasAllScopes(manifest: Record<string, unknown>): void {
    const registries = manifest.scopedRegistries as Array<{
      name: string;
      url: string;
      scopes: string[];
    }>;
    expect(registries).toBeDefined();
    expect(registries.length).toBeGreaterThanOrEqual(1);
    const openUpm = registries.find((r) => r.name === 'package.openupm.com');
    expect(openUpm).toBeDefined();
    expect(openUpm!.url).toBe('https://package.openupm.com');
    for (const scope of REQUIRED_SCOPES) {
      expect(openUpm!.scopes).toContain(scope);
    }
  }

  function assertHasDependency(manifest: Record<string, unknown>, version: string): void {
    const deps = manifest.dependencies as Record<string, string>;
    expect(deps).toBeDefined();
    expect(deps[PACKAGE_ID]).toBe(version);
  }

  // --- scopedRegistries missing entirely ---
  it('adds scoped registry when scopedRegistries is missing', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasAllScopes(result);
    assertHasDependency(result, TEST_VERSION);
  });

  // --- scopedRegistries is empty array ---
  it('adds scoped registry when scopedRegistries is empty', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
      scopedRegistries: [],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasAllScopes(result);
    assertHasDependency(result, TEST_VERSION);
  });

  // --- scopes array is empty ---
  it('adds all scopes when scopes array is empty', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: [],
        },
      ],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasAllScopes(result);
    assertHasDependency(result, TEST_VERSION);
  });

  // --- partial scopes (only 1 scope present) ---
  it('adds missing scopes when only com.ivanmurzak is present', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: ['com.ivanmurzak'],
        },
      ],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasAllScopes(result);
  });

  // --- partial scopes (4 scopes present) ---
  it('adds missing scopes when 4 of 6 are present', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: [
            'com.ivanmurzak',
            'extensions.unity',
            'org.nuget.com.ivanmurzak',
            'org.nuget.microsoft',
          ],
        },
      ],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasAllScopes(result);
  });

  // --- dependencies missing entirely ---
  it('adds dependencies when dependencies field is missing', () => {
    writeManifest({ scopedRegistries: [] });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasDependency(result, TEST_VERSION);
    assertHasAllScopes(result);
  });

  // --- version-aware: prevents downgrade ---
  it('does not downgrade when existing version is higher', () => {
    writeManifest({
      dependencies: { [PACKAGE_ID]: '99.0.0' },
      scopedRegistries: [],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    const deps = result.dependencies as Record<string, string>;
    expect(deps[PACKAGE_ID]).toBe('99.0.0');
  });

  // --- version-aware: allows upgrade ---
  it('upgrades when existing version is lower', () => {
    writeManifest({
      dependencies: { [PACKAGE_ID]: '0.0.1' },
      scopedRegistries: [],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasDependency(result, TEST_VERSION);
  });

  // --- no existing dependency: installs new ---
  it('installs new version when package is not in dependencies', () => {
    writeManifest({
      dependencies: {},
      scopedRegistries: [],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasDependency(result, TEST_VERSION);
  });

  // --- already at same version: no modification to version ---
  it('keeps same version when already installed at target', () => {
    writeManifest({
      dependencies: { [PACKAGE_ID]: TEST_VERSION },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: REQUIRED_SCOPES,
        },
      ],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    assertHasDependency(result, TEST_VERSION);
  });

  // --- preserves existing dependencies ---
  it('preserves other existing dependencies', () => {
    writeManifest({
      dependencies: {
        'com.unity.ugui': '1.0.0',
        'com.unity.test-framework': '1.1.33',
      },
      scopedRegistries: [],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    const deps = result.dependencies as Record<string, string>;
    expect(deps['com.unity.ugui']).toBe('1.0.0');
    expect(deps['com.unity.test-framework']).toBe('1.1.33');
    expect(deps[PACKAGE_ID]).toBe(TEST_VERSION);
  });

  // --- preserves existing registries ---
  it('preserves other existing scoped registries', () => {
    writeManifest({
      dependencies: {},
      scopedRegistries: [
        {
          name: 'other-registry',
          url: 'https://other.example.com',
          scopes: ['com.other'],
        },
      ],
    });
    addPluginToManifest(tmpDir, TEST_VERSION);
    const result = readManifest();
    const registries = result.scopedRegistries as Array<{ name: string }>;
    expect(registries.length).toBe(2);
    expect(registries.find((r) => r.name === 'other-registry')).toBeDefined();
    expect(registries.find((r) => r.name === 'package.openupm.com')).toBeDefined();
  });

  // --- throws on missing manifest ---
  it('throws when manifest.json does not exist', () => {
    const nonexistentPath = path.join(tmpDir, 'nonexistent');
    expect(() => addPluginToManifest(nonexistentPath, TEST_VERSION)).toThrow(
      /manifest\.json not found/
    );
  });
});

// --- removePluginFromManifest tests ---

describe('removePluginFromManifest', () => {
  let tmpDir: string;

  const PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
  const TEST_VERSION = '0.51.6';
  const REQUIRED_SCOPES = [
    'com.ivanmurzak',
    'extensions.unity',
    'org.nuget.com.ivanmurzak',
    'org.nuget.microsoft',
    'org.nuget.system',
    'org.nuget.r3',
  ];

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-test-'));
    fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  function writeManifest(manifest: object): void {
    fs.writeFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      JSON.stringify(manifest, null, 2)
    );
  }

  function readManifest(): Record<string, unknown> {
    const content = fs.readFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      'utf-8'
    );
    return JSON.parse(content);
  }

  it('removes plugin dependency when present', () => {
    writeManifest({
      dependencies: {
        'com.unity.ugui': '1.0.0',
        [PACKAGE_ID]: TEST_VERSION,
      },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: REQUIRED_SCOPES,
        },
      ],
    });
    removePluginFromManifest(tmpDir);
    const result = readManifest();
    const deps = result.dependencies as Record<string, string>;
    expect(deps[PACKAGE_ID]).toBeUndefined();
  });

  it('preserves other dependencies', () => {
    writeManifest({
      dependencies: {
        'com.unity.ugui': '1.0.0',
        'com.unity.test-framework': '1.1.33',
        [PACKAGE_ID]: TEST_VERSION,
      },
    });
    removePluginFromManifest(tmpDir);
    const result = readManifest();
    const deps = result.dependencies as Record<string, string>;
    expect(deps['com.unity.ugui']).toBe('1.0.0');
    expect(deps['com.unity.test-framework']).toBe('1.1.33');
    expect(deps[PACKAGE_ID]).toBeUndefined();
  });

  it('preserves scoped registries untouched', () => {
    writeManifest({
      dependencies: { [PACKAGE_ID]: TEST_VERSION },
      scopedRegistries: [
        {
          name: 'package.openupm.com',
          url: 'https://package.openupm.com',
          scopes: REQUIRED_SCOPES,
        },
        {
          name: 'other-registry',
          url: 'https://other.example.com',
          scopes: ['com.other'],
        },
      ],
    });
    removePluginFromManifest(tmpDir);
    const result = readManifest();
    const registries = result.scopedRegistries as Array<{ name: string; scopes: string[] }>;
    expect(registries.length).toBe(2);
    expect(registries[0].name).toBe('package.openupm.com');
    expect(registries[0].scopes).toEqual(REQUIRED_SCOPES);
    expect(registries[1].name).toBe('other-registry');
  });

  it('no-op when plugin is not in dependencies', () => {
    writeManifest({
      dependencies: { 'com.unity.ugui': '1.0.0' },
    });
    removePluginFromManifest(tmpDir);
    const result = readManifest();
    const deps = result.dependencies as Record<string, string>;
    expect(deps['com.unity.ugui']).toBe('1.0.0');
    expect(deps[PACKAGE_ID]).toBeUndefined();
  });

  it('no-op when dependencies field is missing', () => {
    writeManifest({ scopedRegistries: [] });
    removePluginFromManifest(tmpDir);
    const result = readManifest();
    expect(result.dependencies).toBeUndefined();
  });

  it('throws when manifest.json does not exist', () => {
    const nonexistentPath = path.join(tmpDir, 'nonexistent');
    expect(() => removePluginFromManifest(nonexistentPath)).toThrow(
      /manifest\.json not found/
    );
  });
});
