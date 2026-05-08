import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { platform } from 'os';
import { findUnityHub, ensureUnityHub, listInstalledEditors } from './unity-hub.js';
import * as ui from './ui.js';

/**
 * Compare two Unity version strings with numeric-aware sorting.
 * Parses components like "2022.3.62f3" into [2022, 3, 62, "f", 3].
 * Returns negative if a < b, positive if a > b, 0 if equal.
 */
function compareUnityVersions(a: string, b: string): number {
  const parseVersion = (v: string): (number | string)[] => {
    const parts: (number | string)[] = [];
    for (const segment of v.split(/([.\-])/)) {
      // Split each segment further into numeric and alpha tokens
      const tokens = segment.match(/(\d+|[a-zA-Z]+)/g);
      if (tokens) {
        for (const token of tokens) {
          const num = parseInt(token, 10);
          parts.push(isNaN(num) ? token : num);
        }
      }
    }
    return parts;
  };

  const aParts = parseVersion(a);
  const bParts = parseVersion(b);
  const len = Math.max(aParts.length, bParts.length);

  for (let i = 0; i < len; i++) {
    const ap = aParts[i];
    const bp = bParts[i];
    if (ap === undefined && bp === undefined) return 0;
    if (ap === undefined) return -1;
    if (bp === undefined) return 1;

    if (typeof ap === 'number' && typeof bp === 'number') {
      if (ap !== bp) return ap - bp;
    } else {
      const cmp = String(ap).localeCompare(String(bp));
      if (cmp !== 0) return cmp;
    }
  }
  return 0;
}

/**
 * Read the Unity editor version from a project's ProjectSettings/ProjectVersion.txt.
 */
export function getProjectEditorVersion(projectPath: string): string | null {
  const versionFile = path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt');
  if (!fs.existsSync(versionFile)) {
    return null;
  }

  const content = fs.readFileSync(versionFile, 'utf-8');
  const match = content.match(/m_EditorVersion:\s*(.+)/);
  return match ? match[1].trim() : null;
}

/**
 * Find the Unity Editor binary path for a specific version.
 * Uses Unity Hub to locate installed editors. Installs Unity Hub automatically if needed.
 */
export async function findEditorPath(version?: string): Promise<string | null> {
  // Fast path: if we know the version, check common install locations first (instant filesystem check)
  if (version) {
    const fastResult = findEditorPathByCommonLocations(version);
    if (fastResult) return fastResult;
  }

  // Slow path: query Unity Hub CLI for installed editors
  const hubPath = await ensureUnityHub().catch(() => null);
  if (!hubPath) {
    return findEditorPathByCommonLocations(version);
  }

  const editors = listInstalledEditors(hubPath);
  if (editors.length === 0) {
    return findEditorPathByCommonLocations(version);
  }

  if (version) {
    const match = editors.find((e) => e.version === version);
    if (match) return getEditorBinary(match.path);
  }

  // Return the highest installed editor by version-aware sorting
  const sorted = [...editors].sort((a, b) => compareUnityVersions(b.version, a.version));
  return getEditorBinary(sorted[0].path);
}

/**
 * Find editor by checking common installation directories.
 */
function findEditorPathByCommonLocations(version?: string): string | null {
  const os = platform();
  const candidates: string[] = [];

  switch (os) {
    case 'win32': {
      const programFiles = process.env['PROGRAMFILES'] ?? 'C:\\Program Files';
      if (version) {
        candidates.push(path.join(programFiles, 'Unity', 'Hub', 'Editor', version, 'Editor', 'Unity.exe'));
      }
      // Check for any editor
      const hubEditorDir = path.join(programFiles, 'Unity', 'Hub', 'Editor');
      if (fs.existsSync(hubEditorDir)) {
        try {
          const versions = fs.readdirSync(hubEditorDir).sort((a, b) => compareUnityVersions(b, a));
          for (const v of versions) {
            candidates.push(path.join(hubEditorDir, v, 'Editor', 'Unity.exe'));
          }
        } catch { /* ignore */ }
      }
      break;
    }
    case 'darwin': {
      if (version) {
        candidates.push(`/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity`);
      }
      const hubEditorDir = '/Applications/Unity/Hub/Editor';
      if (fs.existsSync(hubEditorDir)) {
        try {
          const versions = fs.readdirSync(hubEditorDir).sort((a, b) => compareUnityVersions(b, a));
          for (const v of versions) {
            candidates.push(path.join(hubEditorDir, v, 'Unity.app', 'Contents', 'MacOS', 'Unity'));
          }
        } catch { /* ignore */ }
      }
      break;
    }
    case 'linux': {
      const home = process.env['HOME'];
      if (version) {
        candidates.push(`/opt/unity/hub/Editor/${version}/Editor/Unity`);
        if (home) candidates.push(path.join(home, 'Unity', 'Hub', 'Editor', version, 'Editor', 'Unity'));
      }
      const linuxBaseDirs = ['/opt/unity/hub/Editor'];
      if (home) linuxBaseDirs.push(path.join(home, 'Unity', 'Hub', 'Editor'));
      for (const baseDir of linuxBaseDirs) {
        if (fs.existsSync(baseDir)) {
          try {
            const versions = fs.readdirSync(baseDir).sort((a, b) => compareUnityVersions(b, a));
            for (const v of versions) {
              candidates.push(path.join(baseDir, v, 'Editor', 'Unity'));
            }
          } catch { /* ignore */ }
        }
      }
      break;
    }
  }

  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  return null;
}

