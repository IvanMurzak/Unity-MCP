import * as fs from 'fs';
import * as path from 'path';
import { execFileSync, spawn } from 'child_process';
import { platform } from 'os';

/**
 * Detect the Unity Hub installation path based on the current platform.
 * Returns the path if found, or null.
 */
export function findUnityHub(): string | null {
  const os = platform();
  const candidates: string[] = [];

  switch (os) {
    case 'win32':
      candidates.push(
        path.join(process.env['PROGRAMFILES'] ?? 'C:\\Program Files', 'Unity Hub', 'Unity Hub.exe'),
        path.join(process.env['LOCALAPPDATA'] ?? '', 'Programs', 'Unity Hub', 'Unity Hub.exe')
      );
      break;
    case 'darwin':
      candidates.push('/Applications/Unity Hub.app/Contents/MacOS/Unity Hub');
      break;
    case 'linux':
      candidates.push(
        '/usr/bin/unity-hub',
        '/snap/bin/unity-hub',
        path.join(process.env['HOME'] ?? '', 'Applications', 'Unity Hub.AppImage')
      );
      break;
  }

  for (const candidate of candidates) {
    if (candidate && fs.existsSync(candidate)) {
      return candidate;
    }
  }

  return null;
}

export interface InstalledEditor {
  version: string;
  path: string;
}

/**
 * List installed Unity editors via Unity Hub CLI.
 */
export function listInstalledEditors(hubPath: string): InstalledEditor[] {
  try {
    const output = execFileSync(hubPath, ['--', '--headless', 'editors', '--installed'], {
      encoding: 'utf-8',
      timeout: 30000,
    });

    const editors: InstalledEditor[] = [];
    for (const line of output.split('\n')) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      // Format: "2022.3.62f3 , installed at /path/to/editor"
      const match = trimmed.match(/^([\d.]+\w*)\s*,?\s*installed at\s+(.+)$/i);
      if (match) {
        editors.push({ version: match[1], path: match[2].trim() });
      }
    }

    return editors;
  } catch (err) {
    console.error('Failed to list installed editors:', (err as Error).message);
    return [];
  }
}

/**
 * Install a Unity editor version via Unity Hub CLI.
 */
export function installEditor(hubPath: string, version: string): void {
  console.log(`Installing Unity Editor ${version} via Unity Hub...`);
  try {
    const output = execFileSync(
      hubPath,
      ['--', '--headless', 'install', '--version', version],
      { encoding: 'utf-8', timeout: 600000, stdio: 'inherit' }
    );
  } catch (err) {
    throw new Error(`Failed to install Unity Editor ${version}: ${(err as Error).message}`);
  }
}

/**
 * Create a new Unity project via Unity Hub CLI.
 */
export function createProject(hubPath: string, projectPath: string, editorVersion?: string): void {
  const args = ['--', '--headless', 'create', '-projectPath', projectPath];
  if (editorVersion) {
    args.push('-version', editorVersion);
  }

  console.log(`Creating Unity project at: ${projectPath}`);
  try {
    execFileSync(hubPath, args, {
      encoding: 'utf-8',
      timeout: 120000,
      stdio: 'inherit',
    });
    console.log('Project created successfully.');
  } catch (err) {
    throw new Error(`Failed to create project: ${(err as Error).message}`);
  }
}
