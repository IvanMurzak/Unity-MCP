import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { platform } from 'os';
import { findUnityHub, listInstalledEditors } from './unity-hub.js';

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
 * Uses Unity Hub to locate installed editors.
 */
export function findEditorPath(version?: string): string | null {
  const hubPath = findUnityHub();
  if (!hubPath) {
    // Try common default locations
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

  // Return the latest editor by version-aware sorting
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
      if (version) {
        candidates.push(`/opt/unity/hub/Editor/${version}/Editor/Unity`);
        candidates.push(path.join(process.env['HOME'] ?? '', 'Unity', 'Hub', 'Editor', version, 'Editor', 'Unity'));
      }
      for (const baseDir of ['/opt/unity/hub/Editor', path.join(process.env['HOME'] ?? '', 'Unity', 'Hub', 'Editor')]) {
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
 * Get the Unity binary path from an editor installation directory.
 */
function getEditorBinary(editorDir: string): string {
  const os = platform();
  switch (os) {
    case 'win32':
      return path.join(editorDir, 'Editor', 'Unity.exe');
    case 'darwin':
      return path.join(editorDir, 'Unity.app', 'Contents', 'MacOS', 'Unity');
    default:
      return path.join(editorDir, 'Editor', 'Unity');
  }
}

/**
 * Launch Unity Editor with the given project path.
 * Spawns a detached process and returns immediately.
 */
export function launchEditor(
  editorPath: string,
  projectPath: string,
  env?: Record<string, string>
): void {
  const args = ['-projectPath', path.resolve(projectPath)];

  const child = spawn(editorPath, args, {
    detached: true,
    stdio: 'ignore',
    env: { ...process.env, ...env },
  });

  child.unref();
  console.log(`Launched Unity Editor (PID: ${child.pid})`);
}
