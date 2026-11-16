// Library-safe `installUnityEditor`: install a Unity Editor version through Unity Hub — the library form of the
// `install-unity` CLI command. No console output, no `process.exit`, never throws past the boundary.

import {
  ChangesetNotResolvedError,
  findUnityHub,
  installEditorWithHub,
  isValidUnityVersion,
  queryAvailableReleases,
  queryInstalledEditors,
  resolveChangeset,
  resolveEditorExecutable,
  runHubInstall,
  type AvailableRelease,
  type HubProgress,
} from '../utils/unity-hub.js';
import { listEditorsByCommonLocations } from '../utils/unity-editor.js';
import { emitProgress } from './progress.js';
import type { CreateProjectEditorInfo, ProgressCallback } from './types.js';

/** Why {@link installUnityEditor} failed. */
export type InstallUnityEditorFailureReason =
  /** `version` is not a full Unity version such as `6000.3.1f1`. */
  | 'invalid-version'
  /** Unity Hub is not installed (and no `hubPath` was given). Nothing was installed. */
  | 'hub-not-found'
  /** The version is not on the Hub's releases list and its changeset could not be read from unity.com. */
  | 'changeset-not-found'
  /** The Hub install command failed (exit code and stderr in `errorMessage`). */
  | 'install-failed'
  /** The Hub reported success but the editor is not among the installed editors afterwards. */
  | 'not-found-after-install'
  /** `signal` fired; the Hub process was killed. */
  | 'aborted';

export interface InstallUnityEditorOptions {
  /** Full Unity Editor version, e.g. `"6000.3.1f1"` or `"2022.3.62f3"`. Required. */
  version: string;
  /**
   * Unity Hub executable. When omitted the Hub is looked for in its standard locations. This function never installs
   * Unity Hub: without one it returns `reason: 'hub-not-found'` (the CLI's `install-unity` command installs the Hub).
   */
  hubPath?: string;
  /**
   * Progress callback: `start`, `hub-located` (skipped when the editor is found on disk), then either
   * `already-installed`, or `resolving-changeset` (only for a version off the Hub's releases list), `downloading`
   * (with `percent`), `validating`, `installing`, `installed`.
   * Exceptions it throws are swallowed.
   */
  onProgress?: ProgressCallback;
  /** Cancels the install: the Hub process (and its child processes) is killed and the result is `reason: 'aborted'`. */
  signal?: AbortSignal;
  /** Test injection — Unity Hub discovery (default: the standard locations). */
  findHubImpl?: () => string | null;
  /** Test injection — editors found on disk with no Hub process (default: the Hub's editor folders). */
  listLocalEditorsImpl?: () => CreateProjectEditorInfo[];
  /** Test injection — editors the Unity Hub CLI lists (`--headless editors --installed`). */
  listEditorsImpl?: (hubPath: string) => Promise<CreateProjectEditorInfo[]>;
  /** Test injection — the Hub's promoted releases (`--headless editors --releases`). */
  listReleasesImpl?: (hubPath: string, signal?: AbortSignal) => Promise<AvailableRelease[]>;
  /** Test injection — the changeset of a version off the releases list (default: read from unity.com). */
  resolveChangesetImpl?: (version: string) => Promise<string | null>;
  /** Test injection — runs the Hub `--headless install` command and reports its progress lines. */
  runHubInstallImpl?: typeof runHubInstall;
  /** Test injection — editor install path to executable (default: the cross-platform resolution). */
  resolveEditorExecutableImpl?: (editorPath: string) => string;
}

/** Successful {@link installUnityEditor} outcome. Narrow with `kind === 'success'`. */
export interface InstallUnityEditorSuccess {
  kind: 'success';
  success: true;
  version: string;
  /** Absolute path of the installed editor's executable. */
  editorPath: string;
  /** `true` when the version was installed before the call (nothing was downloaded). */
  alreadyInstalled: boolean;
  /** The changeset passed to the Hub, for a version off its releases list. */
  changeset?: string;
  /** Non-fatal warnings, e.g. a Hub listing that failed and was skipped. */
  warnings: string[];
}

/** Failed {@link installUnityEditor} outcome. Narrow with `kind === 'failure'`. */
export interface InstallUnityEditorFailure {
  kind: 'failure';
  success: false;
  reason: InstallUnityEditorFailureReason;
  version: string;
  /** The Unity Hub used, once it was located. */
  hubPath?: string;
  warnings: string[];
  errorMessage: string;
  error: Error;
}

export type InstallUnityEditorResult = InstallUnityEditorSuccess | InstallUnityEditorFailure;

class InstallFailure extends Error {
  constructor(readonly reason: InstallUnityEditorFailureReason, message: string) {
    super(message);
  }
}

/** The progress event a Hub progress line maps to; `null` for lines the caller need not see. */
function hubProgressEvent(version: string, { percent, status }: HubProgress): Parameters<ProgressCallback>[0] | null {
  switch (status) {
    case 'Starting download':
    case 'Downloading':
    case 'Download complete':
      return { phase: 'downloading', message: `Downloading Unity Editor ${version}: ${percent.toFixed(1)}%`, version, percent };
    case 'Validating download':
    case 'Validating installation':
      return { phase: 'validating', message: `${status} of Unity Editor ${version}`, version };
    case 'Queued for install':
    case 'Installing':
      return { phase: 'installing', message: `Installing Unity Editor ${version}`, version };
    default:
      // 'Installed' is reported once the editor is found on disk; unknown Hub lines are not progress.
      return null;
  }
}

