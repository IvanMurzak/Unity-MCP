import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { execFile, execFileSync, execSync, spawn } from 'child_process';
import { platform } from 'os';
import { get as httpsGet } from 'https';
import { get as httpGet, IncomingMessage } from 'http';
import type { Spinner } from 'yocto-spinner';
import { killProcessTree } from '@baizor/gamedev-cli-core';
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
 * How long the Hub CLI may take to list the installed editors. A cold Hub answers in about 15 s; with the Hub app
 * open the call can hang, and the editor folders were already checked on disk before it (`findEditorPath`).
 */
export const HUB_LIST_EDITORS_TIMEOUT_MS = 30_000;

/**
 * Pull the `stderr` capture off an `execFileSync` error and merge it
 * into the error message. Returns the bare `err.message` when no
 * stderr was captured (e.g. spawn failures before the child ran).
 */
function formatExecError(err: unknown): string {
  const message = (err as Error).message;
  const stderr = (err as { stderr?: Buffer | string }).stderr;
  if (!stderr) return message;
  const text = (Buffer.isBuffer(stderr) ? stderr.toString('utf-8') : stderr).trim();
  return text ? `${message}\n${text}` : message;
}

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

/**
 * Run a Unity Hub `-- --headless <args>` query and resolve its stdout. stderr is captured, not inherited: Unity Hub
 * is an Electron app and Chromium routinely logs benign quota_database errors to stderr that would otherwise pollute
 * the CLI output ({@link formatExecError} re-surfaces it on real failures). Rejects with the `execFile` error carrying
 * `stdout`, `stderr` and `timedOut`.
 */
function execHubHeadless(hubPath: string, args: string[], timeoutMs: number, signal?: AbortSignal): Promise<string> {
  return new Promise((resolve, reject) => {
    const child = execFile(hubPath, ['--', '--headless', ...args], {
      encoding: 'utf-8',
      timeout: timeoutMs,
      killSignal: 'SIGKILL',
      windowsHide: true,
      maxBuffer: 16 * 1024 * 1024,
      signal,
    }, (err, stdout, stderr) => {
      if (!err) {
        resolve(stdout);
        return;
      }
      const timedOut = err.killed === true && err.code !== 'ERR_CHILD_PROCESS_STDIO_MAXBUFFER' && err.name !== 'AbortError';
      reject(Object.assign(err, { stdout, stderr, timedOut }));
    });
    child.stdin?.end();
  });
}

/**
 * Query Unity Hub for installed editors — the silent core shared by
 * the spinner-wrapped {@link listInstalledEditors} (CLI surface) and
 * the library's `createProject` (which must not write to stdout).
 * Asynchronous (the Hub is an Electron app and may take seconds, or hang
 * while its window is open) and bounded by `timeoutMs`. Rejects with an
 * `Error` carrying the formatted Hub CLI failure; callers decide how to
 * surface it.
 */
export async function queryInstalledEditors(
  hubPath: string,
  timeoutMs: number = HUB_LIST_EDITORS_TIMEOUT_MS,
  signal?: AbortSignal,
): Promise<InstalledEditor[]> {
  verbose(`queryInstalledEditors invoking Unity Hub CLI: ${hubPath} -- --headless editors --installed`);
  const execStartedAt = Date.now();
  let output: string;
  try {
    output = await execHubHeadless(hubPath, ['editors', '--installed'], timeoutMs, signal);
  } catch (err) {
    const { stdout = '', timedOut = false } = err as { stdout?: string; timedOut?: boolean };
    // A Hub that printed its list but never exited (seen while the Hub app runs) still answered: keep its
    // complete lines (the last one may have been cut off by the kill).
    const complete = stdout.slice(0, stdout.lastIndexOf('\n') + 1);
    if (!timedOut || !complete.split('\n').some((line) => parseInstalledEditorsLine(line) !== null)) {
      verbose(`queryInstalledEditors failed after ${Date.now() - execStartedAt}ms`);
      throw new Error(timedOut
        ? `Failed to list installed editors: Unity Hub did not answer within ${Math.round(timeoutMs / 1000)}s`
        : `Failed to list installed editors: ${formatExecError(err)}`);
    }
    output = complete;
  }
  verbose(`queryInstalledEditors Unity Hub CLI returned in ${Date.now() - execStartedAt}ms`);

  const parseStartedAt = Date.now();
  const editors: InstalledEditor[] = [];
  for (const line of output.split('\n')) {
    const editor = parseInstalledEditorsLine(line);
    if (editor) {
      editors.push(editor);
    }
  }
  verbose(`queryInstalledEditors parsed ${editors.length} editor(s) in ${Date.now() - parseStartedAt}ms`);
  return editors;
}

