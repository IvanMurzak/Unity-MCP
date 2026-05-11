import { homedir } from 'os';
import { join } from 'path';
import { existsSync, readFileSync, writeFileSync } from 'fs';
import { verbose } from './ui.js';

const CACHE_FILE = join(homedir(), '.unity-mcp-cli-editor-cache.json');

// Storage key for the version-less "highest installed" lookup.
// Underscored to stay out of the Unity-version namespace (e.g. `6000.3.1f1`).
const AUTO_KEY = '__auto__';

interface CacheEntry {
  path: string;
  savedAt: number;
}

interface EditorCache {
  [versionKey: string]: CacheEntry;
}

function keyFor(version: string | undefined): string {
  return version ?? AUTO_KEY;
}

function readAll(): EditorCache {
  try {
    const raw = readFileSync(CACHE_FILE, 'utf-8');
    const parsed = JSON.parse(raw) as unknown;
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      const safe: EditorCache = Object.create(null) as EditorCache;
      for (const k of Object.keys(parsed as object)) {
        safe[k] = (parsed as EditorCache)[k];
      }
      return safe;
    }
  } catch {
    // Missing or corrupt — treat as empty cache.
  }
  return Object.create(null) as EditorCache;
}

function writeAll(cache: EditorCache): void {
  try {
    writeFileSync(CACHE_FILE, JSON.stringify(cache, null, 2), 'utf-8');
  } catch {
    // Best-effort: a read-only home dir or full disk should never break `open`.
  }
}

/**
 * Look up a previously-resolved editor binary path for the given
 * Unity version (or `undefined` to look up the "auto / highest" slot).
 * Returns null when no entry exists OR when the cached path no longer
 * exists on disk — in the latter case the stale entry is dropped
 * automatically so subsequent reads don't waste a stat call.
 */
export function readCachedEditorPath(version: string | undefined): string | null {
  const cache = readAll();
  const k = keyFor(version);
  const entry = cache[k];
  if (!entry || typeof entry.path !== 'string') return null;
  if (!existsSync(entry.path)) {
    verbose(`editor-cache: stale entry for ${k} -> ${entry.path} (file missing), evicting`);
    delete cache[k];
    writeAll(cache);
    return null;
  }
  verbose(`editor-cache: hit ${k} -> ${entry.path}`);
  return entry.path;
}

/** Save the resolved editor path under the given version key. */
export function writeCachedEditorPath(version: string | undefined, editorPath: string): void {
  const cache = readAll();
  const k = keyFor(version);
  cache[k] = { path: editorPath, savedAt: Date.now() };
  writeAll(cache);
  verbose(`editor-cache: stored ${k} -> ${editorPath}`);
}

/**
 * Drop the cache entry for the given version. Called when a cached
 * path turned out to be unusable (e.g. spawn failed) so the next
 * invocation re-runs the full resolution.
 */
export function clearCachedEditorPath(version: string | undefined): void {
  const cache = readAll();
  const k = keyFor(version);
  if (cache[k] === undefined) return;
  delete cache[k];
  writeAll(cache);
  verbose(`editor-cache: cleared ${k}`);
}
