import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { installExtension } from '../src/lib.js';
import { EXTENSIONS_CATALOG, findExtension } from '../src/utils/extensions-catalog.js';
import type { ExtensionDescriptor, ProgressEvent } from '../src/lib.js';

const TILEMAP_ID = 'com.ivanmurzak.unity.mcp.tilemap';
const REGISTRY_NAME = 'package.openupm.com';

function mkUnityProject(manifest: unknown = { dependencies: {} }): string {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-test-'));
  fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
  fs.writeFileSync(
    path.join(tmpDir, 'Packages', 'manifest.json'),
    typeof manifest === 'string' ? manifest : JSON.stringify(manifest, null, 2),
  );
  return tmpDir;
}

function readManifest(projectPath: string): {
  dependencies?: Record<string, string>;
  scopedRegistries?: { name: string; url: string; scopes: string[] }[];
} {
  return JSON.parse(fs.readFileSync(path.join(projectPath, 'Packages', 'manifest.json'), 'utf-8'));
}

/** Stub OpenUPM's packument endpoint so no test touches the network. */
function stubOpenUpm(latest: string): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn(async () => ({
    ok: true,
    status: 200,
    json: async () => ({ 'dist-tags': { latest } }),
  }));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const tmpDirs: string[] = [];
function track(dir: string): string {
  tmpDirs.push(dir);
  return dir;
}

beforeEach(() => {
  vi.unstubAllGlobals();
});

