import { describe, it, expect } from 'vitest';
import {
  BUTTON_ENV,
  ID_ENV,
  MAC_LIST_DIALOGS_SCRIPT,
  MAC_PRESS_DIALOG_SCRIPT,
  PIDS_ENV,
  listEditorDialogs,
  macPermissionMessage,
  pressEditorDialogButton,
  type ScriptRunner,
} from '../src/utils/editor-dialogs.js';

// A minimal stand-in for the JXA System Events object model the macOS scripts use, so the real script text runs here.
interface FakeElement { name: string; subrole?: string; modal?: boolean; texts?: string[]; buttons?: string[]; sheets?: FakeElement[] }

function fakeSystemEvents(processes: Record<number, FakeElement[]>, clicks: string[]) {
  const element = (e: FakeElement) => {
    const buttons: Record<string | number, unknown> & { name: () => (string | null)[] } = { name: () => [null, ...(e.buttons ?? [])] };
    (e.buttons ?? []).forEach((b, i) => { buttons[i + 1] = { click: () => clicks.push(`${e.name}/${b}`) }; });
    buttons[0] = { click: () => clicks.push(`${e.name}/<close>`) };
    return {
      name: () => e.name,
      attributes: { byName: (n: string) => ({ value: () => (n === 'AXSubrole' ? e.subrole ?? 'AXStandardWindow' : n === 'AXModal' ? !!e.modal : null) }) },
      buttons,
      staticTexts: { value: () => e.texts ?? [], name: () => [] },
      sheets: () => (e.sheets ?? []).map(element),
    };
  };
  return {
    processes: {
      whose: ({ unixId }: { unixId: number }) => () => (processes[unixId] ? [{ windows: () => processes[unixId].map(element) }] : []),
    },
  };
}

/** Runs a macOS script in-process against fake System Events; its only inputs are the environment variables. */
function fakeOsa(processes: Record<number, FakeElement[]>, clicks: string[] = [], seen: { script?: string; env?: Record<string, string> } = {}): ScriptRunner {
  return async (script, env = {}) => {
    seen.script = script;
    seen.env = env;
    const $ = { NSProcessInfo: { processInfo: { environment: { objectForKey: (k: string) => ({ isNil: () => env[k] === undefined, js: env[k] }) } } } };
    const fn = new Function('ObjC', '$', 'Application', `${script}\nreturn run();`);
    return String(fn({ import: () => undefined }, $, () => fakeSystemEvents(processes, clicks)));
  };
}

const UNITY_WINDOWS: Record<number, FakeElement[]> = {
  42: [
    { name: 'Unity 6 - Main - macOS', texts: ['Hierarchy'], buttons: ['Play'] },
    { name: 'Hold On', subrole: 'AXDialog', texts: ['Save changes?', 'Your changes will be lost.'], buttons: ['Save', "Don't Save", 'Cancel'] },
    { name: 'Inspector', subrole: 'AXFloatingWindow', buttons: ['Lock'] },
    { name: 'Progress', subrole: 'AXDialog', texts: ['Importing'], buttons: [] },
    { name: 'Build', sheets: [{ name: '', texts: ['Überschreiben?'], buttons: ['Replace'] }] },
  ],
};

