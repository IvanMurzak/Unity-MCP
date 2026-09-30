import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { EXTENSIONS_CATALOG, type ExtensionDescriptor } from '../src/utils/extensions-catalog.js';

// The CLI's EXTENSIONS_CATALOG mirror MUST stay equivalent to the SHARED source of truth
// `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json` (shipped in
// the UPM package). If the catalogue gains/changes/loses an entry, this test fails until
// the mirror is updated — the drift tripwire that keeps the editor window, the CLI, and the
// app from diverging. Its sibling `extensions-catalog-csharp-parity.test.ts` enforces the
// same JSON against the C# array.
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CATALOG_JSON = path.resolve(
  __dirname,
  '..',
  '..',
  'Unity-MCP-Plugin',
  'Packages',
  'com.ivanmurzak.unity.mcp',
  'extensions.catalog.json',
);

interface CatalogJsonEntry {
  name?: string;
  description?: string;
  packageId?: string;
  version?: string | null;
  gitUrl?: string | null;
  tools?: { name?: string; description?: string }[];
}

/** Trim to a non-empty string, else null (so `""` and a missing field normalise alike). */
function trimOrNull(v: string | null | undefined): string | null {
  if (v === undefined || v === null) return null;
  const s = String(v).trim();
  return s === '' ? null : s;
}

/** Normalise a raw JSON entry to the canonical ExtensionDescriptor shape. */
function normalizeJsonEntry(e: CatalogJsonEntry): ExtensionDescriptor {
  return {
    name: (e.name ?? '').trim(),
    description: (e.description ?? '').trim(),
    packageId: (e.packageId ?? '').trim(),
    version: trimOrNull(e.version),
    gitUrl: trimOrNull(e.gitUrl),
    tools: (e.tools ?? [])
      .filter((t) => t.name && t.name.trim() !== '')
      .map((t) => ({ name: (t.name ?? '').trim(), description: (t.description ?? '').trim() })),
  };
}

function readCatalogJson(): CatalogJsonEntry[] {
  const raw = JSON.parse(fs.readFileSync(CATALOG_JSON, 'utf-8')) as {
    extensions?: CatalogJsonEntry[];
  };
  return (raw.extensions ?? []).filter(
    (e) => e.name && e.name.trim() !== '' && e.packageId && e.packageId.trim() !== '',
  );
}

describe('extensions-catalog parity with Unity-MCP-Plugin/.../extensions.catalog.json', () => {
  it('the catalogue JSON is reachable from the test (relative layout sanity)', () => {
    expect(fs.existsSync(CATALOG_JSON)).toBe(true);
  });

  // Anti-vacuity floor. Every assertion below compares the mirror against the JSON, so a
  // JSON that failed to load as an empty array would make those comparisons pass against
  // an empty mirror. These two assert a POSITIVE artifact instead: real content was read.
  it('the JSON carries the ten shipped extensions and a non-trivial tool set', () => {
    const entries = readCatalogJson();
    expect(entries.length).toBeGreaterThanOrEqual(10);
    const toolCount = entries.reduce((n, e) => n + (e.tools?.length ?? 0), 0);
    expect(toolCount).toBeGreaterThanOrEqual(50);
  });

  it('the TS mirror EXTENSIONS_CATALOG matches the JSON source of truth exactly', () => {
    const expected = readCatalogJson().map(normalizeJsonEntry);

    // Compare as plain objects (EXTENSIONS_CATALOG is readonly; spread to mutable for deep-equal).
    const actual = EXTENSIONS_CATALOG.map((d) => ({
      name: d.name,
      description: d.description,
      packageId: d.packageId,
      version: d.version,
      gitUrl: d.gitUrl,
      tools: d.tools.map((t) => ({ name: t.name, description: t.description })),
    }));

    expect(
      actual,
      'cli/src/utils/extensions-catalog.ts drifted from ' +
        'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json. ' +
        'Update EXTENSIONS_CATALOG to match the JSON source of truth.',
    ).toEqual(expected);
  });

  it('no catalogue entry pins a version (OpenUPM latest is resolved at install time)', () => {
    // A pin here goes stale on the very next extension release — see extensions.catalog.md.
    // Asserted on BOTH artifacts: a pin added to only one of them is a drift the deep-equal
    // above catches, and a pin added to both would slip past it.
    for (const e of readCatalogJson()) {
      expect(trimOrNull(e.version), `${e.packageId} pins a version in the JSON`).toBeNull();
    }
    for (const d of EXTENSIONS_CATALOG) {
      expect(d.version, `${d.packageId} pins a version in the TS mirror`).toBeNull();
    }
  });

  it('every packageId is unique and lowercase (the UPM dependency key is the install identity)', () => {
    const ids = EXTENSIONS_CATALOG.map((d) => d.packageId);
    expect(new Set(ids).size).toBe(ids.length);
    for (const id of ids) {
      expect(id, `${id} must be lowercase — UPM package names are case-sensitive`).toBe(
        id.toLowerCase(),
      );
    }
  });
});
