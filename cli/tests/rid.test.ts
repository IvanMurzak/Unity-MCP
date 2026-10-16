// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import { resolveHostRid, KNOWN_RIDS } from '../src/utils/rid.js';

describe('resolveHostRid', () => {
  const cases: Array<[NodeJS.Platform, string, string]> = [
    ['win32', 'x64', 'win-x64'],
    ['win32', 'arm64', 'win-arm64'],
    ['win32', 'ia32', 'win-x86'],
    ['darwin', 'x64', 'osx-x64'],
    ['darwin', 'arm64', 'osx-arm64'],
    ['linux', 'x64', 'linux-x64'],
    ['linux', 'arm64', 'linux-arm64'],
  ];

  for (const [platform, arch, expected] of cases) {
    it(`maps ${platform}/${arch} -> ${expected}`, () => {
      expect(resolveHostRid(platform, arch)).toBe(expected);
      expect(KNOWN_RIDS).toContain(expected as (typeof KNOWN_RIDS)[number]);
    });
  }

  it('throws for an unknown platform', () => {
    expect(() => resolveHostRid('sunos' as NodeJS.Platform, 'x64')).toThrow(/Unsupported host/);
  });

  it('throws for an unknown architecture', () => {
    expect(() => resolveHostRid('linux', 'mips')).toThrow(/Unsupported host/);
  });

  it('throws for a platform/arch combo with no published build (osx-x86)', () => {
    // ia32 -> x86 is a valid token, but osx-x86 is not a published RID.
    expect(() => resolveHostRid('darwin', 'ia32')).toThrow(/No GameDev-MCP-Server build/);
  });

  it('defaults to the current process platform/arch', () => {
    expect(KNOWN_RIDS).toContain(resolveHostRid());
  });
});
