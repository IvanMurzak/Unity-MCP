// Modal dialogs of a running Unity Editor (Windows, macOS, Linux): a dialog blocks the editor's main thread, so every
// tool call waits until a human answers it. `listEditorDialogs` finds them (title, text, buttons) and
// `pressEditorDialogButton` clicks one. A windowless (`-batchmode`) editor never shows dialogs. Library-safe: never
// throws, never prints.
//
// Windows: Win32 through PowerShell. macOS: System Events UI scripting through `osascript -l JavaScript` (JXA), which
// needs the Accessibility permission of the app that runs the CLI (terminal or agent app). Linux: the AT-SPI
// accessibility bus through a `python3` helper (PyGObject), which works on X11 and Wayland alike.

import { execFile } from 'child_process';
import * as fs from 'fs';
import { platform as nodePlatform } from 'os';
import * as path from 'path';

export interface EditorDialog {
  /**
   * Stable while the dialog is open; pass it to `pressEditorDialogButton`. Windows: the window handle. macOS, Linux: a
   * decimal FNV-1a hash of the pid, title, text and button labels (window order changes with focus, the content does
   * not).
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
 * PowerShell on Windows, JavaScript for Automation (`osascript -l JavaScript`) on macOS and Python 3 on Linux.
 */
export type ScriptRunner = (script: string, env?: Record<string, string>) => Promise<string>;

/** Environment variable that carries the button label into the press script (never interpolated into code). */
export const BUTTON_ENV = 'UNITY_MCP_DIALOG_BUTTON';
/** macOS, Linux: environment variables that carry the editor pids and the dialog id into the scripts. */
export const PIDS_ENV = 'UNITY_MCP_DIALOG_PIDS';
export const ID_ENV = 'UNITY_MCP_DIALOG_ID';

/**
 * The environment of the PowerShell helper. `Add-Type` compiles through files in TEMP; a process whose TEMP is the
 * machine-wide `C:\Windows\Temp` (services, CI agents, some shells) may not write there and fails with "Source file
 * ... could not be found", so the user's own temp folder is used when it exists.
 */
/** TEMP is the machine-wide `%SystemRoot%\Temp` (only then is it swapped: any other TEMP was chosen on purpose). */
function isMachineTemp(env: NodeJS.ProcessEnv): boolean {
  const temp = env['TEMP'];
  const systemRoot = env['SystemRoot'] ?? env['SYSTEMROOT'] ?? 'C:\\Windows';
  return !!temp && path.win32.resolve(temp).toLowerCase() === path.win32.resolve(systemRoot, 'Temp').toLowerCase();
}

export function powerShellEnv(env: NodeJS.ProcessEnv = process.env, exists: (dir: string) => boolean = fs.existsSync): NodeJS.ProcessEnv {
  const userTemp = env['LOCALAPPDATA'] ? path.join(env['LOCALAPPDATA'], 'Temp') : undefined;
  return userTemp && isMachineTemp(env) && exists(userTemp) ? { ...env, TEMP: userTemp, TMP: userTemp } : env;
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
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr h);
    static bool Closed(IntPtr h) {
      for (var i = 0; i < 20; i++) { if (!IsWindow(h) || !IsWindowVisible(h)) return true; System.Threading.Thread.Sleep(50); }
      return false;
    }
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
        if (match == IntPtr.Zero) continue;
        if (!IsWindowEnabled(match)) return "disabled";
        SendMessageW(match, 0x00F5, IntPtr.Zero, IntPtr.Zero);
        if (Closed(h)) return "pressed";
        // BM_CLICK can be lost on a dialog that is not the active window (another process's dialog); the dialog's own
        // WM_COMMAND / BN_CLICKED notification needs no activation.
        SendMessageW(h, 0x0111, new IntPtr(GetDlgCtrlID(match) & 0xFFFF), match);
        return Closed(h) ? "pressed" : "still-open";
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
  const repeated = title ? texts.indexOf(title) : -1;
  if (repeated >= 0) texts.splice(repeated, 1);
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

// Linux. One constant Python 3 script per mode (list, press) that reads the AT-SPI accessibility tree through
// PyGObject (`gi.repository.Atspi`); the pids, the dialog id and the button label arrive through environment variables,
// so nothing user-supplied is ever part of the script text. AT-SPI is a D-Bus bus, not a window-system API, so the same
// script works on X11 and Wayland: GTK (Unity's `EditorUtility.DisplayDialog`, zenity) registers there by default. A
// dialog is a top-level window of an application whose pid is listed, with role DIALOG/ALERT/FILE_CHOOSER or the MODAL
// state, and at least one labelled push button. Its texts are the showing labels outside buttons. Output: a JSON array
// (ASCII), `pressed`, `not-found`, or `error:<code>: <detail>` (codes: see `LINUX_HINTS`; `atspi` is any other
// AT-SPI failure).
const LINUX_SCRIPT = (mode: 'list' | 'press') => `import json, os, sys

def out(text):
    # UTF-8 bytes whatever the locale: an error text may hold a non-ASCII label or detail.
    sys.stdout.buffer.write(text.encode('utf-8'))
    sys.exit(0)

def fail(code, detail):
    out('error:' + code + ': ' + ' '.join(str(detail).split()))

def no_session(code):
    return code if os.environ.get('DISPLAY') or os.environ.get('WAYLAND_DISPLAY') else 'no-display'

try:
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi, Gio, GLib
except Exception as e:
    fail('no-atspi', e)

def check_bus():
    # libatspi aborts the process when it cannot reach the bus, so reach it first: AT_SPI_BUS_ADDRESS, else the address
    # the session bus hands out (org.a11y.Bus, started on demand by at-spi-bus-launcher).
    address = os.environ.get('AT_SPI_BUS_ADDRESS', '')
    try:
        if not address:
            bus = Gio.bus_get_sync(Gio.BusType.SESSION, None)
            reply = bus.call_sync('org.a11y.Bus', '/org/a11y/bus', 'org.a11y.Bus', 'GetAddress', None,
                                  GLib.VariantType.new('(s)'), Gio.DBusCallFlags.NONE, 3000, None)
            address = reply.unpack()[0]
        flags = Gio.DBusConnectionFlags.AUTHENTICATION_CLIENT | Gio.DBusConnectionFlags.MESSAGE_BUS_CONNECTION
        Gio.DBusConnection.new_for_address_sync(address, flags, None, None).close_sync(None)
    except Exception as e:
        fail(no_session('no-bus'), e)

def fnv(text):
    # FNV-1a over UTF-16 code units, the same id the macOS script computes.
    h = 0x811c9dc5
    data = text.encode('utf-16-le', 'surrogatepass')
    for i in range(0, len(data), 2):
        h = ((h ^ (data[i] | (data[i + 1] << 8))) * 0x01000193) & 0xffffffff
    return str(h)

R = Atspi.Role
S = Atspi.StateType
DIALOG_ROLES = set(getattr(R, n) for n in ('DIALOG', 'ALERT', 'FILE_CHOOSER') if hasattr(R, n))
TEXT_ROLES = set(getattr(R, n) for n in ('LABEL', 'STATIC') if hasattr(R, n))
IMAGE_ROLES = set(getattr(R, n) for n in ('ICON', 'IMAGE') if hasattr(R, n))
# Big containers (a file chooser's file list, menus) hold no dialog text or answer buttons: not walked.
SKIP_ROLES = set(getattr(R, n) for n in ('TABLE', 'TREE_TABLE', 'TREE', 'LIST', 'LIST_BOX', 'MENU', 'MENU_BAR') if hasattr(R, n))

def text_of(el):
    name = el.get_name() or ''
    if name or el.get_text_iface() is None:
        return name
    return Atspi.Text.get_text(el, 0, Atspi.Text.get_character_count(el)) or ''

def collect(el, found, budget):
    if budget[0] <= 0:
        return
    budget[0] -= 1
    role = el.get_role()
    if role in SKIP_ROLES:
        return
    showing = el.get_state_set().contains(S.SHOWING)
    if role == R.PUSH_BUTTON:
        name = (el.get_name() or '').strip()
        if name and showing:
            found['buttons'].append((name, el))
        return
    if role in TEXT_ROLES:
        text = text_of(el).strip()
        if text and showing:
            found['texts'].append(text)
    elif role in IMAGE_ROLES:
        found['images'].append(el.get_name() or '')
    for i in range(el.get_child_count()):
        child = el.get_child_at_index(i)
        if child is not None:
            collect(child, found, budget)

def describe(pid, win):
    found = {'texts': [], 'buttons': [], 'images': []}
    collect(win, found, [2000])
    if not found['buttons']:
        return None
    texts = found['texts']
    title = (win.get_name() or '').strip()
    # A titled window repeats its title as a label (GTK's title bar). An untitled GtkMessageDialog (Unity's
    # DisplayDialog) is named after its icon ("Question"): untitled. As on macOS, an untitled alert's first text is its
    # title.
    if title in texts:
        texts.remove(title)
    elif title in found['images']:
        title = ''
    if not title and len(texts) > 1:
        title = texts.pop(0)
    labels = [b[0] for b in found['buttons']]
    message = chr(10).join(texts)
    key = chr(1).join([str(pid), title, message, chr(2).join(labels)])
    return {'id': fnv(key), 'pid': pid, 'title': title, 'message': message, 'buttons': labels, 'el': found['buttons']}

def dialogs(pids):
    desktop = Atspi.get_desktop(0)
    count = desktop.get_child_count()
    if count == 0:
        fail(no_session('no-apps'), 'no application is registered on the accessibility bus')
    result = []
    for i in range(count):
        # An application or window that closes or stops answering mid-scan is skipped, not fatal to the others.
        try:
            app = desktop.get_child_at_index(i)
            pid = app.get_process_id() if app is not None else 0
            if pid not in pids:
                continue
            windows = [app.get_child_at_index(j) for j in range(app.get_child_count())]
        except Exception:
            continue
        for win in windows:
            try:
                states = win.get_state_set()
                if not states.contains(S.VISIBLE):
                    continue
                if win.get_role() not in DIALOG_ROLES and not states.contains(S.MODAL):
                    continue
                d = describe(pid, win)
                if d:
                    result.append(d)
            except Exception:
                pass
    return result

def press(el):
    # The Action interface: GTK buttons offer "click".
    count = Atspi.Action.get_n_actions(el)
    names = [(Atspi.Action.get_action_name(el, i) or '').lower() for i in range(count)]
    for wanted in ('click', 'press', 'activate'):
        if wanted in names:
            return Atspi.Action.do_action(el, names.index(wanted))
    return count > 0 and Atspi.Action.do_action(el, 0)

try:
    check_bus()
    pids = set(int(p) for p in os.environ.get('${PIDS_ENV}', '').split(',') if p.strip().isdigit() and int(p) > 0)
    found = dialogs(pids)
${mode === 'list'
    ? `    out(json.dumps([{k: d[k] for k in ('id', 'pid', 'title', 'message', 'buttons')} for d in found]))`
    : `    want = os.environ.get('${ID_ENV}', '')
    label = os.environ.get('${BUTTON_ENV}', '').lower()
    for d in found:
        if want and d['id'] != want:
            continue
        for name, el in d['el']:
            if name.lower() == label:
                if press(el):
                    out('pressed')
                out('error:The button "' + name + '" did not accept the click action.')
    out('not-found')`}
except Exception as e:
    fail('atspi', e)
`;

/** The Python script that prints the dialogs of the pids in `$UNITY_MCP_DIALOG_PIDS` as a JSON array (Linux). Constant. */
export const LINUX_LIST_DIALOGS_SCRIPT = LINUX_SCRIPT('list');
/**
 * The Python script that presses the button named by `$UNITY_MCP_DIALOG_BUTTON` (case-insensitive) on dialog
 * `$UNITY_MCP_DIALOG_ID` (empty: any dialog of the pids). Constant: no label, id or pid is ever part of the text.
 */
export const LINUX_PRESS_DIALOG_SCRIPT = LINUX_SCRIPT('press');

/** Linux: what to install or enable for each helper error code. */
export const LINUX_HINTS: Record<string, string> = {
  'no-python': 'Dialog control on Linux runs python3: install it with PyGObject and the AT-SPI typelib '
    + '(Debian/Ubuntu: python3 python3-gi gir1.2-atspi-2.0).',
  'no-atspi': 'Dialog control on Linux needs PyGObject and the AT-SPI typelib: install python3-gi and gir1.2-atspi-2.0 '
    + '(Debian/Ubuntu), python3-gobject and at-spi2-core (Fedora) or python-gobject and at-spi2-core (Arch).',
  'no-bus': 'The AT-SPI accessibility bus is not running or not reachable: install at-spi2-core and run the CLI in the '
    + 'desktop session of the user who runs the editor (its DBUS_SESSION_BUS_ADDRESS).',
  'no-display': 'There is no graphical session (neither DISPLAY nor WAYLAND_DISPLAY is set) and the accessibility bus '
    + 'is unreachable or empty: run the CLI in the desktop session that runs the editor; on a headless machine, run the '
    + 'editor and the CLI under Xvfb inside one dbus-run-session with at-spi2-core installed (with its own '
    + 'XDG_RUNTIME_DIR when the user also has a desktop session: the private bus would replace its at-spi socket).',
  'no-apps': 'No application is registered on the accessibility bus, so accessibility is off in this session: enable it '
    + '(GNOME: gsettings set org.gnome.desktop.interface toolkit-accessibility true), make sure NO_AT_BRIDGE is not set '
    + 'for the editor, then restart the editor.',
};

/** Linux: append the install/enable hint to a helper error `<code>: <detail>`. Exposed for tests. */
export function linuxErrorMessage(message: string): string {
  const [, code = '', detail = ''] = /^([a-z-]+):(.*)$/s.exec(message) ?? [];
  const hint = LINUX_HINTS[code];
  return hint ? `${detail.trim()}. ${hint}` : message;
}

/**
 * Linux: the message for a failed `python3` run, as `<code>: <detail>` when a hint applies. Not found on PATH:
 * `no-python`; libatspi aborting because it cannot reach the bus: `no-bus`. Exposed for tests.
 */
export function linuxRunFailure(err: { code?: string | number | null; killed?: boolean }, stderr: string): string {
  const lastLine = stderr.trim().split('\n').pop()?.trim() ?? '';
  if (err.code === 'ENOENT') return 'no-python: python3 was not found on PATH';
  if (/accessibility bus/i.test(stderr)) return `no-bus: ${lastLine}`;
  if (err.killed) return 'The accessibility bus did not answer within 5 s: the editor may be busy.';
  // `err.message` would repeat the whole script.
  return lastLine || `python3 exited with code ${String(err.code)}`;
}

const runPython: ScriptRunner = (script, env) => new Promise((resolve, reject) => {
  // cwd `/`: `python3 -c` imports from the working directory first, and the project folder could shadow a module.
  execFile('python3', ['-c', script], { timeout: 5_000, cwd: '/', env: { ...process.env, ...env } }, (err, stdout, stderr) => {
    if (err) reject(new Error(linuxRunFailure(err, String(stderr))));
    else resolve(stdout);
  });
});

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
  return platform === 'win32' || platform === 'darwin' || platform === 'linux';
}

