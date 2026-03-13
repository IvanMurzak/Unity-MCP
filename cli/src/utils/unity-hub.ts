import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { execFileSync, execSync } from 'child_process';
import { platform } from 'os';
import { get as httpsGet } from 'https';
import { get as httpGet, IncomingMessage } from 'http';

const UNITY_HUB_DOWNLOAD_URLS: Record<string, string> = {
  win32: 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe',
  darwin: 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.dmg',
  linux: 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage',
};

/**
 * Detect the Unity Hub installation path based on the current platform.
 * Returns the path if found, or null.
 */
export function findUnityHub(): string | null {
  const plat = platform();
  const candidates: string[] = [];

  switch (plat) {
    case 'win32':
      candidates.push(
        path.join(process.env['PROGRAMFILES'] ?? 'C:\\Program Files', 'Unity Hub', 'Unity Hub.exe'),
        ...(process.env['LOCALAPPDATA'] ? [path.join(process.env['LOCALAPPDATA'], 'Programs', 'Unity Hub', 'Unity Hub.exe')] : [])
      );
      break;
    case 'darwin':
      candidates.push('/Applications/Unity Hub.app/Contents/MacOS/Unity Hub');
      break;
    case 'linux':
      candidates.push(
        '/usr/bin/unity-hub',
        '/snap/bin/unity-hub',
        ...(process.env['HOME'] ? [path.join(process.env['HOME'], 'Applications', 'Unity Hub.AppImage')] : [])
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

/**
 * Download a file from a URL, following redirects.
 */
function downloadFile(url: string, destPath: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const doGet = url.startsWith('https') ? httpsGet : httpGet;
    doGet(url, (response: IncomingMessage) => {
      // Follow redirects
      if (response.statusCode && response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
        downloadFile(new URL(response.headers.location, url).toString(), destPath).then(resolve, reject);
        return;
      }
      if (response.statusCode && response.statusCode !== 200) {
        reject(new Error(`Download failed with status ${response.statusCode}`));
        return;
      }
      const file = fs.createWriteStream(destPath);
      response.pipe(file);
      file.on('finish', () => { file.close(); resolve(); });
      file.on('error', reject);
    }).on('error', reject);
  });
}

/**
 * Download and install Unity Hub silently.
 * Supports Windows, macOS, and Linux.
 */
export async function installUnityHub(): Promise<string> {
  const plat = platform();
  const url = UNITY_HUB_DOWNLOAD_URLS[plat];
  if (!url) {
    throw new Error(`Unsupported platform for Unity Hub installation: ${plat}`);
  }

  const tmpDir = os.tmpdir();

  switch (plat) {
    case 'win32': {
      const installerPath = path.join(tmpDir, 'UnityHubSetup.exe');
      console.log('Downloading Unity Hub installer...');
      await downloadFile(url, installerPath);
      console.log('Installing Unity Hub silently (may require administrator privileges)...');
      try {
        execFileSync(installerPath, ['/S'], { timeout: 300000, stdio: 'inherit' });
      } finally {
        try { fs.unlinkSync(installerPath); } catch { /* ignore */ }
      }
      const hubPath = findUnityHub();
      if (!hubPath) {
        throw new Error('Unity Hub was installed but could not be found. Try restarting your terminal.');
      }
      return hubPath;
    }
    case 'darwin': {
      const dmgPath = path.join(tmpDir, 'UnityHubSetup.dmg');
      console.log('Downloading Unity Hub installer...');
      await downloadFile(url, dmgPath);
      console.log('Installing Unity Hub...');
      try {
        const mountOutput = execSync(`hdiutil attach -nobrowse -noverify "${dmgPath}"`, { encoding: 'utf-8' });
        const mountMatch = mountOutput.match(/\/Volumes\/.+/);
        if (!mountMatch) throw new Error('Failed to mount Unity Hub DMG');
        const mountPoint = mountMatch[0].trim();
        try {
          execSync(`cp -R "${mountPoint}/Unity Hub.app" /Applications/`, { stdio: 'inherit' });
        } finally {
          execSync(`hdiutil detach "${mountPoint}" -quiet`, { stdio: 'ignore' });
        }
      } finally {
        try { fs.unlinkSync(dmgPath); } catch { /* ignore */ }
      }
      const hubPath = findUnityHub();
      if (!hubPath) {
        throw new Error('Unity Hub was installed but could not be found.');
      }
      return hubPath;
    }
    case 'linux': {
      const appDir = path.join(process.env['HOME'] ?? '', 'Applications');
      const appImagePath = path.join(appDir, 'UnityHub.AppImage');
      console.log('Downloading Unity Hub AppImage...');
      fs.mkdirSync(appDir, { recursive: true });
      await downloadFile(url, appImagePath);
      fs.chmodSync(appImagePath, 0o755);
      console.log(`Unity Hub installed at: ${appImagePath}`);
      return appImagePath;
    }
    default:
      throw new Error(`Unsupported platform: ${plat}`);
  }
}

/**
 * Find Unity Hub, or install it automatically if not found.
 */
export async function ensureUnityHub(): Promise<string> {
  const hubPath = findUnityHub();
  if (hubPath) return hubPath;

  console.log('Unity Hub not found. Installing automatically...');
  return installUnityHub();
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
 * Parse a Unity version string into comparable numeric parts.
 * Handles formats like "2022.3.62f1", "6000.3.1f1", "2019.4.40f1".
 */
function parseUnityVersion(version: string): number[] {
  // Split on dots and letter boundaries: "2022.3.62f1" -> [2022, 3, 62, 1]
  return version.split(/[.\-]/).flatMap(part => {
    const sub = part.match(/^(\d+)([a-zA-Z]+)(\d+)$/);
    if (sub) return [parseInt(sub[1], 10), parseInt(sub[3], 10)];
    const num = parseInt(part, 10);
    return isNaN(num) ? [] : [num];
  });
}

/**
 * Compare two Unity version strings. Returns positive if a > b, negative if a < b, 0 if equal.
 */
function compareUnityVersions(a: string, b: string): number {
  const partsA = parseUnityVersion(a);
  const partsB = parseUnityVersion(b);
  const len = Math.max(partsA.length, partsB.length);
  for (let i = 0; i < len; i++) {
    const diff = (partsA[i] ?? 0) - (partsB[i] ?? 0);
    if (diff !== 0) return diff;
  }
  return 0;
}

/**
 * Find the installed editor with the highest version number.
 */
export function findHighestEditor(editors: InstalledEditor[]): InstalledEditor {
  return editors.reduce((highest, current) =>
    compareUnityVersions(current.version, highest.version) > 0 ? current : highest
  );
}

/**
 * Install a Unity editor version via Unity Hub CLI.
 */
export function installEditor(hubPath: string, version: string): void {
  console.log(`Installing Unity Editor ${version} via Unity Hub...`);
  try {
    execFileSync(
      hubPath,
      ['--', '--headless', 'install', '--version', version],
      { encoding: 'utf-8', timeout: 600000, stdio: 'inherit' }
    );
  } catch (err) {
    throw new Error(`Failed to install Unity Editor ${version}: ${(err as Error).message}`);
  }
}

/**
 * Resolve the Unity Editor executable path from an editor install path.
 * The path from Unity Hub may already point to the executable (e.g. .../Editor/Unity.exe)
 * or to the install root directory. This function handles both cases.
 */
function resolveEditorExecutable(editorPath: string): string {
  // If the path already points to an existing executable, use it directly
  const basename = path.basename(editorPath).toLowerCase();
  if (basename === 'unity.exe' || (basename === 'unity' && !fs.statSync(editorPath, { throwIfNoEntry: false })?.isDirectory())) {
    if (fs.existsSync(editorPath)) {
      return editorPath;
    }
  }

  // Otherwise, build the path from the install root
  const os = platform();
  const candidates: string[] = [];
  switch (os) {
    case 'win32':
      candidates.push(
        path.join(editorPath, 'Editor', 'Unity.exe'),
        path.join(editorPath, 'Unity.exe')
      );
      break;
    case 'darwin':
      candidates.push(
        path.join(editorPath, 'Unity.app', 'Contents', 'MacOS', 'Unity'),
        path.join(editorPath, 'Contents', 'MacOS', 'Unity')
      );
      break;
    default:
      candidates.push(
        path.join(editorPath, 'Editor', 'Unity'),
        path.join(editorPath, 'Unity')
      );
      break;
  }

  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  // Fallback: return the first candidate and let the caller handle the error
  return candidates[0];
}

/**
 * Create a new Unity project using the Unity Editor directly.
 * Unity Hub's headless CLI does not support a 'create' command in newer versions,
 * so we invoke the editor binary with -createProject -quit -batchmode instead.
 */
export function createProject(hubPath: string, projectPath: string, editorVersion?: string): void {
  // Find the editor install path
  const editors = listInstalledEditors(hubPath);
  if (editors.length === 0) {
    throw new Error('No Unity editors installed. Install one with: unity-mcp-cli install-editor --version <version>');
  }

  let editor: InstalledEditor | undefined;
  if (editorVersion) {
    editor = editors.find(e => e.version === editorVersion);
    if (!editor) {
      throw new Error(`Unity Editor ${editorVersion} not found. Installed versions: ${editors.map(e => e.version).join(', ')}`);
    }
  } else {
    editor = findHighestEditor(editors);
  }

  const editorExe = resolveEditorExecutable(editor.path);
  if (!fs.existsSync(editorExe)) {
    throw new Error(`Unity Editor executable not found at: ${editorExe}`);
  }

  const args = ['-createProject', projectPath, '-quit', '-batchmode'];

  console.log(`Creating Unity project at: ${projectPath}`);
  console.log(`Using Unity Editor: ${editor.version} (${editorExe})`);
  try {
    execFileSync(editorExe, args, {
      encoding: 'utf-8',
      timeout: 120000,
      stdio: 'inherit',
    });
    console.log('Project created successfully.');
  } catch (err) {
    throw new Error(`Failed to create project: ${(err as Error).message}`);
  }
}
