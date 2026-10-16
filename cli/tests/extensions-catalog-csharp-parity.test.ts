import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

// The SECOND half of the catalogue drift tripwire.
//
// `extensions-catalog-parity.test.ts` locks the JSON source of truth against the CLI's
// TypeScript mirror. This file locks the SAME JSON against the THIRD artifact: the C#
// array `MainWindowEditor._extensions`, which drives the editor window's Extensions
// section and feeds `ExtensionPanel.AddToManifest` — the in-editor installer. That array
// is load-bearing and independently maintained, which is exactly what makes this
// comparison meaningful rather than a tautology.
//
// It lives in the CLI's vitest suite on purpose: no Unity licence, no editor, runs on
// every PR via test_cli.yml AND again inside the publish job (deploy.yml), which is what
// makes it a release gate. Precedent for a TS test parsing the engine-side source to
// enforce parity: Godot-MCP's `cli/tests/skills-addon-parity.test.ts`.
//
// `version` is intentionally NOT compared: the C# `ExtensionData` struct has no version
// field, because the editor always resolves OpenUPM `dist-tags.latest` live
// (`ExtensionPanel.FetchLatestOpenUpmVersionAsync`). That is the same policy the JSON
// encodes as `version: null`, and the JSON↔TS test asserts the null on both its sides.
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PLUGIN_PACKAGE = path.resolve(
  __dirname,
  '..',
  '..',
  'Unity-MCP-Plugin',
  'Packages',
  'com.ivanmurzak.unity.mcp',
);
const CATALOG_JSON = path.join(PLUGIN_PACKAGE, 'extensions.catalog.json');
const CSHARP_SOURCE = path.join(
  PLUGIN_PACKAGE,
  'Editor',
  'Scripts',
  'UI',
  'Window',
  'MainWindowEditor.Extensions.cs',
);

interface CatalogEntry {
  name: string;
  description: string;
  packageId: string;
  gitUrl: string | null;
  tools: { name: string; description: string }[];
}

/**
 * Isolate the `_extensions = { ... }` initialiser, so nothing elsewhere in the file
 * (`SetupExtensionsSection`'s body, comments) can be mistaken for a catalogue entry.
 * Brace-scans rather than regexing the whole file, because the entries nest
 * `tools: new[] { ... }` blocks. String literals are skipped so a `{` or `}` inside a
 * description cannot unbalance the scan.
 */
function extractExtensionsBlock(source: string): string {
  const anchor = source.indexOf('_extensions');
  if (anchor === -1) throw new Error(`'_extensions' not found in ${CSHARP_SOURCE}`);

  const open = source.indexOf('{', anchor);
  if (open === -1) throw new Error(`no '{' after '_extensions' in ${CSHARP_SOURCE}`);

  let depth = 0;
  let inString = false;
  for (let i = open; i < source.length; i++) {
    const ch = source[i];

    if (inString) {
      if (ch === '\\') {
        i++; // skip the escaped character
      } else if (ch === '"') {
        inString = false;
      }
      continue;
    }

    if (ch === '"') {
      inString = true;
      continue;
    }
    if (ch === '{') depth++;
    else if (ch === '}') {
      depth--;
      if (depth === 0) return source.slice(open + 1, i);
    }
  }
  throw new Error(`unbalanced braces in the '_extensions' initialiser of ${CSHARP_SOURCE}`);
}