function defaultRunner(platform: string): ScriptRunner {
  return platform === 'darwin' ? runOsaScript : platform === 'linux' ? runPython : runPowerShell;
}

/** A helper error with the permission to grant (macOS) or the package to install (Linux) added. */
function explainError(platform: string, message: string): { kind: 'error'; message: string } {
  return { kind: 'error', message: platform === 'darwin' ? macPermissionMessage(message) : platform === 'linux' ? linuxErrorMessage(message) : message };
}

function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

/** The modal dialogs the editor processes `pids` show. Windows, macOS and Linux; other platforms report `unsupported`. */
export async function listEditorDialogs(
  pids: number[],
  platform: string = nodePlatform(),
  run: ScriptRunner = defaultRunner(platform),
): Promise<ListDialogsResult> {
  if (!isSupported(platform)) return { kind: 'unsupported', platform };
  const valid = pids.filter(isValidPid);
  if (valid.length === 0) return { kind: 'ok', dialogs: [] };
  let result: ListDialogsResult;
  try {
    result = parseDialogList(platform === 'win32'
      ? await run(listDialogsScript(valid))
      : await run(platform === 'darwin' ? MAC_LIST_DIALOGS_SCRIPT : LINUX_LIST_DIALOGS_SCRIPT, { [PIDS_ENV]: valid.join(',') }));
  } catch (err) {
    result = { kind: 'error', message: errorMessage(err) };
  }
  return result.kind === 'error' ? explainError(platform, result.message) : result;
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
  let result: PressDialogResult;
  try {
    const out = (platform === 'win32'
      ? await run(pressDialogScript(valid, id === undefined ? 0n : BigInt(id)), { [BUTTON_ENV]: button })
      : await run(platform === 'darwin' ? MAC_PRESS_DIALOG_SCRIPT : LINUX_PRESS_DIALOG_SCRIPT,
        { [PIDS_ENV]: valid.join(','), [ID_ENV]: id ?? '', [BUTTON_ENV]: button })).trim();
    if (out === 'pressed') result = { kind: 'pressed', button };
    else if (out === 'disabled') result = { kind: 'error', message: `The "${button}" button is disabled.` };
    else if (out === 'still-open') result = { kind: 'error', message: `Pressed "${button}", but the dialog is still open: answer it in the editor.` };
    else if (out.startsWith('error:')) result = { kind: 'error', message: out.slice('error:'.length).trim() };
    else result = { kind: 'not-found', message: `No open dialog has a "${button}" button.` };
  } catch (err) {
    result = { kind: 'error', message: errorMessage(err) };
  }
  return result.kind === 'error' ? explainError(platform, result.message) : result;
}
