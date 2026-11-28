import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { execFileSync } from 'child_process';
import { fileURLToPath } from 'url';
import { isProcessAlive, killProcessTree } from '@baizor/gamedev-cli-core';
import { runEditorCreateProject } from '../src/utils/unity-hub.js';

const compiler = path.join(process.env.WINDIR ?? 'C:\\Windows', 'Microsoft.NET', 'Framework64', 'v4.0.30319', 'csc.exe');
const supported = process.platform !== 'win32' || fs.existsSync(compiler);

describe.skipIf(!supported)('project creation process lifecycle', () => {
  let root: string;
  let editor: string;
  const projects: string[] = [];
  beforeAll(() => {
    root = fs.mkdtempSync(path.join(os.tmpdir(), 'create-project-process-'));
    const script = fileURLToPath(new URL('./helpers/create-project-editor.mjs', import.meta.url));
    if (process.platform === 'win32') {
      // execFile/spawn cannot launch a .cmd without a shell. Compile a tiny
      // native launcher so the Windows test exercises a real executable tree.
      editor = path.join(root, 'editor.exe');
      const source = path.join(root, 'Launcher.cs');
      fs.writeFileSync(source, String.raw`using System;
using System.Diagnostics;
using System.Linq;
class Launcher {
  static string Quote(string value) { return "\"" + value.Replace("\"", "\\\"") + "\""; }
  static int Main(string[] args) {
    var command = Quote(${JSON.stringify(script)}) + " " + String.Join(" ", args.Select(Quote));
    var info = new ProcessStartInfo(${JSON.stringify(process.execPath)}, command);
    info.UseShellExecute = false;
    info.CreateNoWindow = true;
    info.RedirectStandardOutput = true;
    info.RedirectStandardError = true;
    var child = Process.Start(info);
    child.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.Out.WriteLine(e.Data); };
    child.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
    child.BeginOutputReadLine();
    child.BeginErrorReadLine();
    child.WaitForExit();
    return child.ExitCode;
  }
}`);
      execFileSync(compiler, ['/nologo', '/target:exe', `/out:${editor}`, source], { windowsHide: true, timeout: 15_000 });
    } else {
      editor = path.join(root, 'editor.sh');
      const quote = (value: string) => "'" + value.replace(/'/g, "'\\''") + "'";
      fs.writeFileSync(editor, `#!/bin/sh\nexec ${quote(process.execPath)} ${quote(script)} "$@"\n`, { mode: 0o755 });
    }
  }, 20_000);
  afterEach(async () => {
    for (const project of projects.splice(0)) {
      for (const name of ['parent.pid', 'worker.pid']) {
        const file = path.join(project, name);
        if (fs.existsSync(file)) await killProcessTree(Number(fs.readFileSync(file, 'utf8')));
      }
    }
  });
  afterAll(() => { fs.rmSync(root, { recursive: true, force: true }); });
  function project(mode: string) {
    const dir = fs.mkdtempSync(path.join(root, 'project with spaces-'));
    projects.push(dir);
    fs.writeFileSync(path.join(dir, 'mode'), mode);
    return dir;
  }

  it('passes the editor arguments and tolerates more than 1 MiB of stdout', async () => {
    const dir = project('success');
    const listeners = process.listenerCount('exit');
    await expect(runEditorCreateProject(editor, dir, 10_000)).resolves.toBeUndefined();
    expect(JSON.parse(fs.readFileSync(path.join(dir, 'args.json'), 'utf8')))
      .toEqual(['-createProject', dir, '-quit', '-batchmode']);
    expect(process.listenerCount('exit')).toBe(listeners);
  }, 15_000);

  it('keeps the editor diagnostic when creation exits unsuccessfully', async () => {
    await expect(runEditorCreateProject(editor, project('failure'), 10_000))
      .rejects.toThrow(/Failed to create project:[\s\S]*fixture editor diagnostic/);
  }, 15_000);

  it('stops descendant work before returning a timeout', async () => {
    const dir = project('hang');
    const listeners = process.listenerCount('exit');
    // Attach the rejection handler immediately while waiting for the worker to start.
    const result = runEditorCreateProject(editor, dir, 3_000).catch(error => error as Error);
    await vi.waitFor(() => expect(fs.existsSync(path.join(dir, 'heartbeat'))).toBe(true), { timeout: 2_500 });
    expect(await result).toMatchObject({ message: 'Failed to create project: timed out after 3000ms' });
    const heartbeat = fs.readFileSync(path.join(dir, 'heartbeat'), 'utf8');
    await new Promise(resolve => setTimeout(resolve, 250));
    expect(fs.readFileSync(path.join(dir, 'heartbeat'), 'utf8')).toBe(heartbeat);
    // Windows has no zombie PIDs; POSIX's unreaped zombies may still answer kill(0).
    if (process.platform === 'win32') {
      expect(isProcessAlive(Number(fs.readFileSync(path.join(dir, 'worker.pid'), 'utf8')))).toBe(false);
    }
    expect(process.listenerCount('exit')).toBe(listeners);
  }, 15_000);
});
