// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import {
  parseDismissOutput,
  LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS,
  WINDOWS_DISMISS_PS_SCRIPT,
  MACOS_DISMISS_APPLESCRIPT,
  DISMISS_BUTTON_LABEL,
  LINUX_XDOTOOL_MISSING_PREFIX,
  UNSUPPORTED_PLATFORM_PREFIX,
  tryDismissLaunchErrorsDialog,
  _resetXdotoolPresenceForTests,
  regexEscapeForXdotool,
} from '../src/utils/launch-error-dismiss.js';

describe('parseDismissOutput', () => {
  it('returns not-found for empty output', () => {
    expect(parseDismissOutput('')).toEqual({ kind: 'not-found' });
  });

  it('returns not-found for the literal "not-found" token', () => {
    expect(parseDismissOutput('not-found\n')).toEqual({ kind: 'not-found' });
  });

  it('parses a dismissed:<button> token', () => {
    expect(parseDismissOutput('dismissed:Ignore')).toEqual({
      kind: 'dismissed',
      button: 'Ignore',
    });
  });

  it('parses a dismissed token with trailing whitespace', () => {
    expect(parseDismissOutput('dismissed:Ignore\n')).toEqual({
      kind: 'dismissed',
      button: 'Ignore',
    });
  });

  it('falls back to the default button label on dismissed: with empty suffix', () => {
    expect(parseDismissOutput('dismissed:')).toEqual({
      kind: 'dismissed',
      button: DISMISS_BUTTON_LABEL,
    });
  });

  it('parses an error:<message> token', () => {
    expect(parseDismissOutput('error:Accessibility permission missing')).toEqual({
      kind: 'error',
      message: 'Accessibility permission missing',
    });
  });

  it('treats unrecognised output as not-found (defensive)', () => {
    expect(parseDismissOutput('garbage banana 42')).toEqual({ kind: 'not-found' });
  });

  it('inspects the LAST non-empty line — earlier chatter does not misclassify', () => {
    // Real-world shape: a stray PowerShell warning, osascript
    // deprecation notice, or xdotool stderr leaks ahead of the
    // contract token. The parser must still classify on the final
    // line.
    expect(parseDismissOutput('WARNING: deprecated\ndismissed:Ignore\n')).toEqual({
      kind: 'dismissed',
      button: 'Ignore',
    });
    expect(parseDismissOutput('some chatter\nnot-found\n')).toEqual({ kind: 'not-found' });
    expect(parseDismissOutput('chatter\nerror:permission denied\n')).toEqual({
      kind: 'error',
      message: 'permission denied',
    });
  });

  it('handles CRLF line endings (Windows PowerShell stdout)', () => {
    expect(parseDismissOutput('WARN\r\ndismissed:Ignore\r\n')).toEqual({
      kind: 'dismissed',
      button: 'Ignore',
    });
  });
});

describe('regexEscapeForXdotool', () => {
  it('escapes regex metacharacters so a fragment is matched literally', () => {
    expect(regexEscapeForXdotool('Hold On')).toBe('Hold On');
    expect(regexEscapeForXdotool('(Hold On)')).toBe('\\(Hold On\\)');
    expect(regexEscapeForXdotool('Compiler Errors v2.0+')).toBe('Compiler Errors v2\\.0\\+');
    expect(regexEscapeForXdotool('a*b?c[d]')).toBe('a\\*b\\?c\\[d\\]');
  });

  it('every current LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS is regex-safe (no escape needed)', () => {
    // Locks down the contract that current fragments are literal —
    // a future contributor adding a fragment with metacharacters
    // gets an immediate failure here pointing at the regex hazard.
    for (const frag of LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS) {
      expect(regexEscapeForXdotool(frag)).toBe(frag);
    }
  });
});

describe('LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS', () => {
  it('includes both legacy and current Unity dialog titles', () => {
    // The issue calls out resilience across Unity versions — explicit
    // assertions here so a future drive-by edit cannot accidentally
    // remove a fragment.
    const fragments = LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS.map((f) => f.toLowerCase());
    expect(fragments).toContain('compiler errors');
    expect(fragments).toContain('hold on');
    expect(fragments).toContain('compile errors');
  });

  it('is non-empty (a zero-fragment matcher would never match anything)', () => {
    expect(LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS.length).toBeGreaterThan(0);
  });
});

