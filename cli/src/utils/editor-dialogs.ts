// Modal dialogs of a running Unity Editor (Windows, macOS): a dialog blocks the editor's main thread, so every tool call
// waits until a human answers it. `listEditorDialogs` finds them (title, text, buttons) and `pressEditorDialogButton`
// clicks one. A windowless (`-batchmode`) editor never shows dialogs. Library-safe: never throws, never prints.
//
// Windows: Win32 through PowerShell. macOS: System Events UI scripting through `osascript -l JavaScript` (JXA), which
// needs the Accessibility permission of the app that runs the CLI (terminal or agent app).

import { execFile } from 'child_process';
import * as fs from 'fs';
import { platform as nodePlatform } from 'os';
import * as path from 'path';

export interface EditorDialog {
  /**
   * Stable while the dialog is open; pass it to `pressEditorDialogButton`. Windows: the window handle. macOS: a decimal
   * FNV-1a hash of the pid, title, text and button labels (window order changes with focus, the content does not).
   */
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

/**
 * Runs a helper script (with extra environment variables) and returns its stdout; injected by tests. The script is
 * PowerShell on Windows and JavaScript for Automation (`osascript -l JavaScript`) on macOS.
 */
export type ScriptRunner = (script: string, env?: Record<string, string>) => Promise<string>;

/** Environment variable that carries the button label into the press script (never interpolated into code). */
export const BUTTON_ENV = 'UNITY_MCP_DIALOG_BUTTON';
/** macOS: environment variables that carry the editor pids and the dialog id into the scripts. */
export const PIDS_ENV = 'UNITY_MCP_DIALOG_PIDS';
export const ID_ENV = 'UNITY_MCP_DIALOG_ID';

/**
 * The environment of the PowerShell helper. `Add-Type` compiles through files in TEMP; a process whose TEMP is the
 * machine-wide `C:\Windows\Temp` (services, CI agents, some shells) may not write there and fails with "Source file
 * ... could not be found", so the user's own temp folder is used when it exists.
 */
export function powerShellEnv(env: NodeJS.ProcessEnv = process.env, exists: (dir: string) => boolean = fs.existsSync): NodeJS.ProcessEnv {
  const userTemp = env.LOCALAPPDATA ? path.join(env.LOCALAPPDATA, 'Temp') : undefined;
  return userTemp && exists(userTemp) ? { ...env, TEMP: userTemp, TMP: userTemp } : env;
}

const runPowerShell: ScriptRunner = (script, env) => new Promise((resolve, reject) => {
  execFile('powershell', ['-NoProfile', '-NonInteractive', '-Command', script], { timeout: 10_000, windowsHide: true, env: { ...powerShellEnv(), ...env } }, (err, stdout) => {
    if (err) reject(err);
    else resolve(stdout);
  });
});

const MAC_TIMEOUT_MESSAGE = 'System Events did not answer within 5 s: the editor may be busy, or a macOS permission '
  + 'prompt is waiting for an answer.';

const runOsaScript: ScriptRunner = (script, env) => new Promise((resolve, reject) => {
  // Short: the scan runs after a timeout, when a busy (not blocked) editor also stalls System Events.
  execFile('osascript', ['-l', 'JavaScript', '-e', script], { timeout: 5_000, env: { ...process.env, ...env } }, (err, stdout, stderr) => {
    // osascript's own error text is on stderr; `err.message` would repeat the whole script.
    if (err) reject(new Error(String(stderr).trim() || (err.killed ? MAC_TIMEOUT_MESSAGE : err.message)));
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

/**
 * The PowerShell script that presses the button named by `$env:UNITY_MCP_DIALOG_BUTTON` on dialog `id` (0: any dialog
 * of `pids`). The label never enters the script text, so no quoting can break out of it. Exposed for tests.
 */
export function pressDialogScript(pids: number[], id: bigint): string {
  return `$ErrorActionPreference = 'Stop'
try {${HELPER}
  Write-Output ([UnityMcp.Dialogs.Finder]::Press(${pidArray(pids)}, [long]${id.toString()}, $env:${BUTTON_ENV}))
} catch { Write-Output ('error:' + $_.Exception.Message) }`;
}

// macOS. One constant JXA script per mode (list, press): the pids, the dialog id and the button label
// arrive through environment variables, so nothing user-supplied is ever part of the script text. A dialog is a window
// of the editor process whose subrole is AXDialog/AXSystemDialog or that is AXModal, or any sheet of its windows, with
// at least one labelled button (the title-bar buttons have none, a progress window has none). Output: a JSON array
// (non-ASCII escaped), `pressed`, `not-found`, or `error:<message> (<number>)`.
const MAC_SCRIPT = (mode: 'list' | 'press') => `ObjC.import('Foundation');
function env(name) {
  const v = $.NSProcessInfo.processInfo.environment.objectForKey(name);
  return v.isNil() ? '' : v.js;
}
function fnv(s) {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return String(h >>> 0);
}
function strings(list) {
  return (list || []).map(x => (x === null || x === undefined) ? '' : String(x)).filter(x => x.length > 0);
}
function attr(el, name) {
  try { return el.attributes.byName(name).value(); } catch (e) { return null; }
}
function describe(pid, el) {
  const buttons = strings(el.buttons.name());
  if (buttons.length === 0) return null;
  let title = '';
  try { title = el.name() || ''; } catch (e) {}
  let texts = [];
  try { texts = strings(el.staticTexts.value()); } catch (e) {}
  if (texts.length === 0) { try { texts = strings(el.staticTexts.name()); } catch (e) {} }
  // A titled window repeats its title as a static text. An untitled alert (NSAlert: Unity's DisplayDialog) shows its
  // title as the first, bold text: report it as the title, as on Windows.
  texts = texts.filter(t => t !== title);
  if (title === '' && texts.length > 1) title = texts.shift();
  const message = texts.join('\\n');
  return { id: fnv([pid, title, message, buttons.join('\\u0002')].join('\\u0001')), pid: pid, title: title, message: message, buttons: buttons, el: el };
}
function dialogs(se, pid) {
  const found = [];
  const procs = se.processes.whose({ unixId: pid })();
  if (procs.length === 0) return found;
  const windows = procs[0].windows();
  for (const w of windows) {
    const sub = attr(w, 'AXSubrole');
    if (sub === 'AXDialog' || sub === 'AXSystemDialog' || attr(w, 'AXModal') === true) {
      // A window that closes or stops answering mid-scan is skipped, not fatal to the others.
      try { const d = describe(pid, w); if (d) found.push(d); } catch (e) {}
    }
    let sheets = [];
    try { sheets = w.sheets(); } catch (e) {}
    for (const s of sheets) {
      try { const d = describe(pid, s); if (d) found.push(d); } catch (e) {}
    }
  }
  return found;
}
function run() {
  try {
    const se = Application('System Events');
    const pids = env('${PIDS_ENV}').split(',').map(Number).filter(p => Number.isSafeInteger(p) && p > 0);
    const all = [];
    for (const pid of pids) for (const d of dialogs(se, pid)) all.push(d);
${mode === 'list'
    ? `    const json = JSON.stringify(all.map(d => ({ id: d.id, pid: d.pid, title: d.title, message: d.message, buttons: d.buttons })));
    return json.replace(/[\\u007f-\\uffff]/g, c => '\\\\u' +('0000' + c.charCodeAt(0).toString(16)).slice(-4));`
    : `    const id = env('${ID_ENV}');
    const label = env('${BUTTON_ENV}').toLowerCase();
    for (const d of all) {
      if (id && d.id !== id) continue;
      const names = d.el.buttons.name();
      for (let i = 0; i < names.length; i++) {
        if (names[i] !== null && names[i] !== undefined && String(names[i]).toLowerCase() === label) {
          d.el.buttons[i].click();
          return 'pressed';
        }
      }
    }
    return 'not-found';`}
  } catch (e) {
    return 'error:' + (e && e.message ? e.message : String(e)) + (e && e.errorNumber ? ' (' + e.errorNumber + ')' : '');
  }
}`;

/** The JXA script that prints the dialogs of the pids in `$UNITY_MCP_DIALOG_PIDS` as a JSON array (macOS). Constant. */
export const MAC_LIST_DIALOGS_SCRIPT = MAC_SCRIPT('list');
/**
 * The JXA script that presses the button named by `$UNITY_MCP_DIALOG_BUTTON` (case-insensitive) on dialog
 * `$UNITY_MCP_DIALOG_ID` (empty: any dialog of the pids). Constant: no label, id or pid is ever part of the text.
 */
export const MAC_PRESS_DIALOG_SCRIPT = MAC_SCRIPT('press');

/** The Settings pane that unblocks UI scripting on macOS. */
export const MAC_ACCESSIBILITY_HINT =
  'Grant Accessibility access to the app that runs unity-mcp-cli (your terminal or AI agent app) in System Settings → ' +
  'Privacy & Security → Accessibility, then run the command again.';

/**
 * macOS: turn a System Events refusal into a message that names the permission to grant. Assistive access denied shows
 * up as -25211 (JXA), -1719 or -1728 with "not allowed assistive access" (AppleScript); -1743 is the Automation
 * permission for controlling System Events. Exposed for tests.
 */
export function macPermissionMessage(message: string): string {
  // -1719 alone is also "invalid index" (a window closed mid-scan): only its access-denied texts count.
  if (/-25211\b|assistive access|access not allowed/i.test(message)) {
    return `macOS denied access to the editor's windows (${message}). ${MAC_ACCESSIBILITY_HINT}`;
  }
  if (/-1743\b|not authori[sz]ed to send apple events/i.test(message)) {
    return `macOS denied control of System Events (${message}). Allow the app that runs unity-mcp-cli to control ` +
      '"System Events" in System Settings → Privacy & Security → Automation, then run the command again.';
  }
  return message;
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

function isSupported(platform: string): boolean {
  return platform === 'win32' || platform === 'darwin';
}

function defaultRunner(platform: string): ScriptRunner {
  return platform === 'darwin' ? runOsaScript : runPowerShell;
}

function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** The modal dialogs the editor processes `pids` show. Windows and macOS; other platforms report `unsupported`. */
export async function listEditorDialogs(
  pids: number[],
  platform: string = nodePlatform(),
  run: ScriptRunner = defaultRunner(platform),
): Promise<ListDialogsResult> {
  if (!isSupported(platform)) return { kind: 'unsupported', platform };
  const valid = pids.filter(isValidPid);
  if (valid.length === 0) return { kind: 'ok', dialogs: [] };
  const mac = platform === 'darwin';
  let result: ListDialogsResult;
  try {
    result = parseDialogList(mac
      ? await run(MAC_LIST_DIALOGS_SCRIPT, { [PIDS_ENV]: valid.join(',') })
      : await run(listDialogsScript(valid)));
  } catch (err) {
    result = { kind: 'error', message: errorMessage(err) };
  }
  return mac && result.kind === 'error' ? { kind: 'error', message: macPermissionMessage(result.message) } : result;
}

/** Press the button labelled `button` (case-insensitive, `&` ignored) on dialog `id`, or on any dialog when omitted. */
export async function pressEditorDialogButton(
  pids: number[],
  button: string,
  id?: string,
  platform: string = nodePlatform(),
  run: ScriptRunner = defaultRunner(platform),
): Promise<PressDialogResult> {
  if (!isSupported(platform)) return { kind: 'unsupported', platform };
  if (id !== undefined && !/^\d+$/.test(id)) return { kind: 'error', message: `Invalid dialog id: "${id}"` };
  const valid = pids.filter(isValidPid);
  const mac = platform === 'darwin';
  let result: PressDialogResult;
  try {
    const out = (mac
      ? await run(MAC_PRESS_DIALOG_SCRIPT, { [PIDS_ENV]: valid.join(','), [ID_ENV]: id ?? '', [BUTTON_ENV]: button })
      : await run(pressDialogScript(valid, id === undefined ? 0n : BigInt(id)), { [BUTTON_ENV]: button })).trim();
    if (out === 'pressed') result = { kind: 'pressed', button };
    else if (out.startsWith('error:')) result = { kind: 'error', message: out.slice('error:'.length).trim() };
    else result = { kind: 'not-found', message: `No open dialog has a "${button}" button.` };
  } catch (err) {
    result = { kind: 'error', message: errorMessage(err) };
  }
  return mac && result.kind === 'error' ? { kind: 'error', message: macPermissionMessage(result.message) } : result;
}
