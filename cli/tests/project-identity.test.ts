// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import {
  generatePortFromDirectory,
  deriveProjectPin,
  normalizeProjectRoot,
} from '../src/utils/port.js';

// ---------------------------------------------------------------------------
// Golden-vector parity: C# ProjectIdentity <-> this TS port.
//
// Inlined verbatim from MCP-Plugin-dotnet
//   McpPlugin/src/AgentConfig/ProjectIdentity.GoldenVectors.json
// (intentionally copied inline — the test must NOT add a runtime dependency on that file).
// The C# reference implementation is the canonical origin; this TS port MUST reproduce every
// pin/port byte-for-byte, including the ToLowerInvariant-vs-toLowerCase() Unicode divergence.
// ---------------------------------------------------------------------------

interface GoldenVector {
  path: string;
  pin: string;
  port: number;
}

const GOLDEN_VECTORS: GoldenVector[] = [
  { path: '/home/user/my-game', pin: '34ea75f2', port: 23940 },
  { path: '/home/user/my-game/', pin: '34ea75f2', port: 23940 }, // trailing slash trimmed
  { path: '/home/USER/My-Game', pin: '34ea75f2', port: 23940 }, // case-folded
  { path: 'C:\\Users\\user\\my-game', pin: '8ef72cf7', port: 29310 }, // Windows backslash form
  { path: 'C:\\Users\\user\\my-game\\', pin: '8ef72cf7', port: 29310 }, // trailing backslash trimmed
  { path: 'C:/Users/user/my-game', pin: '5a87324e', port: 24298 }, // forward-slash form differs
  { path: '/home/İstanbul/game', pin: '672d80a7', port: 25303 }, // U+0130, ToLowerInvariant canonical
  { path: '/srv/games/space sim', pin: '08c6cbb6', port: 27816 }, // path with a space
];

describe('ProjectIdentity golden-vector parity (C# reference <-> TS port)', () => {
  for (const v of GOLDEN_VECTORS) {
    it(`derives the canonical pin + port for ${JSON.stringify(v.path)}`, () => {
      expect(deriveProjectPin(v.path)).toBe(v.pin);
      expect(generatePortFromDirectory(v.path)).toBe(v.port);
    });
  }

  it('uses ToLowerInvariant for U+0130 (NOT the naive JS toLowerCase value)', () => {
    // A naive toLowerCase() port would derive pin 77300275 / port 27751 (U+0130 -> "i" + combining
    // dot). The canonical C# ToLowerInvariant value is 672d80a7 / 25303 (U+0130 left unchanged).
    expect(deriveProjectPin('/home/İstanbul/game')).toBe('672d80a7');
    expect(deriveProjectPin('/home/İstanbul/game')).not.toBe('77300275');
    expect(generatePortFromDirectory('/home/İstanbul/game')).not.toBe(27751);
  });

  it('does NOT convert separators — backslash and forward-slash forms differ', () => {
    expect(generatePortFromDirectory('C:\\Users\\user\\my-game')).not.toBe(
      generatePortFromDirectory('C:/Users/user/my-game'),
    );
    expect(deriveProjectPin('C:\\Users\\user\\my-game')).not.toBe(
      deriveProjectPin('C:/Users/user/my-game'),
    );
  });

  it('normalizes trailing separators and case before hashing', () => {
    expect(normalizeProjectRoot('/home/user/my-game/')).toBe('/home/user/my-game');
    expect(normalizeProjectRoot('/home/user/my-game\\')).toBe('/home/user/my-game');
    expect(normalizeProjectRoot('/home/USER/My-Game')).toBe('/home/user/my-game');
  });

  it('never trims a lone separator below length 1', () => {
    expect(normalizeProjectRoot('/')).toBe('/');
  });
});