afterEach(() => {
  vi.unstubAllGlobals();
  while (tmpDirs.length) {
    const dir = tmpDirs.pop();
    if (dir) fs.rmSync(dir, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Catalogue lookup
// ---------------------------------------------------------------------------

describe('findExtension', () => {
  it('resolves by exact package id', () => {
    expect(findExtension(TILEMAP_ID)?.name).toBe('Tilemap');
  });

  it('resolves by package id case-insensitively', () => {
    expect(findExtension(TILEMAP_ID.toUpperCase())?.packageId).toBe(TILEMAP_ID);
  });

  it('resolves by display name case-insensitively', () => {
    expect(findExtension('tilemap')?.packageId).toBe(TILEMAP_ID);
    expect(findExtension('Cinemachine')?.packageId).toBe('com.ivanmurzak.unity.mcp.cinemachine');
  });

  it('returns null for an unknown id, an empty string, and nullish input', () => {
    expect(findExtension('com.example.nope')).toBeNull();
    expect(findExtension('')).toBeNull();
    expect(findExtension('   ')).toBeNull();
    expect(findExtension(undefined)).toBeNull();
    expect(findExtension(null)).toBeNull();
  });

  it('the shipped catalogue lists the ten extensions, all unpinned', () => {
    expect(EXTENSIONS_CATALOG.length).toBeGreaterThanOrEqual(10);
    for (const d of EXTENSIONS_CATALOG) {
      expect(d.version).toBeNull();
      expect(d.packageId.startsWith('com.ivanmurzak.unity.mcp.')).toBe(true);
    }
  });
});

// ---------------------------------------------------------------------------
// Failure paths — never throw past the public boundary
// ---------------------------------------------------------------------------

describe('installExtension — failures', () => {
  it('fails with an actionable message for an unknown extension id', async () => {
    const project = track(mkUnityProject());
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'com.example.nope',
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('unreachable');
    expect(result.error.message).toContain('Unknown extension "com.example.nope"');
    // The message must list what IS installable, so the user can self-correct.
    expect(result.error.message).toContain(TILEMAP_ID);
    expect(result.extensionId).toBe('com.example.nope');
  });

  it('fails when the path is not a Unity project, and names the missing manifest', async () => {
    const empty = track(fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-empty-')));
    const result = await installExtension({ unityProjectPath: empty, extensionId: 'Tilemap' });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('unreachable');
    expect(result.error.message).toContain('Packages/manifest.json');
    expect(result.packageId).toBe(TILEMAP_ID);
    expect(result.manifestPath).toContain('manifest.json');
  });

  it('resolves the catalogue BEFORE touching the filesystem', async () => {
    // An unknown id in a non-project directory must report the unknown extension, not
    // the missing manifest: the catalogue miss is the user's actual mistake.
    const empty = track(fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-ext-empty-')));
    const result = await installExtension({ unityProjectPath: empty, extensionId: 'nope' });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('unreachable');
    expect(result.error.message).toContain('Unknown extension');
  });

  it('returns a failure (not a throw) when the manifest is unparseable', async () => {
    const project = track(mkUnityProject('{ this is not json'));
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.0',
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('unreachable');
    expect(result.error).toBeInstanceOf(Error);
  });

  it('surfaces an OpenUPM failure as a failure result suggesting --extension-version', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ ok: false, status: 503, json: async () => ({}) })),
    );
    const project = track(mkUnityProject());
    const result = await installExtension({ unityProjectPath: project, extensionId: 'Tilemap' });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('unreachable');
    // The escape hatch named in the error must be the flag the CLI actually accepts.
    expect(result.error.message).toContain('--extension-version');
    // Nothing was written — a failed version resolution must not half-patch the manifest.
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBeUndefined();
  });
});

// ---------------------------------------------------------------------------
// The install itself
// ---------------------------------------------------------------------------

describe('installExtension — add', () => {
  it('adds the dependency and the OpenUPM scoped registry with the required scopes', async () => {
    const project = track(mkUnityProject());
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.outcome).toBe('added');
    expect(result.changed).toBe(true);
    expect(result.fromVersion).toBeNull();
    expect(result.toVersion).toBe('1.0.16');
    expect(result.packageId).toBe(TILEMAP_ID);
    expect(result.nextSteps.length).toBeGreaterThan(0);

    const manifest = readManifest(project);
    expect(manifest.dependencies?.[TILEMAP_ID]).toBe('1.0.16');

    const registry = manifest.scopedRegistries?.find((r) => r.name === REGISTRY_NAME);
    expect(registry).toBeDefined();
    expect(registry?.url).toBe('https://package.openupm.com');
    // `com.ivanmurzak` is the load-bearing scope: without it UPM resolves the extension
    // against Unity's default registry and the install silently fails to restore.
    expect(registry?.scopes).toContain('com.ivanmurzak');
    expect(registry?.scopes).toContain('extensions.unity');
  });

  it('resolves OpenUPM dist-tags.latest when no version is supplied', async () => {
    const fetchMock = stubOpenUpm('9.9.9');
    const project = track(mkUnityProject());
    const result = await installExtension({ unityProjectPath: project, extensionId: 'Tilemap' });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.toVersion).toBe('9.9.9');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('9.9.9');

    // It must ask OpenUPM about the EXTENSION, not about the plugin.
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(String(fetchMock.mock.calls[0][0])).toBe(`https://package.openupm.com/${TILEMAP_ID}`);
  });

  it('does not hit the network when an explicit version is supplied', async () => {
    const fetchMock = stubOpenUpm('9.9.9');
    const project = track(mkUnityProject());
    await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.0',
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('preserves unrelated dependencies and an existing scoped registry entry', async () => {
    const project = track(
      mkUnityProject({
        dependencies: { 'com.unity.modules.tilemap': '1.0.0' },
        scopedRegistries: [
          { name: REGISTRY_NAME, url: 'https://package.openupm.com', scopes: ['com.someoneelse'] },
        ],
      }),
    );
    await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });

    const manifest = readManifest(project);
    expect(manifest.dependencies?.['com.unity.modules.tilemap']).toBe('1.0.0');
    expect(manifest.scopedRegistries).toHaveLength(1);
    const scopes = manifest.scopedRegistries?.[0].scopes ?? [];
    expect(scopes).toContain('com.someoneelse');
    expect(scopes).toContain('com.ivanmurzak');
  });
});

