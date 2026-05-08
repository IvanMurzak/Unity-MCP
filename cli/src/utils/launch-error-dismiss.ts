import { execFile, execFileSync } from 'child_process';
import { platform as nodePlatform } from 'os';
import { verbose } from './ui.js';

/**
 * Supported `process.platform` values for the launch-errors dialog
 * dismiss helper. Narrowed alias of NodeJS.Platform — keeps the
 * platform-dispatch table exhaustive in tests without forcing callers
 * to import a Node-internal type.
 */
export type DismissPlatform = 'win32' | 'darwin' | 'linux';

/**
 * Outcome of a single dismiss attempt against the running OS desktop.
 *
 * `dismissed`: the dialog was found AND a click was dispatched
 * successfully. The polling loop should stop and return.
 *
 * `not-found`: no matching dialog was visible on this poll tick. The
 * polling loop should continue ticking until either the overall
 * timeout elapses or Unity reports ready (the existing wait-for-ready
 * logic, which runs in parallel, is the authoritative ready signal).
 *
 * `error`: an unexpected platform error happened (a required tool was
 * missing, a syscall failed). The polling loop logs the error once
 * and continues with `not-found` semantics — the dialog may simply
 * not be open yet, and a single transient error must not abort the
 * whole launch flow.
 */
export type DismissOutcome =
  | { kind: 'dismissed'; button: string }
  | { kind: 'not-found' }
  | { kind: 'error'; message: string };

/**
 * Window-title fragments matched against the Unity launch-errors
 * dialog. Both legacy and current strings are listed so the matcher
 * stays resilient across Unity versions. The match is case-insensitive
 * and substring-based — Unity's actual title varies by version but
 * always contains one of these.
 *
 * Exposed so tests can assert the matcher knows about both spellings
 * without grepping the implementation.
 */
export const LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS: readonly string[] = [
  'Compiler Errors', // Unity 2020+ ("Hold On" + "Compiler Errors on Launch")
  'Hold On', // Generic Unity progress dialog wrapping the launch-errors variant
  'Compile Errors', // Older Unity dialog spelling
  'Scripts have compiler errors', // Unity 2022+ on Linux (window manager surfacing)
] as const;

/** The button label this helper presses to dismiss the dialog. */
export const DISMISS_BUTTON_LABEL = 'Ignore';

/**
 * Try once to find and dismiss the Unity launch-errors dialog on the
 * current OS desktop. Pure-ish — performs a single OS call and
 * returns; never blocks past the underlying syscall's own timeout.
 *
 * Library-safe: never throws (errors are returned in the
 * `DismissOutcome` union), never writes to stdout/stderr, never
 * mutates global state.
 *
 * The helper is platform-dispatched:
 * - **Windows**: Win32 (`FindWindowW` / `EnumWindows` /
 *   `EnumChildWindows` / `GetWindowTextW` / `SendMessageW(BM_CLICK)`)
 *   driven from PowerShell so we do not pull in a native node-gyp
 *   dependency. UI Automation is the documented fallback if title
 *   matching breaks on a future Unity release.
 * - **macOS**: AppleScript via `osascript` (the leaner
 *   AX-C-API-direct path is a documented follow-up). Requires the
 *   user to have granted Accessibility permission to the terminal /
 *   `unity-mcp-cli` binary once.
 * - **Linux/X11**: `xdotool` (documented as a Linux platform
 *   dependency; `wmctrl` is acceptable as an alternative window
 *   enumerator). Wayland is deferred — call out explicitly in the
 *   error message so the user does not waste time debugging.
 */
export async function tryDismissLaunchErrorsDialog(
  platform: DismissPlatform = nodePlatform() as DismissPlatform,
): Promise<DismissOutcome> {
  switch (platform) {
    case 'win32':
      return tryDismissWindows();
    case 'darwin':
      return tryDismissMacOS();
    case 'linux':
      return tryDismissLinuxX11();
    default:
      return {
        kind: 'error',
        message: `Unsupported platform for launch-errors auto-dismiss: ${platform as string}`,
      };
  }
}

// ---------------------------------------------------------------------------
// Windows — Win32 via PowerShell
// ---------------------------------------------------------------------------

/**
 * The PowerShell payload that probes for the Unity launch-errors
 * dialog and clicks `Ignore` if found. Exported as a string (not a
 * function) so tests can assert the script shape without launching
 * PowerShell.
 *
 * Strategy:
 *   1. P/Invoke `EnumWindows` via Add-Type to enumerate every visible
 *      top-level window owned by `Unity.exe`.
 *   2. Filter by title fragment (case-insensitive substring).
 *   3. Walk child windows with `EnumChildWindows`, looking for a
 *      Button whose text equals `Ignore`.
 *   4. Send `BM_CLICK` (0x00F5) to the matched button. `BM_CLICK` is
 *      preferred over a synthesised mouse event — it works even if
 *      the user is mid-click in another app, and it does not steal
 *      focus.
 *
 * The script writes a single-token result to stdout:
 *   - `dismissed:<button>` on success
 *   - `not-found` when no dialog was matched
 *   - `error:<message>` on an unexpected exception
 */