describe('WINDOWS_DISMISS_PS_SCRIPT', () => {
  it('mentions the Ignore button label so the matcher will click it', () => {
    expect(WINDOWS_DISMISS_PS_SCRIPT).toContain(DISMISS_BUTTON_LABEL);
  });

  it('uses the Win32 BM_CLICK message constant (0x00F5) — not a synthetic mouse event', () => {
    // The issue prefers BM_CLICK over a synthetic mouse event so the
    // dismiss works even if the user is mid-click in another app.
    expect(WINDOWS_DISMISS_PS_SCRIPT).toContain('0x00F5');
  });

  it('imports the user32 functions the strategy depends on', () => {
    for (const fn of [
      'EnumWindows',
      'EnumChildWindows',
      'GetWindowTextW',
      'GetClassNameW',
      'GetWindowThreadProcessId',
      'SendMessageW',
    ]) {
      expect(WINDOWS_DISMISS_PS_SCRIPT).toContain(fn);
    }
  });

  it('includes every documented title fragment', () => {
    for (const frag of LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS) {
      expect(WINDOWS_DISMISS_PS_SCRIPT).toContain(frag);
    }
  });
});

describe('MACOS_DISMISS_APPLESCRIPT', () => {
  it('clicks the Ignore button on a Unity process window', () => {
    expect(MACOS_DISMISS_APPLESCRIPT).toContain('process "Unity"');
    expect(MACOS_DISMISS_APPLESCRIPT).toContain('button "Ignore"');
    expect(MACOS_DISMISS_APPLESCRIPT).toContain('click button');
  });

  it('returns "not-found" when no Unity process / button is present', () => {
    expect(MACOS_DISMISS_APPLESCRIPT).toContain('return "not-found"');
  });

  it('catches AppleScript errors so the polling loop does not abort', () => {
    expect(MACOS_DISMISS_APPLESCRIPT).toContain('on error');
  });
});

describe('tryDismissLaunchErrorsDialog — Linux/X11 missing-tool guard', () => {
  it('returns an error outcome when xdotool is not on PATH and platform is linux', async () => {
    _resetXdotoolPresenceForTests();
    // We can't reliably guarantee xdotool is missing on every machine
    // running these tests, so we simulate the condition by spoofing
    // PATH to a non-existent directory for the duration of the call.
    const originalPath = process.env.PATH;
    process.env.PATH = '/nonexistent/path/that/will/not/find/xdotool';
    try {
      const result = await tryDismissLaunchErrorsDialog('linux');
      // Acceptable outcomes: error (xdotool truly missing) OR
      // not-found (xdotool happens to be installed system-wide and
      // simply found no matching window). Both are valid; we only
      // assert that the helper returns a structured outcome rather
      // than throwing.
      expect(['error', 'not-found']).toContain(result.kind);
      if (result.kind === 'error') {
        expect(result.message.toLowerCase()).toContain('xdotool');
      }
    } finally {
      process.env.PATH = originalPath;
      _resetXdotoolPresenceForTests();
    }
  });
});

describe('tryDismissLaunchErrorsDialog — unknown platform', () => {
  it('returns an error outcome for an unsupported platform string', async () => {
    // Force-cast to bypass the type system — we are explicitly
    // testing the runtime guard.
    const result = await tryDismissLaunchErrorsDialog(
      'plan9' as unknown as 'win32' | 'darwin' | 'linux',
    );
    expect(result.kind).toBe('error');
    if (result.kind !== 'error') return;
    expect(result.message).toContain(UNSUPPORTED_PLATFORM_PREFIX);
  });
});

describe('permanent-error message constants', () => {
  // The bailout matcher in `lib/open.ts` (PERMANENT_DISMISS_ERROR_MARKERS)
  // uses `String.includes` to detect permanent errors. To keep the
  // matcher and the producer-side message in sync, the producers
  // MUST use these exported constants verbatim — a re-word at the
  // producer site that drifts from the constant would silently
  // de-sync the bailout. These tests fail loudly if a future drive-by
  // edit re-words the producer message but forgets to update the
  // constant.

  it('LINUX_XDOTOOL_MISSING_PREFIX is the prefix the linux producer emits', async () => {
    _resetXdotoolPresenceForTests();
    const originalPath = process.env.PATH;
    process.env.PATH = '/nonexistent/path/that/will/not/find/xdotool';
    try {
      const result = await tryDismissLaunchErrorsDialog('linux');
      // If xdotool happens to exist system-wide, the test cannot
      // observe the producer message — skip the substring check
      // in that case (the unsupported-platform test below already
      // covers the constants-are-imported angle).
      if (result.kind === 'error') {
        expect(result.message).toContain(LINUX_XDOTOOL_MISSING_PREFIX);
      }
    } finally {
      process.env.PATH = originalPath;
      _resetXdotoolPresenceForTests();
    }
  });

  it('UNSUPPORTED_PLATFORM_PREFIX is the prefix the dispatcher emits', async () => {
    const result = await tryDismissLaunchErrorsDialog(
      'plan9' as unknown as 'win32' | 'darwin' | 'linux',
    );
    expect(result.kind).toBe('error');
    if (result.kind !== 'error') return;
    expect(result.message.startsWith(UNSUPPORTED_PLATFORM_PREFIX)).toBe(true);
  });
});
