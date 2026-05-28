import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { homedir, platform } from 'os';
import { findUnityHub, ensureUnityHub, listInstalledEditors } from './unity-hub.js';
import { readCachedEditorPath, writeCachedEditorPath } from './editor-cache.js';
import * as ui from './ui.js';
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
 * Find the Unity Editor binary path for a specific version.
 * Uses Unity Hub to locate installed editors. Installs Unity Hub automatically if needed.
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
  const editors = listInstalledEditors(hubPath);
  verbose(`findEditorPath listInstalledEditors completed in ${Date.now() - listStartedAt}ms (count=${editors.length})`);
  if (editors.length === 0) {
    const fallbackStartedAt = Date.now();
    const fallback = findEditorPathByCommonLocations(version);
    verbose(`findEditorPath empty-editor fallback completed in ${Date.now() - fallbackStartedAt}ms (${fallback ? 'hit' : 'miss'})`);
    verbose(`findEditorPath completed in ${Date.now() - startedAt}ms`);
    if (fallback) writeCachedEditorPath(version, fallback);
    return fallback;
  }

  if (version) {
    const match = editors.find((e) => e.version === version);
    if (match) {
      const resolved = getEditorBinary(match.path);
      writeCachedEditorPath(version, resolved);
      verbose(`findEditorPath matched requested version ${version} in ${Date.now() - startedAt}ms`);
      return resolved;
    }
  }

  // Return the highest installed editor by version-aware sorting.
  // Cache discipline: only write under the requested key when the
  // resolved editor's version actually matches the request. Otherwise
  // (e.g. the user asked for `999.0.0f0` which is not installed)
  // we still return the highest installed editor as a best-effort
  // fallback, but DO NOT cache it — caching would silently return the
  // wrong editor for that version forever after (issue #784). The
  // version-less `__auto__` slot is always safe to cache.
  const sorted = [...editors].sort((a, b) => compareUnityVersions(b.version, a.version));
  const resolved = getEditorBinary(sorted[0].path);
  if (version === undefined || sorted[0].version === version) {
    writeCachedEditorPath(version, resolved);
  } else {
    verbose(`findEditorPath skipping cache write: requested ${version} did not match resolved ${sorted[0].version}`);
  }
  verbose(`findEditorPath selected highest installed version ${sorted[0].version} in ${Date.now() - startedAt}ms`);
  return resolved;
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
  const file = getSecondaryInstallPathFile(os);
  if (!file) return [];
  if (!fs.existsSync(file)) return [];

  let raw: string;
  try {
    raw = fs.readFileSync(file, 'utf-8');
  } catch {
    return [];
  }
  if (!raw.trim()) return [];

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return [];
  }

  if (typeof parsed === 'string') {
    const trimmed = parsed.trim();
    return trimmed ? [trimmed] : [];
  }
  if (Array.isArray(parsed)) {
    return parsed
      .filter((p): p is string => typeof p === 'string' && p.trim().length > 0)
      .map((p) => p.trim());
  }
  return [];
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
 * Find editor by checking common installation directories.
 *
 * Scans, in order:
 *   1. The platform's default Unity Hub editor root
 *      (e.g. `%PROGRAMFILES%\Unity\Hub\Editor` on Windows).
 *   2. Every root listed in Unity Hub's `secondaryInstallPath.json`
 *      (e.g. `C:\UnityEditor\` if the user moved their installs).
 *
 * Reading `secondaryInstallPath.json` keeps cache-miss latency in the
 * sub-100ms range for users whose editors live outside the default Hub root,
 * avoiding the ~14s Unity Hub Electron probe (issue #784).
 */
function findEditorPathByCommonLocations(version?: string): string | null {
  const startedAt = Date.now();
  const os = platform();
  const candidates: string[] = [];

  // Layout per platform: a `defaultRoots` list of "Hub editor root"
  // directories (e.g. `<programFiles>/Unity/Hub/Editor`) and a per-platform
  // builder that turns "(root, version)" into the binary path under it.
  // We append the same shapes for every root, default + secondary, so the
  // candidate list is uniform regardless of where the install lives.
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
      verbose(`findEditorPathByCommonLocations unsupported platform ${os} after ${Date.now() - startedAt}ms`);
      return null;
  }

  // Secondary roots: read `secondaryInstallPath.json`. On every supported
  // platform Unity Hub stores them as a single JSON string (or, defensively,
  // an array of strings). Missing/malformed → empty array, no crash.
  const secondaryRootsBeforeFiltering = readSecondaryInstallPaths(os);
  // Drop duplicates that already appear in `defaultRoots` so the candidate
  // list doesn't double-stat them. Compare via `normalizeRootForDedup` so a
  // user-typed trailing separator (e.g. `C:\Program Files\Unity\Hub\Editor\`)
  // or, on Windows, a drive-letter case difference (`c:\...` vs `C:\...`)
  // still dedupes against the default root. The original string is kept for
  // downstream `existsSync` and verbose logs — only the comparison key is
  // normalized.
  const defaultRootKeys = new Set(defaultRoots.map((r) => normalizeRootForDedup(r, os)));
  const secondaryRoots = secondaryRootsBeforeFiltering.filter(
    (r) => !defaultRootKeys.has(normalizeRootForDedup(r, os)),
  );
  if (secondaryRoots.length > 0) {
    verbose(`findEditorPathByCommonLocations including ${secondaryRoots.length} secondary root(s): ${secondaryRoots.join(', ')}`);
  }

  // Build the candidate list. When the caller requested a specific version we
  // ONLY emit the version-specific candidate under each root — never the
  // sorted-directory fallback. Falling through to "any installed editor"
  // inside a version-specific lookup is what produced the cache-poisoning
  // failure mode in issue #784 (the fast path silently returned a wrong-
  // version editor and the caller cached it under the requested key). When
  // the caller did NOT supply a version, we DO emit the sorted directory
  // listing — that's a legitimate "highest installed" lookup. The default
  // root is scanned before any secondary root, preserving the previous
  // behaviour for users without secondary roots.
  const allRoots = [...defaultRoots, ...secondaryRoots];
  const rootIsSecondary = new Map<string, boolean>(
    allRoots.map((r) => [r, secondaryRoots.includes(r)] as const),
  );

  for (const root of allRoots) {
    if (version) {
      candidates.push(buildBinaryPath(root, version));
      continue;
    }
    if (fs.existsSync(root)) {
      try {
        const versions = fs.readdirSync(root).sort((a, b) => compareUnityVersions(b, a));
        for (const v of versions) {
          candidates.push(buildBinaryPath(root, v));
        }
      } catch { /* ignore */ }
    }
  }

  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      // Identify which root the hit came from so verbose logs distinguish a
      // default-root hit from a `secondaryInstallPath` hit — useful when
      // debugging users who report slow opens. Candidates are always built via
      // `buildBinaryPath(root, ver)` which joins extra segments under `root`,
      // so `startsWith(root + path.sep)` is the only reachable case.
      const fromRoot = allRoots.find((root) => candidate.startsWith(root + path.sep));
      const origin = fromRoot && rootIsSecondary.get(fromRoot) ? 'secondaryInstallPath' : 'default Hub root';
      verbose(`findEditorPathByCommonLocations hit ${candidate} (${origin}) after ${Date.now() - startedAt}ms (${candidates.length} candidates)`);
      return candidate;
    }
  }

  verbose(`findEditorPathByCommonLocations miss after ${Date.now() - startedAt}ms (${candidates.length} candidates)`);
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