export const WINDOWS_DISMISS_PS_SCRIPT = `
$ErrorActionPreference = 'Stop'
try {
  if (-not ([System.Management.Automation.PSTypeName]'UnityMcp.LaunchErrors.Dismisser').Type) {
    Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace UnityMcp.LaunchErrors {
  public static class Dismisser {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    const uint BM_CLICK = 0x00F5;
    static readonly string[] TitleFragments = new[] { "Compiler Errors", "Hold On", "Compile Errors", "Scripts have compiler errors" };
    static readonly string[] ButtonLabels = new[] { "${DISMISS_BUTTON_LABEL}", "&${DISMISS_BUTTON_LABEL}" };
    public static string TryDismiss(int[] unityPids) {
      var unityPidSet = new HashSet<uint>();
      for (int i = 0; i < unityPids.Length; i++) unityPidSet.Add((uint)unityPids[i]);
      IntPtr matchedDialog = IntPtr.Zero;
      var sb = new StringBuilder(512);
      EnumWindows((hWnd, lParam) => {
        if (!IsWindowVisible(hWnd)) return true;
        uint procId; GetWindowThreadProcessId(hWnd, out procId);
        if (!unityPidSet.Contains(procId)) return true;
        sb.Length = 0;
        GetWindowTextW(hWnd, sb, sb.Capacity);
        var text = sb.ToString();
        foreach (var frag in TitleFragments) {
          if (text.IndexOf(frag, StringComparison.OrdinalIgnoreCase) >= 0) {
            matchedDialog = hWnd;
            return false;
          }
        }
        return true;
      }, IntPtr.Zero);
      if (matchedDialog == IntPtr.Zero) return "not-found";
      IntPtr matchedButton = IntPtr.Zero;
      EnumChildWindows(matchedDialog, (hWnd, lParam) => {
        sb.Length = 0;
        GetClassNameW(hWnd, sb, sb.Capacity);
        if (sb.ToString() != "Button") return true;
        sb.Length = 0;
        GetWindowTextW(hWnd, sb, sb.Capacity);
        var text = sb.ToString();
        foreach (var label in ButtonLabels) {
          if (string.Equals(text, label, StringComparison.OrdinalIgnoreCase)) {
            matchedButton = hWnd;
            return false;
          }
        }
        return true;
      }, IntPtr.Zero);
      if (matchedButton == IntPtr.Zero) return "not-found";
      SendMessageW(matchedButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
      return "dismissed:${DISMISS_BUTTON_LABEL}";
    }
  }
}
"@
  }
  $unityPids = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.Id })
  if ($unityPids.Count -eq 0) { Write-Output 'not-found'; return }
  Write-Output ([UnityMcp.LaunchErrors.Dismisser]::TryDismiss([int[]]$unityPids))
} catch {
  Write-Output ('error:' + $_.Exception.Message)
}
`;

async function tryDismissWindows(): Promise<DismissOutcome> {
  return new Promise<DismissOutcome>((resolve) => {
    execFile(
      'powershell',
      ['-NoProfile', '-NonInteractive', '-Command', WINDOWS_DISMISS_PS_SCRIPT],
      { timeout: 5000, windowsHide: true },
      (err, stdout) => {
        if (err) {
          // ETIMEDOUT, ENOENT (powershell missing), etc. Treat as transient.
          verbose(
            `tryDismissWindows: powershell invocation failed (${err.message}) — treating as not-found`,
          );
          resolve({ kind: 'error', message: err.message });
          return;
        }
        resolve(parseDismissOutput(stdout));
      },
    );
  });
}

// ---------------------------------------------------------------------------
// macOS — AppleScript via osascript
// ---------------------------------------------------------------------------

/**
 * The AppleScript snippet used for the macOS dismiss path. Exposed as
 * a string for testability.
 *
 * Strategy: iterate every window of the Unity application process and
 * click the first button titled `Ignore`. If the Unity process is not
 * running OR no matching button exists, the script reports
 * `not-found`. Any AppleScript exception (e.g. Accessibility
 * permission not granted) is reported as `error:<message>` so the
 * caller can surface it once and continue polling.
 *
 * Requires the Terminal / `unity-mcp-cli` binary to have been granted
 * Accessibility permission in System Settings → Privacy & Security →
 * Accessibility. Documented in the README.
 */
export const MACOS_DISMISS_APPLESCRIPT = `
on run
  try
    tell application "System Events"
      if not (exists process "Unity") then
        return "not-found"
      end if
      tell process "Unity"
        repeat with w in windows
          try
            if exists (button "${DISMISS_BUTTON_LABEL}" of w) then
              click button "${DISMISS_BUTTON_LABEL}" of w
              return "dismissed:${DISMISS_BUTTON_LABEL}"
            end if
          end try
        end repeat
      end tell
    end tell
    return "not-found"
  on error errMsg
    return "error:" & errMsg
  end try
end run
`;

