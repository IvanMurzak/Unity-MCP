import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { execFileSync, execSync, spawn } from 'child_process';
import { platform } from 'os';
import { get as httpsGet } from 'https';
import { get as httpGet, IncomingMessage } from 'http';
import type { Spinner } from 'yocto-spinner';
import * as ui from './ui.js';
import { verbose } from './ui.js';

export interface AvailableRelease {
  version: string;
  isStable: boolean;
}

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
  const startedAt = Date.now();
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
    const candidateStartedAt = Date.now();
    verbose(`Checking Unity Hub candidate: ${candidate}`);
    if (candidate && fs.existsSync(candidate)) {
      verbose(`Found Unity Hub at: ${candidate} (${Date.now() - candidateStartedAt}ms for candidate, ${Date.now() - startedAt}ms total)`);
      return candidate;
    }
    verbose(`Unity Hub candidate missing: ${candidate} (${Date.now() - candidateStartedAt}ms)`);
  }

  verbose(`Unity Hub not found in any candidate location (${Date.now() - startedAt}ms total)`);
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
      const spinner = ui.startSpinner('Downloading Unity Hub installer...');
      try {
        await downloadFile(url, installerPath);
      } catch (err) {
        spinner.error('Failed to download Unity Hub installer');
        throw err;
      }
      spinner.success('Unity Hub installer downloaded');
      ui.info('Installing Unity Hub silently (may require administrator privileges)...');
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
      const spinner = ui.startSpinner('Downloading Unity Hub installer...');
      try {
        await downloadFile(url, dmgPath);
      } catch (err) {
        spinner.error('Failed to download Unity Hub installer');
        throw err;
      }
      spinner.success('Unity Hub installer downloaded');
      const installSpinner = ui.startSpinner('Installing Unity Hub...');
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
        installSpinner.success('Unity Hub installed');
      } catch (err) {
        installSpinner.error('Unity Hub installation failed');
        throw err;
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
      const spinner = ui.startSpinner('Downloading Unity Hub AppImage...');
      fs.mkdirSync(appDir, { recursive: true });
      try {
        await downloadFile(url, appImagePath);
      } catch (err) {
        spinner.error('Failed to download Unity Hub AppImage');
        throw err;
      }
      fs.chmodSync(appImagePath, 0o755);
      spinner.success(`Unity Hub installed at: ${appImagePath}`);
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
  const startedAt = Date.now();
  const hubPath = findUnityHub();
  if (hubPath) {
    verbose(`ensureUnityHub reused existing installation in ${Date.now() - startedAt}ms`);
    return hubPath;
  }

  ui.warn('Unity Hub not found. Installing automatically...');
  const installedHubPath = await installUnityHub();
  verbose(`ensureUnityHub installed Unity Hub in ${Date.now() - startedAt}ms`);
  return installedHubPath;
}

export interface InstalledEditor {
  version: string;
  path: string;
}

/**
 * List installed Unity editors via Unity Hub CLI.
 */
/**
 * Parse a single line from Unity Hub `--installed` output into an InstalledEditor entry.
 * Handles formats such as:
 *   "2022.3.62f3 , installed at /path/to/editor"
 *   "6000.3.11f1 (Apple silicon) installed at /path/to/editor"
 *   "6000.3.11f1 (Apple silicon), installed at /path/to/editor"
 * Returns null if the line does not match the expected format.
 */
export function parseInstalledEditorsLine(line: string): InstalledEditor | null {
  const trimmed = line.trim();
  if (!trimmed) return null;

  const match = trimmed.match(/^([\d.]+\w*)\s*(?:\([^)]*\)\s*)?(?:,\s*)?installed at\s+(.+)$/i);
  if (match) {
    return { version: match[1], path: match[2].trim() };
  }
  return null;
}