export async function listInstalledEditors(hubPath: string): Promise<InstalledEditor[]> {
  const spinner = ui.startSpinner('Listing installed editors...');
  const startedAt = Date.now();
  try {
    const editors = await queryInstalledEditors(hubPath);
    spinner.success(`Found ${editors.length} installed editor${editors.length !== 1 ? 's' : ''}`);
    verbose(`listInstalledEditors completed in ${Date.now() - startedAt}ms`);
    return editors;
  } catch (err) {
    spinner.error((err as Error).message);
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
 * Parse Unity Hub `-- --headless editors --releases` output: one entry per line that starts with a version (a line
 * may also carry an "installed at ..." suffix).
 */
export function parseAvailableReleases(output: string): AvailableRelease[] {
  const releases: AvailableRelease[] = [];
  for (const line of output.split('\n')) {
    const match = line.trim().match(/^([\d.]+[a-zA-Z]\d+)/);
    if (match) {
      releases.push({ version: match[1], isStable: /f\d+$/.test(match[1]) });
    }
  }
  return releases;
}

/** How long the Hub CLI may take to list the available releases. */
const HUB_LIST_RELEASES_TIMEOUT_MS = 120_000;

/**
 * List available Unity editor releases from Unity Hub CLI — silent (no spinner), rejects with the Hub CLI failure,
 * and `signal` kills the Hub process. The CLI wraps it in a spinner ({@link listAvailableReleases}).
 */
export async function queryAvailableReleases(hubPath: string, signal?: AbortSignal): Promise<AvailableRelease[]> {
  try {
    return parseAvailableReleases(await execHubHeadless(hubPath, ['editors', '--releases'], HUB_LIST_RELEASES_TIMEOUT_MS, signal));
  } catch (err) {
    throw new Error(`Failed to fetch available releases: ${formatExecError(err)}`);
  }
}

/**
 * {@link queryAvailableReleases} with a spinner; an empty list when the Hub fails.
 */
export async function listAvailableReleases(hubPath: string): Promise<AvailableRelease[]> {
  const spinner = ui.startSpinner('Fetching available releases...');
  try {
    const releases = await queryAvailableReleases(hubPath);
    spinner.success(`Found ${releases.length} available release${releases.length !== 1 ? 's' : ''}`);
    return releases;
  } catch (err) {
    spinner.error((err as Error).message);
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

/** The install phase a Unity Hub progress line belongs to (`other` for a status line not known here). */
export type HubPhase = 'downloading' | 'download-complete' | 'validating' | 'installing' | 'installed' | 'other';

/** One Unity Hub progress line, as {@link parseHubProgress} reads it. */
export type HubProgress = { percent: number; status: string; phase: HubPhase };

/**
 * Parse one line of Unity Hub install output into its progress.
 * Hub output lines look like:
 *   [Unity (6000.3.11f1)] downloading 50.54%
 *   [Unity (6000.3.11f1)] validating download...
 *   [Unity (6000.3.11f1)] installing...
 *   [Unity (6000.3.11f1)] installed successfully.
 */
export function parseHubProgress(line: string): HubProgress | null {
  const trimmed = line.replace(/\x1b\[[0-9;]*[A-Za-z]/g, '').trim();
  if (!trimmed || trimmed === 'Progress:' || trimmed === 'All Tasks Completed Successfully.') return null;

  // Match: [Unity (VERSION)] STATUS
  const match = trimmed.match(/^\[.+?\]\s+(.+)$/);
  if (!match) return null;

  const status = match[1];

  // "downloading 50.54%"
  const dlMatch = status.match(/^downloading\s+([\d.]+)%$/);
  if (dlMatch) {
    return { percent: parseFloat(dlMatch[1]), status: 'Downloading', phase: 'downloading' };
  }

  // Map known phases to approximate progress
  if (status.includes('validating download')) return { percent: 0, status: 'Validating download', phase: 'validating' };
  if (status.includes('in progress')) return { percent: 0, status: 'Starting download', phase: 'downloading' };
  if (status.includes('finished downloading')) return { percent: 100, status: 'Download complete', phase: 'download-complete' };
  if (status.includes('queued for install')) return { percent: 100, status: 'Queued for install', phase: 'installing' };
  if (status.includes('validating installation')) return { percent: 100, status: 'Validating installation', phase: 'validating' };
  if (status.includes('installing')) return { percent: 100, status: 'Installing', phase: 'installing' };
  if (status.includes('installed successfully')) return { percent: 100, status: 'Installed', phase: 'installed' };

  return { percent: 0, status, phase: 'other' };
}


export interface RunHubInstallOptions {
  /** Called for every progress line the Hub prints. */
  onProgress?: (progress: HubProgress) => void;
  /** Kills the Hub process and the processes it started, and rejects with an `AbortError`. */
  signal?: AbortSignal;
}

/** The last this many characters of the Hub's stderr are kept for the failure message. */
const HUB_STDERR_TAIL_CHARS = 16 * 1024;

function abortError(version: string): Error {
  return new DOMException(`Installation of Unity Editor ${version} was aborted`, 'AbortError');
}

/**
 * Run a Unity Hub `-- --headless install ...` command and report each progress line it prints. Silent (no spinner,
 * no stdout/stderr writes): the CLI's progress bar ({@link installEditor}) and the library's `installUnityEditor`
 * `onProgress` both sit on top of it. Rejects with the Hub's exit code and stderr when the Hub fails.
 */
export function runHubInstall(
  hubPath: string,
  args: string[],
  version: string,
  options: RunHubInstallOptions = {},
): Promise<void> {
  const { onProgress, signal } = options;
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(abortError(version));
      return;
    }
    let settled = false;
    const settle = (err?: Error): void => {
      if (settled) return;
      settled = true;
      signal?.removeEventListener('abort', onAbort);
      if (err) reject(err);
      else resolve();
    };

    // With a signal on POSIX the Hub leads its own process group, so an abort kills the processes it started too
    // (Windows: `taskkill /T`). Without one the Hub stays in the terminal's group and Ctrl+C reaches it.
    const proc = spawn(hubPath, args, {
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
      detached: signal !== undefined && process.platform !== 'win32',
    });
    const onAbort = (): void => {
      const pid = proc.pid;
      const killed = pid !== undefined ? killProcessTree(pid) : Promise.resolve(false);
      killed.then((ok) => { if (!ok) proc.kill('SIGKILL'); }, () => proc.kill('SIGKILL'));
      settle(abortError(version));
    };
    signal?.addEventListener('abort', onAbort, { once: true });

    const report = (line: string): void => {
      const parsed = parseHubProgress(line);
      if (parsed && !settled) onProgress?.(parsed);
    };
    let stdoutBuffer = '';
    proc.stdout.on('data', (data: Buffer) => {
      const lines = (stdoutBuffer + data.toString('utf-8')).split('\n');
      stdoutBuffer = lines.pop() ?? '';
      lines.forEach(report);
    });
    let stderr = '';
    proc.stderr.on('data', (data: Buffer) => {
      stderr = (stderr + data.toString('utf-8')).slice(-HUB_STDERR_TAIL_CHARS);
    });

    proc.on('close', (code) => {
      report(stdoutBuffer);
      settle(code === 0 ? undefined : new Error(
        `Failed to install Unity Editor ${version} (exit code ${code})` +
        (stderr.trim() ? `:\n${stderr.trim()}` : ''),
      ));
    });
    proc.on('error', (err) => {
      settle(new Error(`Failed to install Unity Editor ${version}: ${err.message}`));
    });
  });
}

/** A version off the Hub's promoted releases list whose changeset could not be read from its release notes. */
export class ChangesetNotResolvedError extends Error {
  constructor(version: string) {
    super(
      `Unity Editor ${version} is not in the promoted releases list and its changeset could not be resolved. ` +
      `You can provide a changeset manually: unity-hub -- --headless install --version ${version} --changeset <hash>`,
    );
    this.name = 'ChangesetNotResolvedError';
  }
}

/** What {@link installEditorWithHub} reports while it works. */
export type EditorInstallEvent =
  | { type: 'resolving-changeset' }
  | { type: 'changeset-resolved'; changeset: string }
  | { type: 'hub-install-started' }
  | ({ type: 'hub-progress' } & HubProgress);

export interface InstallEditorWithHubOptions {
  onEvent?: (event: EditorInstallEvent) => void;
  signal?: AbortSignal;
  /** Test injection — defaults to {@link resolveChangeset}. */
  resolveChangesetImpl?: (version: string) => Promise<string | null>;
  /** Test injection — defaults to {@link runHubInstall}. */
  runHubInstallImpl?: typeof runHubInstall;
}

/**
 * Install `version` with Unity Hub: a version on the Hub's promoted `releases` list by version alone, any other with
 * the changeset read from its release-notes page ({@link ChangesetNotResolvedError} when there is none). The silent
 * core shared by the CLI's {@link installEditor} and the library's `installUnityEditor`. Resolves the changeset it
 * passed to the Hub (none for a promoted version).
 */
export async function installEditorWithHub(
  hubPath: string,
  version: string,
  releases: AvailableRelease[],
  options: InstallEditorWithHubOptions = {},
): Promise<{ changeset?: string }> {
  const { onEvent } = options;
  const args = ['--', '--headless', 'install', '--version', version];
  let changeset: string | undefined;
  if (!releases.some((r) => r.version === version)) {
    onEvent?.({ type: 'resolving-changeset' });
    changeset = (await (options.resolveChangesetImpl ?? resolveChangeset)(version)) ?? undefined;
    if (!changeset) throw new ChangesetNotResolvedError(version);
    onEvent?.({ type: 'changeset-resolved', changeset });
    args.push('--changeset', changeset);
  }
  onEvent?.({ type: 'hub-install-started' });
  await (options.runHubInstallImpl ?? runHubInstall)(hubPath, args, version, {
    signal: options.signal,
    onProgress: (progress) => onEvent?.({ type: 'hub-progress', ...progress }),
  });
  return { changeset };
}

/**
 * Install a Unity editor version via Unity Hub CLI.
 * Automatically resolves changeset for versions not in the promoted releases list.
 * Shows a progress bar during download and status during installation.
 */
export async function installEditor(hubPath: string, version: string, prefetchedReleases?: AvailableRelease[]): Promise<void> {
  // Use pre-fetched releases when available to avoid a duplicate Hub CLI call
  const releases = prefetchedReleases ?? await listAvailableReleases(hubPath);
  const progress = ui.createProgressBar();
  // Asserted so the assignments in the event handler do not narrow it to `null` below.
  let activeSpinner = null as Spinner | null;
  let isDownloading = false;

  const stopActiveSpinner = (msg: string): void => {
    activeSpinner?.success(msg);
    activeSpinner = null;
  };

  const onEvent = (event: EditorInstallEvent): void => {
    switch (event.type) {
      case 'resolving-changeset':
        activeSpinner = ui.startSpinner(`Resolving changeset for ${version}...`);
        return;
      case 'changeset-resolved':
        stopActiveSpinner(`Resolved changeset: ${event.changeset}`);
        return;
      case 'hub-install-started':
        activeSpinner = ui.startSpinner(`Preparing Unity Editor ${version}...`);
        return;
      case 'hub-progress':
        if (event.phase === 'downloading') {
          if (!isDownloading) {
            isDownloading = true;
            stopActiveSpinner(`Downloading Unity Editor ${version}`);
          }
          progress.update(event.percent, `Downloading Unity Editor ${version}`);
        } else if (event.phase === 'download-complete') {
          if (isDownloading) {
            progress.complete(`Unity Editor ${version} downloaded`);
            isDownloading = false;
          }
        } else if (event.phase === 'installed') {
          stopActiveSpinner(`Unity Editor ${version} installed`);
        } else if (!isDownloading) {
          // Post-download phases: show as a spinner so the user sees activity
          stopActiveSpinner(activeSpinner?.text ?? '');
          activeSpinner = ui.startSpinner(`${event.status}...`);
        }
        return;
    }
  };

  try {
    await installEditorWithHub(hubPath, version, releases, { onEvent });
  } catch (err) {
    if (err instanceof ChangesetNotResolvedError) {
      activeSpinner?.error(`Could not resolve changeset for ${version}`);
      activeSpinner = null;
    } else {
      activeSpinner?.stop();
      if (isDownloading) progress.fail(`Download failed for Unity Editor ${version}`);
    }
    throw err;
  }
  activeSpinner?.stop();
  if (isDownloading) progress.complete(`Unity Editor ${version} downloaded`);
}

/**
 * Resolve the Unity Editor executable path from an editor install path.
 * The path from Unity Hub may already point to the executable (e.g. .../Editor/Unity.exe)
 * or to the install root directory. This function handles both cases.
 */
export function resolveEditorExecutable(editorPath: string): string {
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

/** {@link resolveEditorExecutable}, or `null` when no executable exists there. */
export function findEditorExecutable(editorPath: string): string | null {
  const exe = resolveEditorExecutable(editorPath);
  return fs.existsSync(exe) ? exe : null;
}

/**
 * Default timeout (ms) for the editor's `-createProject` invocation —
 * the single source of truth shared with `lib/create-project.ts`.
 */
export const DEFAULT_CREATE_TIMEOUT_MS = 120000;

/**
 * Invoke the Unity Editor binary to create a new project at
 * `projectPath`. Unity Hub's headless CLI does not support a 'create'
 * command in newer versions, so we invoke the editor binary with
 * `-createProject -quit -batchmode` instead.
 *
 * Silent (library-safe): no spinners, no stdout/stderr writes — the
 * editor's output is captured and folded into the rejection error on
 * failure. The orchestration around this call (editor discovery /
 * version selection) lives in `lib/create-project.ts`; the CLI's
 * `create-project` command delegates there.
 */
export function runEditorCreateProject(
  editorExePath: string,
  projectPath: string,
  timeoutMs = DEFAULT_CREATE_TIMEOUT_MS,
): Promise<void> {
  return new Promise((resolve, reject) => {
    execFile(
      editorExePath,
      ['-createProject', projectPath, '-quit', '-batchmode'],
      // maxBuffer: Infinity — captured output is only used to enrich the
      // failure message, and Unity's verbose first-run import logs can
      // exceed Node's 1 MiB default, which would otherwise kill (with
      // ERR_CHILD_PROCESS_STDOUT_MAXBUFFER) a creation that was succeeding.
      { encoding: 'utf-8', timeout: timeoutMs, windowsHide: true, maxBuffer: Infinity },
      (err) => {
        if (err) {
          // On timeout, execFile kills the process with SIGTERM and leaves
          // a generic "Command failed" message, so name the timeout
          // explicitly. Otherwise execFile already folds the captured
          // stderr into err.message ("Command failed: <cmd>\n<stderr>") —
          // surface that as-is rather than appending stderr a second time.
          const message = err.killed && err.signal === 'SIGTERM'
            ? `Failed to create project: timed out after ${timeoutMs}ms`
            : `Failed to create project: ${err.message}`;
          reject(new Error(message));
          return;
        }
        resolve();
      },
    );
  });
}