/**
 * Install a Unity Editor version through Unity Hub (`-- --headless install`), the library form of the
 * `install-unity` command, which shares its Hub install core. Library-safe: never calls `process.exit`, never writes
 * to stdout / stderr, never throws past the boundary.
 *
 * An editor already installed (found on disk first, then via the Hub CLI) is reported as `alreadyInstalled: true`
 * without downloading anything. Otherwise the editor is installed — by version when the Hub lists it as a release,
 * else with the changeset read from its unity.com release notes — and then looked up again, so the result carries the
 * installed editor's executable. Only the editor is installed, no platform modules. Unity Hub is never installed.
 */
export async function installUnityEditor(options: InstallUnityEditorOptions): Promise<InstallUnityEditorResult> {
  const warnings: string[] = [];
  const version = typeof options.version === 'string' ? options.version.trim() : '';
  const { onProgress, signal } = options;
  let hubPath: string | undefined;

  const resolveExe = options.resolveEditorExecutableImpl ?? resolveEditorExecutable;
  const listLocal = options.listLocalEditorsImpl ?? listEditorsByCommonLocations;
  const listHub = options.listEditorsImpl ?? queryInstalledEditors;
  /** The installed editor of `version`: on disk first, then (when the Hub is known) via the Hub CLI. */
  const findInstalled = async (): Promise<string | null> => {
    let editor = listLocal().find((e) => e.version === version);
    if (!editor && hubPath) {
      try {
        editor = (await listHub(hubPath)).find((e) => e.version === version);
      } catch (err) {
        warnings.push((err as Error).message);
      }
    }
    return editor ? resolveExe(editor.path) : null;
  };
  const throwIfAborted = (): void => {
    if (signal?.aborted) throw new InstallFailure('aborted', `Installation of Unity Editor ${version} was aborted`);
  };

  try {
    if (!isValidUnityVersion(version)) {
      throw new InstallFailure('invalid-version', `Invalid Unity Editor version "${options.version}" (expected e.g. 6000.3.1f1).`);
    }
    throwIfAborted();
    emitProgress(onProgress, { phase: 'start', message: `Installing Unity Editor ${version}` });

    // The editor folders need no Hub: an installed editor is reported even where the Hub is missing.
    let editorPath = await findInstalled();
    if (!editorPath) {
      hubPath = options.hubPath ?? (options.findHubImpl ?? findUnityHub)() ?? undefined;
      if (!hubPath) {
        throw new InstallFailure('hub-not-found',
          'Unity Hub not found. Install Unity Hub (https://unity.com/download), or run: unity-mcp-cli install-unity ' + version);
      }
      emitProgress(onProgress, { phase: 'hub-located', message: `Unity Hub located at ${hubPath}`, hubPath });
      editorPath = await findInstalled();
    }
    if (editorPath) {
      emitProgress(onProgress, {
        phase: 'already-installed',
        message: `Unity Editor ${version} is already installed at ${editorPath}`,
        version,
        editorPath,
      });
      return { kind: 'success', success: true, version, editorPath, alreadyInstalled: true, warnings };
    }
    const hub = hubPath as string;
    throwIfAborted();

    let releases: AvailableRelease[] = [];
    try {
      releases = await (options.listReleasesImpl ?? queryAvailableReleases)(hub, signal);
    } catch (err) {
      throwIfAborted();
      // Not fatal: without the list the changeset is resolved, which installs promoted versions too.
      warnings.push((err as Error).message);
    }
    throwIfAborted();

    let lastPercent: number | undefined;
    let changeset: string | undefined;
    try {
      ({ changeset } = await installEditorWithHub(hub, version, releases, {
        signal,
        resolveChangesetImpl: options.resolveChangesetImpl ?? resolveChangeset,
        runHubInstallImpl: options.runHubInstallImpl,
        onEvent: (event) => {
          if (event.type === 'resolving-changeset') {
            emitProgress(onProgress, { phase: 'resolving-changeset', message: `Resolving changeset for ${version}`, version });
          } else if (event.type === 'hub-progress') {
            const progress = hubProgressEvent(version, event);
            if (progress?.phase === 'downloading') {
              if (progress.percent === lastPercent) return;
              lastPercent = progress.percent;
            }
            if (progress) emitProgress(onProgress, progress);
          }
        },
      }));
    } catch (err) {
      throwIfAborted();
      if (err instanceof ChangesetNotResolvedError) throw new InstallFailure('changeset-not-found', err.message);
      throw new InstallFailure('install-failed', (err as Error).message);
    }

    editorPath = await findInstalled();
    if (!editorPath) {
      throw new InstallFailure('not-found-after-install',
        `Unity Hub finished installing Unity Editor ${version}, but the editor is not among the installed editors.`);
    }
    emitProgress(onProgress, {
      phase: 'installed',
      message: `Unity Editor ${version} installed at ${editorPath}`,
      version,
      editorPath,
    });
    return { kind: 'success', success: true, version, editorPath, alreadyInstalled: false, changeset, warnings };
  } catch (err: unknown) {
    const error = err instanceof Error ? err : new Error(String(err));
    return {
      kind: 'failure',
      success: false,
      reason: err instanceof InstallFailure ? err.reason : 'install-failed',
      version,
      hubPath,
      warnings,
      errorMessage: error.message,
      error,
    };
  }
}
