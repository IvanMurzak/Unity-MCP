// Modal dialogs of a running Unity Editor (Windows): a dialog blocks the editor's main thread, so every tool call waits
// until a human answers it. `listEditorDialogs` finds them (title, text, buttons) and `pressEditorDialogButton` clicks
// one. A windowless (`-batchmode`) editor never shows dialogs. Library-safe: never throws, never prints.

import { execFile } from 'child_process';
import { platform as nodePlatform } from 'os';

export interface EditorDialog {
  /** Window handle, stable while the dialog is open; pass it to `pressEditorDialogButton`. */
  id: string;
  pid: number;
  title: string;
  /** The dialog's text lines (its static labels). */
  message: string;
  /** Button labels, without the `&` mnemonic marker. */
  buttons: string[];
}

export type ListDialogsResult =
  | { kind: 'ok'; dialogs: EditorDialog[] }
  | { kind: 'unsupported'; platform: string }
  | { kind: 'error'; message: string };

export type PressDialogResult =
  | { kind: 'pressed'; button: string }
  | { kind: 'not-found'; message: string }
  | { kind: 'unsupported'; platform: string }
  | { kind: 'error'; message: string };

/** Runs a PowerShell script and returns its stdout; injected by tests. */
export type PowerShellRunner = (script: string) => Promise<string>;

const runPowerShell: PowerShellRunner = script => new Promise((resolve, reject) => {
  execFile('powershell', ['-NoProfile', '-NonInteractive', '-Command', script], { timeout: 10_000, windowsHide: true }, (err, stdout) => {
    if (err) reject(err);
    else resolve(stdout);
  });
});

function isValidPid(pid: number): boolean {
  return Number.isSafeInteger(pid) && pid > 0;
}

// The Win32 helper both scripts load. Standard dialogs (`#32770`: Unity's DisplayDialog, "Enter Safe Mode?", the save
// prompt) are listed with their Static texts and Button labels; a button is pressed with BM_CLICK, which does not move
// the mouse or steal focus.
const HELPER = `
if (-not ([System.Management.Automation.PSTypeName]'UnityMcp.Dialogs.Finder').Type) {
  Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace UnityMcp.Dialogs {
  public static class Finder {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumWindowsProc f, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr SendMessageW(IntPtr h, uint m, IntPtr w, IntPtr l);
    static string Text(IntPtr h) { var sb = new StringBuilder(1024); GetWindowTextW(h, sb, sb.Capacity); return sb.ToString(); }
    static string Cls(IntPtr h) { var sb = new StringBuilder(256); GetClassNameW(h, sb, sb.Capacity); return sb.ToString(); }
    static string Esc(string s) {
      var sb = new StringBuilder();
      foreach (var c in s) {
        if (c == '"' || c == '\\\\') { sb.Append('\\\\'); sb.Append(c); }
        else if (c < ' ') sb.AppendFormat("\\\\u{0:x4}", (int)c);
        else sb.Append(c);
      }
      return sb.ToString();
    }
    public static List<IntPtr> Dialogs(int[] pids) {
      var set = new HashSet<uint>(); foreach (var p in pids) set.Add((uint)p);
      var found = new List<IntPtr>();
      EnumWindows((h, l) => {
        if (!IsWindowVisible(h)) return true;
        uint pid; GetWindowThreadProcessId(h, out pid);
        if (set.Contains(pid) && Cls(h) == "#32770") found.Add(h);
        return true;
      }, IntPtr.Zero);
      return found;
    }
    public static string List(int[] pids) {
      var items = new List<string>();
      foreach (var h in Dialogs(pids)) {
        uint pid; GetWindowThreadProcessId(h, out pid);
        var texts = new List<string>(); var buttons = new List<string>();
        EnumChildWindows(h, (c, l) => {
          if (!IsWindowVisible(c)) return true;
          var cls = Cls(c); var t = Text(c);
          if (t.Length == 0) return true;
          if (cls == "Button") buttons.Add("\\"" + Esc(t.Replace("&", "")) + "\\"");
          else if (cls == "Static") texts.Add(t);
          return true;
        }, IntPtr.Zero);
        items.Add("{\\"id\\":\\"" + h.ToInt64() + "\\",\\"pid\\":" + pid + ",\\"title\\":\\"" + Esc(Text(h)) + "\\",\\"message\\":\\"" + Esc(string.Join("\\n", texts)) + "\\",\\"buttons\\":[" + string.Join(",", buttons) + "]}");
      }
      return "[" + string.Join(",", items) + "]";
    }
    public static string Press(int[] pids, long id, string label) {
      foreach (var h in Dialogs(pids)) {
        if (id != 0 && h.ToInt64() != id) continue;
        IntPtr match = IntPtr.Zero;
        EnumChildWindows(h, (c, l) => {
          if (Cls(c) == "Button" && string.Equals(Text(c).Replace("&", ""), label, StringComparison.OrdinalIgnoreCase)) { match = c; return false; }
          return true;
        }, IntPtr.Zero);
        if (match != IntPtr.Zero) { SendMessageW(match, 0x00F5, IntPtr.Zero, IntPtr.Zero); return "pressed"; }
      }
      return "not-found";
    }
  }
}
"@
}
`;

