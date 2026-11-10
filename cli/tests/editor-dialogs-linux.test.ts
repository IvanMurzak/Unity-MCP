import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { execFile, execFileSync } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  BUTTON_ENV,
  ID_ENV,
  LINUX_LIST_DIALOGS_SCRIPT,
  LINUX_PRESS_DIALOG_SCRIPT,
  PIDS_ENV,
  linuxErrorMessage,
  linuxRunFailure,
  listEditorDialogs,
  pressEditorDialogButton,
  type ScriptRunner,
} from '../src/utils/editor-dialogs.js';

/** A runner that records what it was given and answers with `reply` (or throws it when it is an Error). */
function stubRunner(reply: string | Error, seen: { script?: string; env?: Record<string, string> } = {}): ScriptRunner {
  return async (script, env = {}) => {
    seen.script = script;
    seen.env = env;
    if (reply instanceof Error) throw reply;
    return reply;
  };
}

describe('editor dialogs on Linux (injected runner)', () => {
  it('runs the constant list script with the valid pids in the environment and parses its output', async () => {
    const seen: { script?: string; env?: Record<string, string> } = {};
    const json = '[{"id":"123","pid":42,"title":"Hold On","message":"Save?","buttons":["Save","Cancel"]}]';
    const listed = await listEditorDialogs([42, 0, -3, 7], 'linux', stubRunner(json, seen));
    expect(seen.script).toBe(LINUX_LIST_DIALOGS_SCRIPT);
    expect(seen.env).toEqual({ [PIDS_ENV]: '42,7' });
    expect(listed).toEqual({ kind: 'ok', dialogs: [{ id: '123', pid: 42, title: 'Hold On', message: 'Save?', buttons: ['Save', 'Cancel'] }] });
    expect(await listEditorDialogs([0], 'linux', stubRunner('boom'))).toEqual({ kind: 'ok', dialogs: [] });
    expect((await listEditorDialogs([42], 'linux', stubRunner('garbage'))).kind).toBe('error');
  });

  it('passes the label and id through the environment; the script text is constant', async () => {
    const label = "Don't \" + __import__('os').system('rm -rf ~') + ' ${y}";
    const seen: { script?: string; env?: Record<string, string> } = {};
    expect(await pressEditorDialogButton([42], label, '987', 'linux', stubRunner('pressed\n', seen))).toEqual({ kind: 'pressed', button: label });
    expect(seen.script).toBe(LINUX_PRESS_DIALOG_SCRIPT);
    expect(seen.script).not.toContain('rm -rf');
    expect(seen.script).not.toContain('987');
    expect(seen.env).toEqual({ [PIDS_ENV]: '42', [ID_ENV]: '987', [BUTTON_ENV]: label });
    expect(LINUX_LIST_DIALOGS_SCRIPT).not.toContain('42');
    expect(await pressEditorDialogButton([42], 'OK', '12; rm', 'linux', stubRunner('pressed'))).toMatchObject({ kind: 'error' });
  });

  it('maps not-found and helper errors', async () => {
    expect(await pressEditorDialogButton([42], 'Nope', undefined, 'linux', stubRunner('not-found')))
      .toEqual({ kind: 'not-found', message: 'No open dialog has a "Nope" button.' });
    expect(await pressEditorDialogButton([42], 'OK', undefined, 'linux', stubRunner('error:The button "OK" did not accept the click action.')))
      .toEqual({ kind: 'error', message: 'The button "OK" did not accept the click action.' });
  });

  it('names the package to install or the setting to enable', async () => {
    const cases: [string, string][] = [
      ['no-atspi: Namespace Atspi not available', 'gir1.2-atspi-2.0'],
      ['no-bus: Could not connect: No such file or directory', 'at-spi2-core'],
      ['no-display: Cannot autolaunch D-Bus without X11 $DISPLAY', 'DISPLAY nor WAYLAND_DISPLAY'],
      ['no-apps: no application is registered on the accessibility bus', 'toolkit-accessibility true'],
    ];
    for (const [helperError, hint] of cases) {
      for (const result of [
        await listEditorDialogs([42], 'linux', stubRunner(`error:${helperError}`)),
        await pressEditorDialogButton([42], 'OK', undefined, 'linux', stubRunner(`error:${helperError}`)),
      ]) {
        expect(result.kind).toBe('error');
        if (result.kind !== 'error') continue;
        expect(result.message).toContain(helperError.slice(helperError.indexOf(':') + 2));
        expect(result.message).toContain(hint);
        expect(result.message).not.toMatch(/^no-/);
      }
    }
    // A failed run (python3 missing) is mapped the same way.
    const thrown = await listEditorDialogs([42], 'linux', stubRunner(new Error('no-python: python3 was not found on PATH')));
    expect(thrown).toEqual({ kind: 'error', message: expect.stringContaining('python3-gi gir1.2-atspi-2.0') });
    expect(linuxErrorMessage('atspi: something odd')).toBe('atspi: something odd');
    expect(linuxErrorMessage('plain failure')).toBe('plain failure');
  });

  it('turns a failed python3 run into a helper error code', () => {
    expect(linuxRunFailure({ code: 'ENOENT' }, '')).toMatch(/^no-python: /);
    const abort = '\n(process:1): dbind-WARNING **: Couldn\'t connect to accessibility bus: Failed to connect to socket /x\n'
      + '(process:1): dbind-ERROR **: AT-SPI: Couldn\'t connect to accessibility bus. Is at-spi-bus-launcher running?\n';
    expect(linuxRunFailure({ code: null }, abort)).toBe('no-bus: (process:1): dbind-ERROR **: AT-SPI: Couldn\'t connect to accessibility bus. Is at-spi-bus-launcher running?');
    expect(linuxRunFailure({ code: null, killed: true }, '')).toBe('The accessibility bus did not answer within 5 s: the editor may be busy.');
    expect(linuxRunFailure({ code: 1 }, 'Traceback\nValueError: x\n')).toBe('ValueError: x');
    expect(linuxRunFailure({ code: 1 }, '')).toBe('python3 exited with code 1');
  });
});