const ENTRY_RE =
  /new\(\s*name:\s*"((?:[^"\\]|\\.)*)"\s*,\s*description:\s*"((?:[^"\\]|\\.)*)"\s*,\s*packageId:\s*"((?:[^"\\]|\\.)*)"\s*,\s*gitUrl:\s*"((?:[^"\\]|\\.)*)"\s*,\s*tools:\s*new\[\]\s*\{([\s\S]*?)\}\s*\)/g;
const TOOL_RE = /\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)/g;

/** Unescape the C# string escapes actually reachable in this file's literals. */
function unescapeCSharp(s: string): string {
  return s.replace(/\\(["\\nrt])/g, (_m, c: string) => {
    if (c === 'n') return '\n';
    if (c === 'r') return '\r';
    if (c === 't') return '\t';
    return c;
  });
}

function parseCSharpCatalog(): { entries: CatalogEntry[]; newCount: number } {
  const block = extractExtensionsBlock(fs.readFileSync(CSHARP_SOURCE, 'utf-8'));

  // How many entries the file CLAIMS to have, counted independently of the entry regex.
  // Compared against the parse result below so a regex that silently matches a subset
  // fails loudly as a parser bug instead of masquerading as catalogue drift.
  const newCount = (block.match(/\bnew\(/g) ?? []).length;

  const entries: CatalogEntry[] = [];
  ENTRY_RE.lastIndex = 0;
  let m: RegExpExecArray | null;
  while ((m = ENTRY_RE.exec(block)) !== null) {
    const toolsBlock = m[5];
    const tools: { name: string; description: string }[] = [];
    TOOL_RE.lastIndex = 0;
    let t: RegExpExecArray | null;
    while ((t = TOOL_RE.exec(toolsBlock)) !== null) {
      tools.push({ name: unescapeCSharp(t[1]), description: unescapeCSharp(t[2]) });
    }
    entries.push({
      name: unescapeCSharp(m[1]),
      description: unescapeCSharp(m[2]),
      packageId: unescapeCSharp(m[3]),
      gitUrl: unescapeCSharp(m[4]),
      tools,
    });
  }
  return { entries, newCount };
}

function readJsonCatalog(): CatalogEntry[] {
  const raw = JSON.parse(fs.readFileSync(CATALOG_JSON, 'utf-8')) as {
    extensions?: {
      name?: string;
      description?: string;
      packageId?: string;
      gitUrl?: string | null;
      tools?: { name?: string; description?: string }[];
    }[];
  };
  return (raw.extensions ?? []).map((e) => ({
    name: (e.name ?? '').trim(),
    description: (e.description ?? '').trim(),
    packageId: (e.packageId ?? '').trim(),
    gitUrl: e.gitUrl === undefined || e.gitUrl === null || e.gitUrl.trim() === '' ? null : e.gitUrl.trim(),
    tools: (e.tools ?? []).map((t) => ({
      name: (t.name ?? '').trim(),
      description: (t.description ?? '').trim(),
    })),
  }));
}

describe('extensions.catalog.json parity with the C# MainWindowEditor._extensions array', () => {
  it('both artifacts are reachable from the test (relative layout sanity)', () => {
    expect(fs.existsSync(CATALOG_JSON), `missing ${CATALOG_JSON}`).toBe(true);
    expect(fs.existsSync(CSHARP_SOURCE), `missing ${CSHARP_SOURCE}`).toBe(true);
  });

  // Anti-vacuity guards. Without these, a parser that matched nothing would produce `[]`
  // and the deep-equal below would only report "drift" — hiding a broken test behind a
  // plausible-looking failure. Worse, if the JSON were ever empty too, `[] === []` would
  // pass with the whole tripwire dead. These assert POSITIVE artifacts.
  it('the C# parser extracts every entry the source declares', () => {
    const { entries, newCount } = parseCSharpCatalog();
    expect(newCount, 'no `new(` entries found in the _extensions block').toBeGreaterThanOrEqual(10);
    expect(
      entries.length,
      `the entry regex matched ${entries.length} of ${newCount} \`new(\` entries — the parser, ` +
        'not the catalogue, is out of date with the C# formatting',
    ).toBe(newCount);
  });

  it('the C# parser extracts a non-trivial tool set', () => {
    const { entries } = parseCSharpCatalog();
    const toolCount = entries.reduce((n, e) => n + e.tools.length, 0);
    expect(toolCount, 'parsed zero/too few tools — the tools regex is broken').toBeGreaterThanOrEqual(50);
    for (const e of entries) {
      expect(e.tools.length, `${e.packageId} parsed with no tools`).toBeGreaterThan(0);
    }
  });

  it('the C# array matches the JSON source of truth exactly', () => {
    const { entries } = parseCSharpCatalog();
    expect(
      entries,
      'Unity-MCP-Plugin/.../Editor/Scripts/UI/Window/MainWindowEditor.Extensions.cs drifted from ' +
        'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/extensions.catalog.json. ' +
        'Update BOTH the C# array and cli/src/utils/extensions-catalog.ts to match the JSON.',
    ).toEqual(readJsonCatalog());
  });
});