function pidArray(pids: number[]): string {
  return `[int[]]@(${pids.filter(isValidPid).join(',')})`;
}

/** The PowerShell script that prints the dialogs of `pids` as a JSON array. Exposed for tests. */
export function listDialogsScript(pids: number[]): string {
  return `$ErrorActionPreference = 'Stop'
try {${HELPER}
  [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
  Write-Output ([UnityMcp.Dialogs.Finder]::List(${pidArray(pids)}))
} catch { Write-Output ('error:' + $_.Exception.Message) }`;
}

/** The PowerShell script that presses `button` on dialog `id` (0: any dialog of `pids`). Exposed for tests. */
export function pressDialogScript(pids: number[], id: bigint, button: string): string {
  // The label goes in as a single-quoted PowerShell literal ('' escapes a quote): never interpolated.
  const label = `'${button.replace(/'/g, "''")}'`;
  return `$ErrorActionPreference = 'Stop'
try {${HELPER}
  Write-Output ([UnityMcp.Dialogs.Finder]::Press(${pidArray(pids)}, [long]${id.toString()}, ${label}))
} catch { Write-Output ('error:' + $_.Exception.Message) }`;
}

/** Parse the `List` output; exposed for tests. */
export function parseDialogList(stdout: string): ListDialogsResult {
  const text = stdout.trim();
  if (text.startsWith('error:')) return { kind: 'error', message: text.slice('error:'.length).trim() };
  try {
    const parsed = JSON.parse(text) as EditorDialog[];
    if (!Array.isArray(parsed)) return { kind: 'error', message: `Unexpected dialog list: ${text}` };
    return { kind: 'ok', dialogs: parsed };
  } catch {
    return { kind: 'error', message: `Unexpected dialog list: ${text}` };
  }
}

/** The modal dialogs the editor processes `pids` show. Windows only; other platforms report `unsupported`. */
export async function listEditorDialogs(
  pids: number[],
  platform: string = nodePlatform(),
  run: PowerShellRunner = runPowerShell,
): Promise<ListDialogsResult> {
  if (platform !== 'win32') return { kind: 'unsupported', platform };
  const valid = pids.filter(isValidPid);
  if (valid.length === 0) return { kind: 'ok', dialogs: [] };
  try {
    return parseDialogList(await run(listDialogsScript(valid)));
  } catch (err) {
    return { kind: 'error', message: err instanceof Error ? err.message : String(err) };
  }
}

/** Press the button labelled `button` (case-insensitive, `&` ignored) on dialog `id`, or on any dialog when omitted. */
export async function pressEditorDialogButton(
  pids: number[],
  button: string,
  id?: string,
  platform: string = nodePlatform(),
  run: PowerShellRunner = runPowerShell,
): Promise<PressDialogResult> {
  if (platform !== 'win32') return { kind: 'unsupported', platform };
  let handle = 0n;
  if (id !== undefined) {
    if (!/^\d+$/.test(id)) return { kind: 'error', message: `Invalid dialog id: "${id}"` };
    handle = BigInt(id);
  }
  try {
    const out = (await run(pressDialogScript(pids.filter(isValidPid), handle, button))).trim();
    if (out === 'pressed') return { kind: 'pressed', button };
    if (out.startsWith('error:')) return { kind: 'error', message: out.slice('error:'.length).trim() };
    return { kind: 'not-found', message: `No open dialog has a "${button}" button.` };
  } catch (err) {
    return { kind: 'error', message: err instanceof Error ? err.message : String(err) };
  }
}