// The real scripts, run by python3 against a fake `gi` package (the AT-SPI tree comes from $FAKE_ATSPI), so the
// walking, title and id rules are tested without a desktop session.
const FAKE_GI_INIT = `import os
def require_version(name, version):
    if os.environ.get('FAKE_NO_ATSPI'):
        raise ValueError('Namespace %s not available' % name)
`;

const FAKE_GI_REPOSITORY = `import json, os
from types import SimpleNamespace

SCENE = json.loads(os.environ.get('FAKE_ATSPI', '{}'))
ROLES = ['DIALOG', 'ALERT', 'FILE_CHOOSER', 'FRAME', 'PANEL', 'FILLER', 'LABEL', 'STATIC', 'ICON', 'IMAGE',
         'PUSH_BUTTON', 'TABLE', 'TREE_TABLE', 'TREE', 'LIST', 'LIST_BOX', 'MENU', 'MENU_BAR', 'APPLICATION', 'DESKTOP_FRAME']

class States:
    def __init__(self, names): self.names = set(names)
    def contains(self, state): return state in self.names

class Node:
    def __init__(self, spec, pid=0):
        self.spec = spec; self.pid = pid
        self.children = [Node(c, pid) for c in spec.get('children', [])]
    def get_name(self): return self.spec.get('name', '')
    def get_role(self): return self.spec.get('role', 'PANEL')
    def get_state_set(self): return States(self.spec.get('states', ['VISIBLE', 'SHOWING']))
    def get_child_count(self): return len(self.children)
    def get_child_at_index(self, i): return self.children[i]
    def get_process_id(self): return self.pid
    def get_text_iface(self): return self if 'text' in self.spec else None

class Text:
    @staticmethod
    def get_character_count(el): return len(el.spec['text'])
    @staticmethod
    def get_text(el, start, end): return el.spec['text'][start:end]

class Action:
    @staticmethod
    def get_n_actions(el): return len(el.spec.get('actions', ['click']))
    @staticmethod
    def get_action_name(el, i): return el.spec.get('actions', ['click'])[i]
    @staticmethod
    def do_action(el, i):
        with open(os.environ['FAKE_CLICKS'], 'a', encoding='utf-8') as f:
            f.write(json.dumps([el.pid, el.get_name(), Action.get_action_name(el, i)]) + chr(10))
        return True

class Desktop(Node):
    def __init__(self):
        self.spec = {}; self.pid = 0
        self.children = []
        for app in SCENE.get('apps', []):
            node = Node({'role': 'APPLICATION', 'name': app.get('name', 'app')}, app['pid'])
            node.children = [Node(w, app['pid']) for w in app.get('windows', [])]
            self.children.append(node)

def get_desktop(i):
    return Desktop()

Atspi = SimpleNamespace(
    Role=SimpleNamespace(**{r: r for r in ROLES}),
    StateType=SimpleNamespace(VISIBLE='VISIBLE', SHOWING='SHOWING', MODAL='MODAL'),
    Text=Text, Action=Action, get_desktop=get_desktop)

class GError(Exception):
    pass

class Connection:
    def call_sync(self, *args):
        if SCENE.get('bus') == 'no-address':
            raise GError('g-dbus-error-quark: The name org.a11y.Bus was not provided by any .service files (2)')
        return SimpleNamespace(unpack=lambda: ('unix:path=/fake/at-spi/bus',))
    def close_sync(self, cancellable): pass

def bus_get_sync(kind, cancellable):
    if SCENE.get('bus') == 'no-session':
        raise GError('g-io-error-quark: Cannot autolaunch D-Bus without X11 $DISPLAY (0)')
    return Connection()

def new_for_address_sync(address, flags, observer, cancellable):
    if SCENE.get('bus') == 'down':
        raise GError('g-io-error-quark: Could not connect: No such file or directory (1)')
    return Connection()

Gio = SimpleNamespace(
    BusType=SimpleNamespace(SESSION=1), DBusCallFlags=SimpleNamespace(NONE=0),
    DBusConnectionFlags=SimpleNamespace(AUTHENTICATION_CLIENT=1, MESSAGE_BUS_CONNECTION=2),
    DBusConnection=SimpleNamespace(new_for_address_sync=new_for_address_sync), bus_get_sync=bus_get_sync)
GLib = SimpleNamespace(VariantType=SimpleNamespace(new=lambda s: s))
`;

