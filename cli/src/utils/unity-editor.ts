import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { homedir, platform } from 'os';
import { findUnityHub, ensureUnityHub, listInstalledEditors, type InstalledEditor } from './unity-hub.js';
import { readCachedEditorPath, writeCachedEditorPath } from './editor-cache.js';
import { verbose } from './ui.js';

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
 * Find the Unity Editor binary path for a specific version: the editor-path cache, then the editors on disk (the
 * Hub's editor folders and the editors it lists in `editors-v2.json`, see {@link listEditorsByCommonLocations}), and
 * only when those miss the Unity Hub CLI (asynchronous and time-bounded: it can hang while the Hub app is open).
 * Installs Unity Hub automatically if needed.
 */
export async function findEditorPath(version?: string): Promise<string | null> {
  const startedAt = Date.now();
  verbose(`findEditorPath start (version=${version ?? 'auto'})`);

  // Cache hit short-circuits the Unity Hub Electron probe below
  // (~13s cold -> ~instant warm). Stale entries self-evict.
  const cached = readCachedEditorPath(version);
  if (cached) {
    verbose(`findEditorPath cache hit in ${Date.now() - startedAt}ms`);
    return cached;
  }

  // Fast path: check common install locations first (instant filesystem
  // check). Runs for both explicit-version requests (`findEditorPath('X')`)
  // and the version-less "highest installed" request (`findEditorPath()`).
  // Both branches benefit equally from reading
  // `secondaryInstallPath.json` — the only difference is the candidate list
  // shape (version-specific vs sorted-directory listing). When the fast path
  // hits, it returns without ever spawning the Unity Hub Electron probe
  // (the ~14s cache-miss cost in issue #784).
  const fastStartedAt = Date.now();
  const fastResult = findEditorPathByCommonLocations(version);
  verbose(`findEditorPath fast path completed in ${Date.now() - fastStartedAt}ms (${fastResult ? 'hit' : 'miss'})`);
  if (fastResult) {
    writeCachedEditorPath(version, fastResult);
    verbose(`findEditorPath resolved via common locations in ${Date.now() - startedAt}ms`);
    return fastResult;
  }

  // Slow path: query Unity Hub CLI for installed editors
  const hubStartedAt = Date.now();
  const hubPath = await ensureUnityHub().catch(() => null);
  verbose(`findEditorPath ensureUnityHub completed in ${Date.now() - hubStartedAt}ms (${hubPath ? 'found' : 'unavailable'})`);
  if (!hubPath) {
    const fallbackStartedAt = Date.now();
    const fallback = findEditorPathByCommonLocations(version);
    verbose(`findEditorPath fallback common-location scan completed in ${Date.now() - fallbackStartedAt}ms (${fallback ? 'hit' : 'miss'})`);
    verbose(`findEditorPath completed in ${Date.now() - startedAt}ms`);
    if (fallback) writeCachedEditorPath(version, fallback);
    return fallback;
  }

  const listStartedAt = Date.now();
  const editors = await listInstalledEditors(hubPath);
  verbose(`findEditorPath listInstalledEditors completed in ${Date.now() - listStartedAt}ms (count=${editors.length})`);
  if (editors.length === 0) {
    const fallbackStartedAt = Date.now();
    const fallback = findEditorPathByCommonLocations(version);
    verbose(`findEditorPath empty-editor fallback completed in ${Date.now() - fallbackStartedAt}ms (${fallback ? 'hit' : 'miss'})`);
    verbose(`findEditorPath completed in ${Date.now() - startedAt}ms`);
    if (fallback) writeCachedEditorPath(version, fallback);
    return fallback;
  }

  // A requested version that is not installed is NEVER replaced by another editor: opening a project in a newer
  // editor upgrades it (Unity blocks on a "Project Upgrade Required" dialog nobody answers) and in an older one
  // downgrades it. The caller reports it as not installed, listing the installed editors. Only a version-less request
  // takes the highest installed editor (cached under the auto key).
  if (version) {
    const match = editors.find((e) => e.version === version);
    if (!match) {
      verbose(`findEditorPath: requested ${version} is not installed (installed: ${editors.map((e) => e.version).join(', ')})`);
      return null;
    }
    const resolved = getEditorBinary(match.path);
    writeCachedEditorPath(version, resolved);
    verbose(`findEditorPath matched requested version ${version} in ${Date.now() - startedAt}ms`);
    return resolved;
  }
  const sorted = [...editors].sort((a, b) => compareUnityVersions(b.version, a.version));
  const resolved = getEditorBinary(sorted[0].path);
  writeCachedEditorPath(undefined, resolved);
  verbose(`findEditorPath selected highest installed version ${sorted[0].version} in ${Date.now() - startedAt}ms`);
  return resolved;
}

/**
 * The installed editor to suggest for a project whose version `requested` is missing: the oldest stable (`f`)
 * release newer than it (the smallest upgrade), else the newest stable one, else the newest of any kind. Undefined
 * when nothing is installed.
 */
export function suggestEditorVersion(requested: string, installed: string[]): string | undefined {
  const stable = installed.filter((v) => /\d+\.\d+\.\d+f\d+/.test(v));
  const newerStable = stable.filter((v) => compareUnityVersions(v, requested) > 0).sort(compareUnityVersions);
  if (newerStable.length > 0) return newerStable[0];
  const newestFirst = (list: string[]) => [...list].sort((a, b) => compareUnityVersions(b, a))[0];
  return newestFirst(stable) ?? newestFirst(installed);
}

/**
 * Resolve Unity Hub's `secondaryInstallPath.json` file location for the given
 * platform. Returns null on unsupported platforms or when the required env var
 * (e.g. `APPDATA` on Windows) is missing.
 *
 * Exported for tests; not part of the package's public API.
 */
export function getSecondaryInstallPathFile(os: NodeJS.Platform): string | null {
  switch (os) {
    case 'win32': {
      const appData = process.env['APPDATA'];
      if (!appData) return null;
      return path.join(appData, 'UnityHub', 'secondaryInstallPath.json');
    }
    case 'darwin':
      return path.join(homedir(), 'Library', 'Application Support', 'UnityHub', 'secondaryInstallPath.json');
    case 'linux':
      return path.join(homedir(), '.config', 'UnityHub', 'secondaryInstallPath.json');
    default:
      return null;
  }
}

/**
 * Read Unity Hub's `secondaryInstallPath.json` and return the parsed install
 * root(s). The file is a single JSON-encoded string (per Unity Hub) on every
 * supported platform, e.g. `"C:\\UnityEditor"`. We also tolerate an array of
 * strings defensively in case Unity Hub ever extends the format.
 *
 * Treats every failure mode as "no secondary roots configured":
 *   - file absent
 *   - file empty / whitespace only
 *   - file not valid JSON
 *   - parsed value is not a non-empty string (or array of strings)
 *   - I/O error reading the file
 *
 * In all those cases we return `[]` and the caller silently falls through to
 * today's default-Hub-root scan. This matches the issue's "best-effort" spec
 * and keeps `findEditorPathByCommonLocations` crash-free on misconfigured
 * machines.
 */
export function readSecondaryInstallPaths(os: NodeJS.Platform): string[] {
  const roots = getHubConfigDirs(os).flatMap((dir) => {
    const parsed = readJsonFile(path.join(dir, 'secondaryInstallPath.json'));
    return Array.isArray(parsed) ? parsed : [parsed];
  });
  return [...new Set(roots
    .filter((p): p is string => typeof p === 'string' && p.trim().length > 0)
    .map((p) => p.trim()))];
}

/**
 * Unity Hub's settings folders for `os` (they hold `secondaryInstallPath.json` and `editors-v2.json`): the one of
 * {@link getSecondaryInstallPathFile}, and on Linux also `$XDG_CONFIG_HOME/UnityHub` (a Flatpak or AppImage Hub may use
 * either). Exported for tests.
 */
export function getHubConfigDirs(os: NodeJS.Platform): string[] {
  const file = getSecondaryInstallPathFile(os);
  const dirs = file ? [path.dirname(file)] : [];
  const xdg = process.env['XDG_CONFIG_HOME'];
  if (os === 'linux' && xdg) dirs.push(path.join(xdg, 'UnityHub'));
  return [...new Set(dirs)];
}

/** The parsed JSON of `file`; undefined when it is absent, empty, unreadable or not JSON. */
function readJsonFile(file: string): unknown {
  try {
    const raw = fs.readFileSync(file, 'utf-8');
    return raw.trim() ? JSON.parse(raw) : undefined;
  } catch {
    return undefined;
  }
}

/**
 * The editors Unity Hub lists in `editors-v2.json` (`{ data: [{ version, location: [path] }] }`): among them the ones
 * added with "Locate" that live outside every editor root. Only entries whose executable exists; `path` is the
 * executable. Exported for tests.
 */
export function readHubListedEditors(os: NodeJS.Platform): InstalledEditor[] {
  const editors: InstalledEditor[] = [];
  for (const dir of getHubConfigDirs(os)) {
    const data = (readJsonFile(path.join(dir, 'editors-v2.json')) as { data?: unknown } | undefined)?.data;
    if (!Array.isArray(data)) continue;
    for (const entry of data as unknown[]) {
      const { version, location } = (entry ?? {}) as { version?: unknown; location?: unknown };
      if (typeof version !== 'string' || !version.trim()) continue;
      const binary = (Array.isArray(location) ? location : [location])
        .filter((l): l is string => typeof l === 'string' && l.trim().length > 0)
        .map((l) => resolveEditorPath(l.trim(), os))
        .find((candidate) => fs.existsSync(candidate));
      if (binary) editors.push({ version: version.trim(), path: binary });
    }
  }
  return editors;
}

/**
 * The Unity version a Hub editor folder holds: the folder name, or its version prefix when the Hub added a suffix
 * (`2022.3.4f1-x86_64`, the Intel build next to the Apple silicon one).
 */
function editorFolderVersion(folder: string): string {
  return /^\d+\.\d+\.\d+[abfpx]\d+(?:c\d+)?/.exec(folder)?.[0] ?? folder;
}

/**
 * Build a comparison key for an editor-root path, used to dedupe a
 * `secondaryInstallPath.json` entry against the platform's default root.
 * Returns a normalized form: redundant separators collapsed via
 * `path.normalize`, a single trailing separator stripped, and (on Windows
 * only) lowercased so `c:\…` and `C:\…` compare equal. Posix paths preserve
 * their case because Posix filesystems are case-sensitive. The original
 * string is still used for `existsSync` and verbose logs — only the
 * comparison key is normalized.
 */
function normalizeRootForDedup(root: string, os: NodeJS.Platform): string {
  let normalized = path.normalize(root);
  // Strip a single trailing separator (path.sep is `\\` on win32, `/` on posix).
  if (normalized.length > 1 && normalized.endsWith(path.sep)) {
    normalized = normalized.slice(0, -1);
  }
  return os === 'win32' ? normalized.toLowerCase() : normalized;
}

/**
 * The directories Unity Hub installs editors into ("editor roots") for the
 * current platform, plus the per-platform shape of an editor binary under a
 * root. Roots, in order:
 *   1. The platform's default Unity Hub editor root
 *      (e.g. `%PROGRAMFILES%\Unity\Hub\Editor` on Windows).
 *   2. Every root listed in Unity Hub's `secondaryInstallPath.json`
 *      (e.g. `C:\UnityEditor\` if the user moved their installs), minus
 *      duplicates of a default root.
 * Returns `null` on an unsupported platform.
 */
function getEditorRootLayout(): {
  defaultRoots: string[];
  secondaryRoots: string[];
  buildBinaryPath: (root: string, ver: string) => string;
} | null {
  const os = platform();
  const defaultRoots: string[] = [];
  let buildBinaryPath: (root: string, ver: string) => string;

  switch (os) {
    case 'win32': {
      const programFiles = process.env['PROGRAMFILES'] ?? 'C:\\Program Files';
      defaultRoots.push(path.join(programFiles, 'Unity', 'Hub', 'Editor'));
      buildBinaryPath = (root, ver) => path.join(root, ver, 'Editor', 'Unity.exe');
      break;
    }
    case 'darwin': {
      defaultRoots.push('/Applications/Unity/Hub/Editor');
      buildBinaryPath = (root, ver) => path.join(root, ver, 'Unity.app', 'Contents', 'MacOS', 'Unity');
      break;
    }
    case 'linux': {
      defaultRoots.push('/opt/unity/hub/Editor');
      const home = process.env['HOME'];
      if (home) defaultRoots.push(path.join(home, 'Unity', 'Hub', 'Editor'));
      buildBinaryPath = (root, ver) => path.join(root, ver, 'Editor', 'Unity');
      break;
    }
    default:
      return null;
  }

  // Secondary roots: `secondaryInstallPath.json` (a JSON string or,
  // defensively, an array). Missing/malformed → empty array, no crash.
  // Duplicates of a default root are dropped, compared via
  // `normalizeRootForDedup` so a trailing separator or (on Windows) a
  // drive-letter case difference still dedupes. The original string is kept
  // for `existsSync` and verbose logs.
  const defaultRootKeys = new Set(defaultRoots.map((r) => normalizeRootForDedup(r, os)));
  const secondaryRoots = readSecondaryInstallPaths(os).filter(
    (r) => !defaultRootKeys.has(normalizeRootForDedup(r, os)),
  );
  return { defaultRoots, secondaryRoots, buildBinaryPath };
}

/**
 * List every editor installed under the Unity Hub editor roots (see
 * {@link getEditorRootLayout}) and the editors the Hub lists in
 * `editors-v2.json` (ones added with "Locate", {@link readHubListedEditors}) by
 * reading the filesystem only — no Unity Hub process is started. Each entry is
 * `{ version, path }` with `path` pointing at the editor executable, the same
 * shape `queryInstalledEditors` returns. Order: the default root first, then
 * secondary roots, newest first within a root (a folder named exactly by its
 * version before a suffixed one, `2022.3.4f1-x86_64`), then the Hub-listed
 * editors. The first place that holds a version wins.
 */
export function listEditorsByCommonLocations(): InstalledEditor[] {
  const layout = getEditorRootLayout();
  if (!layout) return [];
  const editors: InstalledEditor[] = [];
  const seen = new Set<string>();
  const add = (editor: InstalledEditor) => {
    if (seen.has(editor.version)) return;
    seen.add(editor.version);
    editors.push(editor);
  };
  const newestFirst = (a: string, b: string) =>
    compareUnityVersions(editorFolderVersion(b), editorFolderVersion(a)) || a.length - b.length;
  for (const root of [...layout.defaultRoots, ...layout.secondaryRoots]) {
    let folders: string[];
    try {
      folders = fs.readdirSync(root);
    } catch {
      continue;
    }
    for (const folder of folders.sort(newestFirst)) {
      const version = editorFolderVersion(folder);
      if (seen.has(version)) continue;
      const binary = layout.buildBinaryPath(root, folder);
      if (fs.existsSync(binary)) add({ version, path: binary });
    }
  }
  readHubListedEditors(platform()).forEach(add);
  return editors;
}

/**
 * The installed editors, from disk first ({@link listEditorsByCommonLocations}); the Unity Hub CLI (`hubPath`) only
 * when the disk has none, or not `version`. Never empty because the Hub failed or timed out while the disk had some.
 */
export async function listInstalledEditorsDiskFirst(hubPath: string | null, version?: string): Promise<InstalledEditor[]> {
  const onDisk = listEditorsByCommonLocations();
  const enough = version ? onDisk.some((editor) => editor.version === version) : onDisk.length > 0;
  if (enough || !hubPath) return onDisk;
  const listed = await listInstalledEditors(hubPath);
  return listed.length > 0 ? listed : onDisk;
}

/**
 * Find editor by checking common installation directories
 * (see {@link getEditorRootLayout}).
 *
 * Reading `secondaryInstallPath.json` keeps cache-miss latency in the
 * sub-100ms range for users whose editors live outside the default Hub root,
 * avoiding the ~14s Unity Hub Electron probe (issue #784).
 */
function findEditorPathByCommonLocations(version?: string): string | null {
  const startedAt = Date.now();
  const layout = getEditorRootLayout();
  if (!layout) {
    verbose(`findEditorPathByCommonLocations unsupported platform ${platform()}`);
    return null;
  }
  const { defaultRoots, secondaryRoots, buildBinaryPath } = layout;
  if (secondaryRoots.length > 0) {
    verbose(`findEditorPathByCommonLocations including ${secondaryRoots.length} secondary root(s): ${secondaryRoots.join(', ')}`);
  }

  // A requested version ONLY matches that version under each root — never
  // "any installed editor". Falling through to another version inside a
  // version-specific lookup is what produced the cache-poisoning failure mode
  // in issue #784 (the fast path silently returned a wrong-version editor and
  // the caller cached it under the requested key). Without a version, the
  // first editor of `listEditorsByCommonLocations` is a legitimate "highest
  // installed" lookup (the default root is scanned before any secondary root).
  let hit: string | null = null;
  if (version) {
    // The exact folder first (no directory listing), then the suffixed folders and the Hub-listed editors.
    hit = [...defaultRoots, ...secondaryRoots]
      .map((root) => buildBinaryPath(root, version))
      .find((candidate) => fs.existsSync(candidate))
      ?? listEditorsByCommonLocations().find((editor) => editor.version === version)?.path
      ?? null;
  } else {
    hit = listEditorsByCommonLocations()[0]?.path ?? null;
  }

  verbose(`findEditorPathByCommonLocations ${hit ? `hit ${hit}` : 'miss'} after ${Date.now() - startedAt}ms`);
  return hit;
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
 * What Unity Hub adds when it opens a project, minus its session arguments (`-hubSessionId`, `-accessToken`): the
 * editor then connects to the Hub (starting it in the background, `--silent`, when it is not running) and takes the
 * Unity account the Hub is signed in to. Without them a signed-out editor stays signed out, and its sign-in only offers
 * "Install Unity Hub". Never a credential: the editor fetches its session from the Hub itself.
 */
export const HUB_LAUNCH_ARGS: readonly string[] = ['-useHub', '-hubIPC'];

/**
 * The editor's command-line arguments for opening `projectPath` in a window (`launchEditor` and an `openProject`
 * `spawnImpl`): through Unity Hub (`HUB_LAUNCH_ARGS`) when the Hub is installed (`hubPath`, found by default).
 */
export function editorLaunchArgs(projectPath: string, hubPath: string | null = findUnityHub()): string[] {
  return ['-projectPath', path.resolve(projectPath), ...(hubPath ? HUB_LAUNCH_ARGS : [])];
}

/** The editor's full environment: `process.env`, then `env` (the MCP variables). */
export function editorLaunchEnv(env?: Record<string, string>): NodeJS.ProcessEnv {
  return { ...process.env, ...env };
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
  const args = editorLaunchArgs(projectPath);

  const child = spawn(editorPath, args, {
    detached: true,
    stdio: 'ignore',
    env: editorLaunchEnv(env),
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