async function tryDismissMacOS(): Promise<DismissOutcome> {
  return new Promise<DismissOutcome>((resolve) => {
    execFile(
      'osascript',
      ['-e', MACOS_DISMISS_APPLESCRIPT],
      { timeout: 5000 },
      (err, stdout) => {
        if (err) {
          verbose(
            `tryDismissMacOS: osascript invocation failed (${err.message}) — treating as transient error`,
          );
          resolve({ kind: 'error', message: err.message });
          return;
        }
        resolve(parseDismissOutput(stdout));
      },
    );
  });
}

// ---------------------------------------------------------------------------
// Linux/X11 — xdotool
// ---------------------------------------------------------------------------

/**
 * Whether `xdotool` is on PATH. Cached per process so the polling
 * loop pays the lookup cost at most once. `which xdotool` /
 * `command -v xdotool` is too coarse here — we directly try
 * `xdotool --version` and observe the exit code.
 */
let xdotoolPresence: boolean | undefined;

function isXdotoolAvailable(): boolean {
  if (xdotoolPresence !== undefined) return xdotoolPresence;
  try {
    execFileSync('xdotool', ['--version'], {
      stdio: 'ignore',
      timeout: 2000,
    });
    xdotoolPresence = true;
  } catch {
    xdotoolPresence = false;
  }
  return xdotoolPresence;
}

/**
 * Reset the cached `xdotool` presence flag. Test-only — production
 * code never needs to call this. Exposed so tests can simulate "the
 * tool was installed mid-process" without affecting other tests.
 */
export function _resetXdotoolPresenceForTests(): void {
  xdotoolPresence = undefined;
}

async function tryDismissLinuxX11(): Promise<DismissOutcome> {
  if (!isXdotoolAvailable()) {
    return {
      kind: 'error',
      message:
        'xdotool not found on PATH. Install it (e.g. `sudo apt-get install xdotool`) to enable Unity launch-errors auto-dismiss on Linux/X11. Wayland is not yet supported.',
    };
  }
  // Strategy:
  //   1. `xdotool search --name '<fragment>'` once per fragment, collect
  //      candidate window IDs. Empty stdout from `xdotool search` is
  //      reported via a non-zero exit; treat that as "no match".
  //   2. For each match, try to send the `Ignore` button's accelerator
  //      (Alt+I) — Unity's "Ignore" button does not always expose an
  //      accelerator, so we fall back to xdotool's `key Return` after
  //      activating the dialog window. The dialog defaults its focus
  //      to the leftmost button, which on this dialog IS `Ignore`.
  //   3. Activate the window (`windowactivate --sync`) before sending
  //      keys so the keystrokes route to the dialog rather than
  //      whichever window currently holds focus.
  return new Promise<DismissOutcome>((resolve) => {
    const fragments = LAUNCH_ERROR_DIALOG_TITLE_FRAGMENTS;
    let idx = 0;
    const tryNext = (): void => {
      if (idx >= fragments.length) {
        resolve({ kind: 'not-found' });
        return;
      }
      const fragment = fragments[idx++];
      execFile(
        'xdotool',
        ['search', '--name', fragment],
        { timeout: 2000 },
        (err, stdout) => {
          if (err || !stdout.trim()) {
            tryNext();
            return;
          }
          const winId = stdout.trim().split(/\s+/)[0];
          if (!winId) {
            tryNext();
            return;
          }
          // Activate then send Return to click the focused (Ignore) button.
          execFile(
            'xdotool',
            ['windowactivate', '--sync', winId, 'key', '--clearmodifiers', 'Return'],
            { timeout: 2000 },
            (activateErr) => {
              if (activateErr) {
                resolve({
                  kind: 'error',
                  message: `xdotool failed to dismiss window ${winId}: ${activateErr.message}`,
                });
                return;
              }
              resolve({ kind: 'dismissed', button: DISMISS_BUTTON_LABEL });
            },
          );
        },
      );
    };
    tryNext();
  });
}

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

/**
 * Parse the single-token contract every platform-specific dispatcher
 * writes to stdout. Exposed for tests so we can exhaustively cover
 * the parser without invoking PowerShell / osascript / xdotool.
 *
 * Contract:
 *   - `dismissed:<button>` → `{ kind: 'dismissed', button }`
 *   - `not-found`          → `{ kind: 'not-found' }`
 *   - `error:<message>`    → `{ kind: 'error', message }`
 *   - any other / empty    → `{ kind: 'not-found' }` (defensive — a
 *     transient parse miss is treated as "not yet visible" rather
 *     than aborting the polling loop)
 */
export function parseDismissOutput(stdout: string): DismissOutcome {
  const trimmed = stdout.trim();
  if (trimmed === '' || trimmed === 'not-found') return { kind: 'not-found' };
  if (trimmed.startsWith('dismissed:')) {
    const button = trimmed.substring('dismissed:'.length);
    return { kind: 'dismissed', button: button || DISMISS_BUTTON_LABEL };
  }
  if (trimmed.startsWith('error:')) {
    return { kind: 'error', message: trimmed.substring('error:'.length) };
  }
  return { kind: 'not-found' };
}