describe('editor dialogs on macOS', () => {
  it('lists dialog windows and sheets with buttons (not the editor, floating or progress windows)', async () => {
    const seen: { script?: string; env?: Record<string, string> } = {};
    const listed = await listEditorDialogs([42, 0, 7], 'darwin', fakeOsa(UNITY_WINDOWS, [], seen));
    expect(seen.script).toBe(MAC_LIST_DIALOGS_SCRIPT);
    expect(seen.env).toEqual({ [PIDS_ENV]: '42,7' });
    expect(listed.kind).toBe('ok');
    if (listed.kind !== 'ok') return;
    expect(listed.dialogs.map(({ id: _id, ...d }) => d)).toEqual([
      { pid: 42, title: 'Hold On', message: 'Save changes?\nYour changes will be lost.', buttons: ['Save', "Don't Save", 'Cancel'] },
      { pid: 42, title: '', message: 'Überschreiben?', buttons: ['Replace'] },
    ]);
    for (const d of listed.dialogs) expect(d.id).toMatch(/^\d+$/);
    expect(new Set(listed.dialogs.map(d => d.id)).size).toBe(2);
  });

  it('keeps an id stable when the window order changes', async () => {
    const reordered = { 42: [...UNITY_WINDOWS[42]].reverse() };
    const a = await listEditorDialogs([42], 'darwin', fakeOsa(UNITY_WINDOWS));
    const b = await listEditorDialogs([42], 'darwin', fakeOsa(reordered));
    const ids = (r: typeof a) => (r.kind === 'ok' ? r.dialogs.map(d => d.id).sort() : []);
    expect(ids(a)).toEqual(ids(b));
  });

  it('escapes non-ASCII text so the output is pure ASCII', async () => {
    const out = await fakeOsa(UNITY_WINDOWS)(MAC_LIST_DIALOGS_SCRIPT, { [PIDS_ENV]: '42' });
    expect(out).not.toMatch(/[^\x20-\x7e]/);
    expect(out).toContain('\\u00dcberschreiben?');
  });

  it('presses a button case-insensitively, on the dialog with the given id', async () => {
    const clicks: string[] = [];
    const listed = await listEditorDialogs([42], 'darwin', fakeOsa(UNITY_WINDOWS));
    const sheet = listed.kind === 'ok' ? listed.dialogs[1] : undefined;
    expect(await pressEditorDialogButton([42], "don't save", undefined, 'darwin', fakeOsa(UNITY_WINDOWS, clicks)))
      .toEqual({ kind: 'pressed', button: "don't save" });
    expect(await pressEditorDialogButton([42], 'REPLACE', sheet?.id, 'darwin', fakeOsa(UNITY_WINDOWS, clicks)))
      .toEqual({ kind: 'pressed', button: 'REPLACE' });
    expect(clicks).toEqual(["Hold On/Don't Save", '/Replace']);
  });

  it('reports not-found for an unknown label or an id of another dialog, without clicking', async () => {
    const clicks: string[] = [];
    expect((await pressEditorDialogButton([42], 'Play', undefined, 'darwin', fakeOsa(UNITY_WINDOWS, clicks))).kind).toBe('not-found');
    expect((await pressEditorDialogButton([42], 'Save', '123', 'darwin', fakeOsa(UNITY_WINDOWS, clicks))).kind).toBe('not-found');
    expect(clicks).toEqual([]);
  });

  it('passes the label and id through the environment; the script text is constant', async () => {
    const label = "Don’t \" + $.system('rm -rf ~') + ' `x` ${y}";
    const seen: { script?: string; env?: Record<string, string> } = {};
    await pressEditorDialogButton([42], label, '987', 'darwin', fakeOsa(UNITY_WINDOWS, [], seen));
    expect(seen.script).toBe(MAC_PRESS_DIALOG_SCRIPT);
    expect(seen.script).not.toContain('rm -rf');
    expect(seen.script).not.toContain('987');
    expect(seen.env).toEqual({ [PIDS_ENV]: '42', [ID_ENV]: '987', [BUTTON_ENV]: label });
    expect(await pressEditorDialogButton([42], 'OK', '12; rm', 'darwin', fakeOsa(UNITY_WINDOWS))).toMatchObject({ kind: 'error' });
  });

  it('maps a denied Accessibility permission to the Settings hint', async () => {
    const denied = async () => 'error:osascript is not allowed assistive access. (-25211)';
    for (const result of [await listEditorDialogs([42], 'darwin', denied), await pressEditorDialogButton([42], 'OK', undefined, 'darwin', denied)]) {
      expect(result.kind).toBe('error');
      if (result.kind !== 'error') continue;
      expect(result.message).toContain('-25211');
      expect(result.message).toContain('Privacy & Security → Accessibility');
    }
    // osascript itself failing (AppleScript form) is mapped too.
    const thrown = await listEditorDialogs([42], 'darwin', async () => { throw new Error('System Events got an error: Access not allowed. (-1719)'); });
    expect(thrown).toMatchObject({ kind: 'error', message: expect.stringContaining('Privacy & Security → Accessibility') });
    expect(macPermissionMessage('execution error: Not authorized to send Apple events to System Events. (-1743)')).toContain('Automation');
    expect(macPermissionMessage('boom')).toBe('boom');
  });

  it('a process without windows (or gone) has no dialogs', async () => {
    expect(await listEditorDialogs([5], 'darwin', fakeOsa(UNITY_WINDOWS))).toEqual({ kind: 'ok', dialogs: [] });
  });
});