function hasPython(): boolean {
  try {
    execFileSync('python3', ['--version'], { stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

/** The id rule of both the macOS and the Linux scripts. */
function fnv(s: string): string {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return String(h >>> 0);
}

function dialogId(pid: number, title: string, message: string, buttons: string[]): string {
  return fnv([pid, title, message, buttons.join('\u0002')].join('\u0001'));
}

interface FakeNode { role?: string; name?: string; text?: string; states?: string[]; actions?: string[]; children?: FakeNode[] }
interface FakeScene { bus?: 'ok' | 'down' | 'no-address' | 'no-session'; apps?: { pid: number; name?: string; windows: FakeNode[] }[] }

const label = (name: string, extra: FakeNode = {}): FakeNode => ({ role: 'LABEL', name, ...extra });
const button = (name: string, extra: FakeNode = {}): FakeNode => ({ role: 'PUSH_BUTTON', name, children: [label(name)], ...extra });

// Shapes recorded from live dialogs on Ubuntu 26.04 (GNOME, Wayland): zenity 4 (GTK4) and a GTK3 MessageDialog with no
// window title next to its parent window (Unity's DisplayDialog), whose accessible name is its icon's ("Question").
const SCENE: FakeScene = {
  apps: [
    {
      pid: 42,
      windows: [
        { role: 'FRAME', name: 'Unity 6 - Main - Linux', children: [{ role: 'FILLER', children: [button('Minimize'), button('Close')] }] },
        {
          role: 'ALERT', name: 'Question', states: ['VISIBLE', 'SHOWING', 'MODAL'], children: [
            { role: 'FILLER', children: [label('', { states: ['VISIBLE'] })] },
            { role: 'FILLER', children: [
              { role: 'ICON', name: 'Question' },
              { role: 'FILLER', children: [label('Probe Title'), label('Editor blocked by probe')] },
              { role: 'FILLER', children: [button('Cancel'), button('Proceed')] },
            ] },
          ],
        },
        { role: 'DIALOG', name: 'Importing', children: [label('Importing assets')] },
        { role: 'DIALOG', name: 'Hidden', states: ['MODAL'], children: [button('OK')] },
      ],
    },
    {
      pid: 7,
      windows: [
        {
          role: 'DIALOG', name: 'E2E Probe', states: ['VISIBLE', 'SHOWING', 'MODAL'], children: [
            { role: 'PANEL', children: [label('E2E Probe'), { role: 'PANEL', children: [{ role: 'IMAGE' }, label('', { text: 'Blocked by test' })] }] },
            { role: 'PANEL', children: [button('Cancel'), button('Proceed', { actions: ['press', 'click'] })] },
            { role: 'TABLE', children: [label('not walked'), button('Row')] },
          ],
        },
        // A modal window with another role, and a non-ASCII text.
        { role: 'FRAME', name: 'Build', states: ['VISIBLE', 'SHOWING', 'MODAL'], children: [label('Überschreiben?'), button('Ersetzen')] },
      ],
    },
    { pid: 99, windows: [{ role: 'DIALOG', name: 'Other app', children: [button('OK')] }] },
  ],
};

describe.skipIf(!hasPython())('editor dialogs on Linux (real script, fake AT-SPI)', () => {
  let dir = '';
  let clicks = '';

  beforeAll(() => {
    dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-fake-atspi-'));
    fs.mkdirSync(path.join(dir, 'gi', 'repository'), { recursive: true });
    fs.writeFileSync(path.join(dir, 'gi', '__init__.py'), FAKE_GI_INIT);
    fs.writeFileSync(path.join(dir, 'gi', 'repository', '__init__.py'), FAKE_GI_REPOSITORY);
    clicks = path.join(dir, 'clicks.jsonl');
  });

  afterAll(() => fs.rmSync(dir, { recursive: true, force: true }));

  function fakePython(scene: FakeScene, extra: Record<string, string> = {}): ScriptRunner {
    return (script, env = {}) => new Promise((resolve, reject) => {
      const childEnv = { ...process.env, PYTHONPATH: dir, FAKE_ATSPI: JSON.stringify(scene), FAKE_CLICKS: clicks, DISPLAY: ':0', ...extra, ...env };
      execFile('python3', ['-c', script], { env: childEnv }, (err, stdout, stderr) => (err ? reject(new Error(String(stderr))) : resolve(stdout)));
    });
  }

  function readClicks(): unknown[] {
    if (!fs.existsSync(clicks)) return [];
    const lines = fs.readFileSync(clicks, 'utf-8').trim().split('\n').filter(Boolean).map(l => JSON.parse(l) as unknown);
    fs.rmSync(clicks);
    return lines;
  }

  it('lists the dialogs of the given pids: titles, texts and buttons, with the macOS id rule', async () => {
    const listed = await listEditorDialogs([42, 7], 'linux', fakePython(SCENE));
    expect(listed).toEqual({
      kind: 'ok',
      dialogs: [
        { id: dialogId(42, 'Probe Title', 'Editor blocked by probe', ['Cancel', 'Proceed']), pid: 42, title: 'Probe Title', message: 'Editor blocked by probe', buttons: ['Cancel', 'Proceed'] },
        { id: dialogId(7, 'E2E Probe', 'Blocked by test', ['Cancel', 'Proceed']), pid: 7, title: 'E2E Probe', message: 'Blocked by test', buttons: ['Cancel', 'Proceed'] },
        { id: dialogId(7, 'Build', 'Überschreiben?', ['Ersetzen']), pid: 7, title: 'Build', message: 'Überschreiben?', buttons: ['Ersetzen'] },
      ],
    });
  });

  it('prints pure ASCII', async () => {
    const out = await fakePython(SCENE)(LINUX_LIST_DIALOGS_SCRIPT, { [PIDS_ENV]: '7' });
    expect(out).not.toMatch(/[^\x20-\x7e]/);
    expect(out).toContain('\\u00dcberschreiben?');
  });

  it('presses a button case-insensitively, on the dialog with the given id, through its click action', async () => {
    readClicks();
    expect(await pressEditorDialogButton([42, 7], 'PROCEED', dialogId(7, 'E2E Probe', 'Blocked by test', ['Cancel', 'Proceed']), 'linux', fakePython(SCENE)))
      .toEqual({ kind: 'pressed', button: 'PROCEED' });
    expect(await pressEditorDialogButton([42, 7], 'cancel', undefined, 'linux', fakePython(SCENE))).toEqual({ kind: 'pressed', button: 'cancel' });
    expect(readClicks()).toEqual([[7, 'Proceed', 'click'], [42, 'Cancel', 'click']]);
  });

  it('reports not-found for an unknown label, another dialog\'s id, a window that is no dialog or another app', async () => {
    readClicks();
    const run = fakePython(SCENE);
    for (const [pids, label, id] of [[[42], 'Nope', undefined], [[42], 'Proceed', '123'], [[42], 'Close', undefined], [[42], 'OK', undefined], [[42, 7], 'Row', undefined], [[7], 'OK', undefined]] as [number[], string, string | undefined][]) {
      expect((await pressEditorDialogButton(pids, label, id, 'linux', run)).kind).toBe('not-found');
    }
    expect(readClicks()).toEqual([]);
  });

  it('maps a missing AT-SPI binding, an unreachable bus, no session and an empty bus to hints', async () => {
    expect(await listEditorDialogs([42], 'linux', fakePython(SCENE, { FAKE_NO_ATSPI: '1' })))
      .toEqual({ kind: 'error', message: expect.stringMatching(/^Namespace Atspi not available\. .*python3-gi and gir1\.2-atspi-2\.0/) });
    expect(await listEditorDialogs([42], 'linux', fakePython({ ...SCENE, bus: 'down' })))
      .toEqual({ kind: 'error', message: expect.stringMatching(/Could not connect.*install at-spi2-core/) });
    expect(await listEditorDialogs([42], 'linux', fakePython({ ...SCENE, bus: 'no-address' })))
      .toEqual({ kind: 'error', message: expect.stringMatching(/org\.a11y\.Bus.*install at-spi2-core/) });
    expect(await listEditorDialogs([42], 'linux', fakePython({ ...SCENE, bus: 'no-session' }, { DISPLAY: '', WAYLAND_DISPLAY: '' })))
      .toEqual({ kind: 'error', message: expect.stringMatching(/autolaunch.*no graphical session/) });
    expect(await pressEditorDialogButton([42], 'OK', undefined, 'linux', fakePython({ apps: [] })))
      .toEqual({ kind: 'error', message: expect.stringContaining('toolkit-accessibility true') });
    expect(await listEditorDialogs([42], 'linux', fakePython({ apps: [] }, { DISPLAY: '', WAYLAND_DISPLAY: '' })))
      .toEqual({ kind: 'error', message: expect.stringContaining('unreachable or empty') });
  });

  it('reports a refused click action with the (non-ASCII) label intact, whatever the locale', async () => {
    const scene: FakeScene = { apps: [{ pid: 3, windows: [{ role: 'DIALOG', name: 'Frage', children: [button('Übernehmen', { actions: [] })] }] }] };
    expect(await pressEditorDialogButton([3], 'übernehmen', undefined, 'linux', fakePython(scene, { LC_ALL: 'C', PYTHONIOENCODING: 'ascii' })))
      .toEqual({ kind: 'error', message: 'The button "Übernehmen" did not accept the click action.' });
  });

  it('a pid without windows on the bus has no dialogs', async () => {
    expect(await listEditorDialogs([5], 'linux', fakePython(SCENE))).toEqual({ kind: 'ok', dialogs: [] });
  });
});