/**
 * Resolve the Unity binary path from an editor installation directory for a given platform.
 * Exported for testing purposes.
 *
 * Handles cases where the path already points to the executable
 * (e.g. Unity Hub may return ".../Editor/Unity.exe" directly).
 * On macOS, also handles paths that already end with `.app`.
 */
export function resolveEditorPath(editorDir: string, os: string): string {
  const basename = path.basename(editorDir).toLowerCase();

  // If the path already points to the executable, return it as-is
  if (basename === 'unity.exe' || basename === 'unity') {
    return editorDir;
  }

  // Use platform-appropriate path joining: posix for non-Windows, native for Windows
  const join = os === 'win32' ? path.join : path.posix.join;

  switch (os) {
    case 'win32':
      return join(editorDir, 'Editor', 'Unity.exe');
    case 'darwin':
      // If path already ends with .app (e.g. Unity Hub returns ".../Unity.app"),
      // go directly into Contents/MacOS/Unity instead of appending another Unity.app
      if (editorDir.endsWith('.app')) {
        return join(editorDir, 'Contents', 'MacOS', 'Unity');
      }
      return join(editorDir, 'Unity.app', 'Contents', 'MacOS', 'Unity');
    default:
      return join(editorDir, 'Editor', 'Unity');
  }
}

/**
 * Get the Unity binary path from an editor installation directory.
 * Handles cases where the path already points to the executable
 * (e.g. Unity Hub may return ".../Editor/Unity.exe" directly).
 */
function getEditorBinary(editorDir: string): string {
  return resolveEditorPath(editorDir, platform());
}

export interface LaunchEditorCallbacks {
  /** Fired once the OS reports the child process has spawned. */
  onSpawn?: (pid: number | undefined) => void;
  /** Fired if the spawn itself fails (binary missing, permission denied, …). */
  onError?: (err: Error) => void;
}

/**
 * Spawn the Unity Editor binary with the given project path. Returns
 * the spawned `ChildProcess` so callers can await its `spawn` /
 * `error` events themselves; library callers pass `onSpawn`/`onError`
 * to avoid plumbing event listeners through their own code.
 *
 * Library-safe (does NOT call `process.exit`, does NOT print to
 * stdout/stderr — observability is the caller's responsibility via
 * the optional callbacks). The CLI's `commands/open.ts` wires those
 * callbacks back into `ui.success` / `ui.error` so the terminal
 * experience stays identical.
 */
export function launchEditor(
  editorPath: string,
  projectPath: string,
  env?: Record<string, string>,
  callbacks?: LaunchEditorCallbacks,
): import('child_process').ChildProcess {
  const args = ['-projectPath', path.resolve(projectPath)];

  const child = spawn(editorPath, args, {
    detached: true,
    stdio: 'ignore',
    env: { ...process.env, ...env },
  });

  child.on('spawn', () => {
    callbacks?.onSpawn?.(child.pid);
  });

  child.on('error', (err) => {
    callbacks?.onError?.(err);
  });

  child.unref();
  return child;
}

/**
 * Print actionable help when a required Unity Editor version is not found.
 * Lists installed editors and suggests install or override commands.
 */
export function printEditorNotFoundHelp(requestedVersion: string | undefined, commandName: string): void {
  if (requestedVersion) {
    ui.error(`Unity Editor ${requestedVersion} is not installed.`);
  } else {
    ui.error('No Unity Editor found.');
  }

  ui.heading('Options:');

  if (requestedVersion) {
    ui.info(`Install it:  npx unity-mcp-cli install-unity ${requestedVersion}`);
  }
  ui.info('Install latest stable:  npx unity-mcp-cli install-unity');

  // Show installed editors as alternatives
  const hubPath = findUnityHub();
  if (hubPath) {
    const editors = listInstalledEditors(hubPath);
    if (editors.length > 0) {
      ui.heading('Installed editors:');
      for (const editor of editors) {
        ui.label(editor.version, editor.path);
      }
      if (requestedVersion) {
        const hint = commandName === 'connect'
          ? `npx unity-mcp-cli ${commandName} --unity ${editors[0].version} --path <path> --url <url>`
          : `npx unity-mcp-cli ${commandName} <path> --unity ${editors[0].version}`;
        ui.info(`Use a different version:  ${hint}`);
      }
    }
  }
}