export function listInstalledEditors(hubPath: string): InstalledEditor[] {
  const spinner = ui.startSpinner('Listing installed editors...');
  const startedAt = Date.now();
  try {
    verbose(`listInstalledEditors invoking Unity Hub CLI: ${hubPath} -- --headless editors --installed`);
    const execStartedAt = Date.now();
    const output = execFileSync(hubPath, ['--', '--headless', 'editors', '--installed'], {
      encoding: 'utf-8',
      timeout: 120000,
    });
    verbose(`listInstalledEditors Unity Hub CLI returned in ${Date.now() - execStartedAt}ms`);

    const parseStartedAt = Date.now();
    const editors: InstalledEditor[] = [];
    for (const line of output.split('\n')) {
      const editor = parseInstalledEditorsLine(line);
      if (editor) {
        editors.push(editor);
      }
    }
    verbose(`listInstalledEditors parsed ${editors.length} editor(s) in ${Date.now() - parseStartedAt}ms`);

    spinner.success(`Found ${editors.length} installed editor${editors.length !== 1 ? 's' : ''}`);
    verbose(`listInstalledEditors completed in ${Date.now() - startedAt}ms`);
    return editors;
  } catch (err) {
    spinner.error(`Failed to list installed editors: ${(err as Error).message}`);
    verbose(`listInstalledEditors failed after ${Date.now() - startedAt}ms`);
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
 * List available Unity editor releases from Unity Hub CLI.
 */
export function listAvailableReleases(hubPath: string): AvailableRelease[] {
  const spinner = ui.startSpinner('Fetching available releases...');
  try {
    const output = execFileSync(hubPath, ['--', '--headless', 'editors', '--releases'], {
      encoding: 'utf-8',
      timeout: 120000,
    });

    const releases: AvailableRelease[] = [];
    for (const line of output.split('\n')) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      // Extract version from line (may also contain "installed at ..." suffix)
      const match = trimmed.match(/^([\d.]+[a-zA-Z]\d+)/);
      if (match) {
        const version = match[1];
        const isStable = /f\d+$/.test(version);
        releases.push({ version, isStable });
      }
    }

    spinner.success(`Found ${releases.length} available release${releases.length !== 1 ? 's' : ''}`);
    return releases;
  } catch (err) {
    spinner.error(`Failed to fetch available releases: ${(err as Error).message}`);
    return [];
  }
}

/**
 * Find the latest stable (f-suffix) release from the available releases.
 */
export function findLatestStableRelease(releases: AvailableRelease[]): AvailableRelease | null {
  const stable = releases.filter(r => r.isStable);
  if (stable.length === 0) return null;
  return stable.reduce((highest, current) =>
    compareUnityVersions(current.version, highest.version) > 0 ? current : highest
  );
}

const FETCH_TIMEOUT_MS = 10000;
const FETCH_MAX_REDIRECTS = 5;

/**
 * Fetch a URL and return the response body as a string.
 * Supports redirect following up to FETCH_MAX_REDIRECTS deep, and enforces a
 * FETCH_TIMEOUT_MS request timeout to prevent indefinite hangs.
 */
function fetchUrl(url: string, redirectsRemaining = FETCH_MAX_REDIRECTS): Promise<string> {
  return new Promise((resolve, reject) => {
    const controller = new AbortController();
    const timer = setTimeout(() => {
      controller.abort();
      reject(new Error(`Request timed out after ${FETCH_TIMEOUT_MS}ms: ${url}`));
    }, FETCH_TIMEOUT_MS);

    const doGet = url.startsWith('https') ? httpsGet : httpGet;
    const req = doGet(url, { signal: controller.signal } as Parameters<typeof httpsGet>[1], (response: IncomingMessage) => {
      if (response.statusCode && response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
        clearTimeout(timer);
        if (redirectsRemaining <= 0) {
          response.resume();
          reject(new Error(`Too many redirects (max ${FETCH_MAX_REDIRECTS}) fetching: ${url}`));
          return;
        }
        response.resume();
        fetchUrl(new URL(response.headers.location, url).toString(), redirectsRemaining - 1).then(resolve, reject);
        return;
      }
      if (response.statusCode && response.statusCode !== 200) {
        clearTimeout(timer);
        response.resume();
        reject(new Error(`HTTP ${response.statusCode}`));
        return;
      }
      const chunks: Buffer[] = [];
      response.on('data', (chunk: Buffer) => chunks.push(chunk));
      response.on('end', () => { clearTimeout(timer); resolve(Buffer.concat(chunks).toString('utf-8')); });
      response.on('error', (err) => { clearTimeout(timer); reject(err); });
    });
    req.on('error', (err) => { clearTimeout(timer); reject(err); });
  });
}

/**
 * Resolve the changeset hash for a Unity version by scraping the release notes page.
 * Returns the changeset string, or null if not found.
 */
// Unity version format: major.minor.patchSuffix (e.g. 2022.3.62f3, 6000.3.1f1)
const UNITY_VERSION_RE = /^\d+\.\d+\.\d+[a-zA-Z]\d+$/;

export function isValidUnityVersion(version: string): boolean {
  return UNITY_VERSION_RE.test(version);
}

export async function resolveChangeset(version: string): Promise<string | null> {
  if (!isValidUnityVersion(version)) return null;

  const url = `https://unity.com/releases/editor/whats-new/${version}`;
  try {
    const html = await fetchUrl(url);
    // Look for unityhub://VERSION/CHANGESET pattern
    const match = html.match(/unityhub:\/\/[^/]+\/([a-f0-9]{12})/);
    if (match) return match[1];
    return null;
  } catch {
    return null;
  }
}

/**
 * Parse Unity Hub CLI stdout and update the progress bar accordingly.
 * Hub output lines look like:
 *   [Unity (6000.3.11f1)] downloading 50.54%
 *   [Unity (6000.3.11f1)] validating download...
 *   [Unity (6000.3.11f1)] installing...
 *   [Unity (6000.3.11f1)] installed successfully.
 */
export function parseHubProgress(line: string): { percent: number; status: string } | null {
  const trimmed = line.replace(/\x1b\[[0-9;]*[A-Za-z]/g, '').trim();
  if (!trimmed || trimmed === 'Progress:' || trimmed === 'All Tasks Completed Successfully.') return null;

  // Match: [Unity (VERSION)] STATUS
  const match = trimmed.match(/^\[.+?\]\s+(.+)$/);
  if (!match) return null;

  const status = match[1];

  // "downloading 50.54%"
  const dlMatch = status.match(/^downloading\s+([\d.]+)%$/);
  if (dlMatch) {
    return { percent: parseFloat(dlMatch[1]), status: 'Downloading' };
  }

  // Map known phases to approximate progress
  if (status.includes('validating download')) return { percent: 0, status: 'Validating download' };
  if (status.includes('in progress')) return { percent: 0, status: 'Starting download' };
  if (status.includes('finished downloading')) return { percent: 100, status: 'Download complete' };
  if (status.includes('queued for install')) return { percent: 100, status: 'Queued for install' };
  if (status.includes('validating installation')) return { percent: 100, status: 'Validating installation' };
  if (status.includes('installing')) return { percent: 100, status: 'Installing' };
  if (status.includes('installed successfully')) return { percent: 100, status: 'Installed' };

  return { percent: 0, status };
}

/**
 * Run Unity Hub install command with a progress bar.
 * Spawns the process and parses stdout to display download/install progress.
 */
function runHubInstallWithProgress(hubPath: string, args: string[], version: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const progress = ui.createProgressBar();
    let activeSpinner: Spinner | null = ui.startSpinner(`Preparing Unity Editor ${version}...`);
    let isDownloading = false;

    const proc = spawn(hubPath, args, { stdio: ['ignore', 'pipe', 'pipe'] });
    let stderr = '';

    function stopActiveSpinner(msg: string): void {
      if (activeSpinner) {
        activeSpinner.success(msg);
        activeSpinner = null;
      }
    }

    function startPhaseSpinner(status: string): void {
      stopActiveSpinner(activeSpinner?.text ?? '');
      activeSpinner = ui.startSpinner(`${status}...`);
    }

    let stdoutBuffer = '';
    proc.stdout.on('data', (data: Buffer) => {
      const text = stdoutBuffer + data.toString('utf-8');
      const lines = text.split('\n');
      stdoutBuffer = lines.pop() ?? '';
      for (const line of lines) {
        const parsed = parseHubProgress(line);
        if (!parsed) continue;

        if (parsed.status === 'Downloading' || parsed.status === 'Starting download') {
          if (!isDownloading) {
            isDownloading = true;
            stopActiveSpinner(`Downloading Unity Editor ${version}`);
          }
          progress.update(parsed.percent, `Downloading Unity Editor ${version}`);
        } else if (parsed.status === 'Download complete') {
          if (isDownloading) {
            progress.complete(`Unity Editor ${version} downloaded`);
            isDownloading = false;
          }
        } else if (parsed.status === 'Installed') {
          stopActiveSpinner(`Unity Editor ${version} installed`);
        } else {
          // Post-download phases: show as a spinner so the user sees activity
          if (!isDownloading) {
            startPhaseSpinner(parsed.status);
          }
        }
      }
    });

    proc.stderr.on('data', (data: Buffer) => { stderr += data.toString('utf-8'); });

    proc.on('close', (code) => {
      if (activeSpinner) activeSpinner.stop();
      if (code === 0) {
        if (isDownloading) {
          progress.complete(`Unity Editor ${version} downloaded`);
        }
        resolve();
      } else {
        if (isDownloading) {
          progress.fail(`Download failed for Unity Editor ${version}`);
        }
        reject(new Error(
          `Failed to install Unity Editor ${version} (exit code ${code})` +
          (stderr ? `:\n${stderr.trim()}` : '')
        ));
      }
    });

    proc.on('error', (err) => {
      if (activeSpinner) activeSpinner.stop();
      reject(new Error(`Failed to install Unity Editor ${version}: ${err.message}`));
    });
  });
}

