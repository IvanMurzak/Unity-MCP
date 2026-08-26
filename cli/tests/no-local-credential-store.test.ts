// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

/**
 * STRUCTURAL GATE (task d2 DoD): "No local credential-store code remains in the CLI beyond the
 * explicit `--project` override path."
 *
 * The credential store's serializer lives ONCE, in `@baizor/gamedev-cli-core` (store v2, DPAPI /
 * 0600, atomic writes, cross-process lock). This gate fails the build if store-serializer code —
 * hand-built store paths, DPAPI plumbing, or the raw `read()?.accessToken` token grab that d2
 * deleted — reappears anywhere in `src/` outside the two allowlisted files:
 *
 *  - `commands/login.ts` — the explicit `--project` override (it composes the project-local store
 *    path and its user-facing help mentions the store location);
 *  - `utils/machine-credentials.ts` — the thin cli-core re-export (the CLI's stable import path).
 *
 * Comments are stripped before matching so documentation may still NAME the store file; string
 * literals and code cannot.
 */

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SRC_DIR = path.resolve(__dirname, '..', 'src');

const ALLOWLIST = new Set<string>([
  path.join('commands', 'login.ts'),
  path.join('utils', 'machine-credentials.ts'),
]);

/** Store-serializer smells. Applied to comment-stripped source of every non-allowlisted file. */
const FORBIDDEN_PATTERNS: Array<{ name: string; pattern: RegExp }> = [
  { name: 'store file name literal (credentials.json)', pattern: /credentials\.json/i },
  { name: 'store directory literal (.ai-game-dev)', pattern: /\.ai-game-dev/ },
  {
    name: 'DPAPI plumbing (belongs to cli-core only)',
    pattern: /CryptProtectData|CryptUnprotectData|ProtectedData|DPAPI/i,
  },
  {
    name: 'raw store token read (read()?.accessToken — the pre-d2 defect)',
    pattern: /\.read\(\)\s*[?!]?\.\s*accessToken/,
  },
];

function listSourceFiles(dir: string): string[] {
  const out: string[] = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...listSourceFiles(full));
    else if (entry.isFile() && entry.name.endsWith('.ts')) out.push(full);
  }
  return out;
}

/** Strip line comments and block comments (string-aware enough for this gate). */
function stripComments(source: string): string {
  // Remove block comments first (non-greedy, across lines), then line comments. The `[^:]` guard
  // keeps `https://…` URLs inside string literals intact.
  return source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/(^|[^:])\/\/.*$/gm, '$1');
}

describe('structural gate — no local credential-store code outside the --project override', () => {
  it('the allowlisted files still exist (a rename must revisit this gate, not silently void it)', () => {
    for (const rel of ALLOWLIST) {
      expect(fs.existsSync(path.join(SRC_DIR, rel)), `${rel} missing`).toBe(true);
    }
  });

  it('no src file outside the allowlist contains store-serializer code', () => {
    const violations: string[] = [];

    for (const file of listSourceFiles(SRC_DIR)) {
      const rel = path.relative(SRC_DIR, file);
      if (ALLOWLIST.has(rel)) continue;

      const code = stripComments(fs.readFileSync(file, 'utf-8'));
      for (const { name, pattern } of FORBIDDEN_PATTERNS) {
        if (pattern.test(code)) {
          violations.push(`${rel}: ${name}`);
        }
      }
    }

    expect(violations, `local credential-store code detected:\n  ${violations.join('\n  ')}`).toEqual(
      [],
    );
  });

  it('the gate patterns can actually match (self-test: the pre-d2 defect shape trips the raw-read pattern)', () => {
    // Guard the gate against pattern rot: the exact shape d2 deleted from config.ts must match.
    const byName = (fragment: string): RegExp => {
      const entry = FORBIDDEN_PATTERNS.find((p) => p.name.includes(fragment));
      if (!entry) throw new Error(`gate pattern missing: ${fragment}`);
      return entry.pattern;
    };
    const preD2Defect = 'return new MachineCredentialStore().read()?.accessToken ?? undefined;';
    expect(byName('raw store token read').test(preD2Defect)).toBe(true);
    expect(byName('store file name').test("path.join(base, 'credentials.json')")).toBe(true);
    expect(byName('store directory').test("path.join(home, '.ai-game-dev')")).toBe(true);
  });
});
