import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { platform } from 'os';
import { findUnityHub, listInstalledEditors } from './unity-hub.js';

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

  // Return the first (usually latest) editor
  return getEditorBinary(editors[0].path);
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
          const versions = fs.readdirSync(hubEditorDir).sort().reverse();
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
          const versions = fs.readdirSync(hubEditorDir).sort().reverse();
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
            const versions = fs.readdirSync(baseDir).sort().reverse();
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