/**
 * Install a Unity editor version via Unity Hub CLI.
 * Automatically resolves changeset for versions not in the promoted releases list.
 * Shows a progress bar during download and status during installation.
 */
export async function installEditor(hubPath: string, version: string, prefetchedReleases?: AvailableRelease[]): Promise<void> {
  // Use pre-fetched releases when available to avoid a duplicate Hub CLI call
  const releases = prefetchedReleases ?? listAvailableReleases(hubPath);
  const isPromoted = releases.some(r => r.version === version);

  const baseArgs = ['--', '--headless', 'install', '--version', version];

  if (isPromoted) {
    await runHubInstallWithProgress(hubPath, baseArgs, version);
    return;
  }

  // Version not in releases — resolve changeset
  const changesetSpinner = ui.startSpinner(`Resolving changeset for ${version}...`);
  const changeset = await resolveChangeset(version);
  if (!changeset) {
    changesetSpinner.error(`Could not resolve changeset for ${version}`);
    throw new Error(
      `Unity Editor ${version} is not in the promoted releases list and its changeset could not be resolved. ` +
      `You can provide a changeset manually: unity-hub -- --headless install --version ${version} --changeset <hash>`
    );
  }
  changesetSpinner.success(`Resolved changeset: ${changeset}`);

  await runHubInstallWithProgress(hubPath, [...baseArgs, '--changeset', changeset], version);
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
      // If path already ends with .app, go directly into Contents/MacOS/Unity
      if (editorPath.endsWith('.app')) {
        candidates.push(path.join(editorPath, 'Contents', 'MacOS', 'Unity'));
      }
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
    throw new Error('No Unity editors installed. Install one with: unity-mcp-cli install-unity [version]');
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

  ui.info(`Creating Unity project at: ${projectPath}`);
  ui.label('Unity Editor', `${editor.version} (${editorExe})`);
  try {
    execFileSync(editorExe, args, {
      encoding: 'utf-8',
      timeout: 120000,
      stdio: 'inherit',
    });
    ui.success('Project created successfully.');
  } catch (err) {
    throw new Error(`Failed to create project: ${(err as Error).message}`);
  }
}