describe('installExtension — update / idempotence', () => {
  it('is idempotent: a second identical run reports already-up-to-date and rewrites nothing', async () => {
    const project = track(mkUnityProject());
    await installExtension({ unityProjectPath: project, extensionId: 'Tilemap', version: '1.0.16' });

    const manifestPath = path.join(project, 'Packages', 'manifest.json');
    const before = fs.readFileSync(manifestPath, 'utf-8');

    const second = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });

    expect(second.kind).toBe('success');
    if (second.kind !== 'success') throw new Error('unreachable');
    expect(second.outcome).toBe('already-up-to-date');
    expect(second.changed).toBe(false);
    expect(second.fromVersion).toBe('1.0.16');
    expect(second.toVersion).toBe('1.0.16');
    expect(fs.readFileSync(manifestPath, 'utf-8')).toBe(before);
  });

  it('reports `updated` with both versions when a newer version is installed', async () => {
    const project = track(mkUnityProject());
    await installExtension({ unityProjectPath: project, extensionId: 'Tilemap', version: '1.0.10' });
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.outcome).toBe('updated');
    expect(result.changed).toBe(true);
    expect(result.fromVersion).toBe('1.0.10');
    expect(result.toVersion).toBe('1.0.16');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('never downgrades on an auto-resolved version, and warns instead', async () => {
    const project = track(mkUnityProject());
    await installExtension({ unityProjectPath: project, extensionId: 'Tilemap', version: '2.0.0' });

    stubOpenUpm('1.0.16');
    const result = await installExtension({ unityProjectPath: project, extensionId: 'Tilemap' });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.outcome).toBe('already-up-to-date');
    expect(result.toVersion).toBe('2.0.0');
    expect(result.warnings.join(' ')).toContain('2.0.0');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('2.0.0');
  });

  it('allows a downgrade when the version is explicit', async () => {
    const project = track(mkUnityProject());
    await installExtension({ unityProjectPath: project, extensionId: 'Tilemap', version: '2.0.0' });
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.outcome).toBe('updated');
    expect(result.fromVersion).toBe('2.0.0');
    expect(result.toVersion).toBe('1.0.16');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('classifies as `added` even when the scoped registry also had to be created', async () => {
    // `addPackageToManifest` reports `modified: true` for a registry-only change too, so
    // the outcome must be derived from the dependency's prior presence, not from `modified`.
    const project = track(mkUnityProject({ dependencies: {} }));
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
    });
    if (result.kind !== 'success') throw new Error('unreachable');
    expect(result.outcome).toBe('added');
    expect(result.fromVersion).toBeNull();
  });
});

describe('installExtension — progress + catalogue override', () => {
  it('emits start / dependencies-resolved / manifest-patched / done in order', async () => {
    stubOpenUpm('1.2.3');
    const project = track(mkUnityProject());
    const events: ProgressEvent[] = [];
    await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      onProgress: (e) => events.push(e),
    });

    expect(events.map((e) => e.phase)).toEqual([
      'start',
      'dependencies-resolved',
      'manifest-patched',
      'done',
    ]);
  });

  it('a throwing onProgress callback cannot abort the install', async () => {
    const project = track(mkUnityProject());
    const result = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.16',
      onProgress: () => {
        throw new Error('consumer bug');
      },
    });

    expect(result.kind).toBe('success');
    expect(readManifest(project).dependencies?.[TILEMAP_ID]).toBe('1.0.16');
  });

  it('honours a catalogue override, including an empty catalogue', async () => {
    const project = track(mkUnityProject());
    const custom: ExtensionDescriptor[] = [
      {
        name: 'Custom',
        description: 'A test-only extension.',
        packageId: 'com.example.custom',
        version: null,
        gitUrl: null,
        tools: [],
      },
    ];

    const ok = await installExtension({
      unityProjectPath: project,
      extensionId: 'Custom',
      version: '0.1.0',
      catalog: custom,
    });
    expect(ok.kind).toBe('success');
    expect(readManifest(project).dependencies?.['com.example.custom']).toBe('0.1.0');

    // A shipped id must NOT resolve against an overridden catalogue.
    const miss = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      version: '1.0.0',
      catalog: custom,
    });
    expect(miss.kind).toBe('failure');

    const empty = await installExtension({
      unityProjectPath: project,
      extensionId: 'Tilemap',
      catalog: [],
    });
    expect(empty.kind).toBe('failure');
    if (empty.kind !== 'failure') throw new Error('unreachable');
    expect(empty.error.message).toContain('catalogue is currently empty');
  });
});
