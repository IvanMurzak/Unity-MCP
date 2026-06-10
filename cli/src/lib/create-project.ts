import * as fs from 'fs';
import * as path from 'path';
import {
  findUnityHub,
  queryInstalledEditors,
  findHighestEditor,
  resolveEditorExecutable,
  runEditorCreateProject,
  DEFAULT_CREATE_TIMEOUT_MS,
} from '../utils/unity-hub.js';
import { emitProgress } from './progress.js';
import type {
  CreateProjectEditorInfo,
  CreateProjectOptions,
  CreateProjectResult,
} from './types.js';

/**
 * Resolve an editor install path to its executable, returning `null`
 * when no executable exists at the resolved location. Default for
 * `CreateProjectOptions.resolveEditorExecutableImpl`.
 */
function defaultResolveEditorExecutable(editorInstallPath: string): string | null {
  const exe = resolveEditorExecutable(editorInstallPath);
  return fs.existsSync(exe) ? exe : null;
}

/**
 * Create a new Unity project — the library-callable equivalent of the
 * `create-project` CLI command. Library-safe: never calls
 * `process.exit`, never prints to stdout / stderr, never throws past
 * the public boundary.
 *
 * The orchestration (Unity Hub discovery, installed-editor query,
 * version selection, executable resolution, `-createProject` spawn)
 * is shared with the CLI command — `commands/create-project.ts`
 * delegates to this function so the two paths cannot drift.
 *
 * Unlike the CLI surface, this function never installs Unity Hub.
 * When the Hub is missing (and no `hubPath` override is supplied) it
 * returns a `kind: 'failure'` result; callers that want the
 * auto-install bootstrap (like the CLI) run `ensureUnityHub()` first
 * and pass the result via `options.hubPath`.
 *
 * Returns a discriminated union — narrow with
 * `result.kind === 'success'` to access `projectPath` /
 * `editorVersion` / `editorPath`, or `result.kind === 'failure'` to
 * access `errorMessage` / `error`.
 */
export async function createProject(
  options: CreateProjectOptions,
): Promise<CreateProjectResult> {
  const warnings: string[] = [];
  let resolvedProjectPath: string | undefined;
  let resolvedVersion: string | undefined;
  let resolvedEditorExe: string | undefined;

  try {
    // -- Argument validation (before any I/O) ---------------------------
    if (typeof options.projectPath !== 'string' || options.projectPath.trim().length === 0) {
      throw new Error('projectPath is required and must be a non-empty string.');
    }
    if (
      options.editorVersion !== undefined &&
      (typeof options.editorVersion !== 'string' || options.editorVersion.trim().length === 0)
    ) {
      throw new Error('editorVersion, when provided, must be a non-empty string.');
    }

    const projectPath = path.resolve(options.projectPath.trim());
    resolvedProjectPath = projectPath;

    emitProgress(options.onProgress, {
      phase: 'start',
      message: `Creating Unity project at ${projectPath}`,
    });

    // -- Unity Hub -------------------------------------------------------
    const findHub = options.findHubImpl ?? findUnityHub;
    const hubPath = options.hubPath ?? findHub();
    if (!hubPath) {
      throw new Error(
        'Unity Hub not found. Install it with: unity-mcp-cli install-unity [version]',
      );
    }

    // -- Installed editors -----------------------------------------------
    const listEditors = options.listEditorsImpl ?? queryInstalledEditors;
    const editors = listEditors(hubPath);

    emitProgress(options.onProgress, {
      phase: 'editors-located',
      message: editors.length > 0
        ? `Found ${editors.length} installed editor${editors.length !== 1 ? 's' : ''}`
        : 'No installed Unity editors found',
      found: editors.length > 0,
    });

    if (editors.length === 0) {
      throw new Error(
        'No Unity editors installed. Install one with: unity-mcp-cli install-unity [version]',
      );
    }

    // -- Version selection: explicit version wins, otherwise highest ------
    const requestedVersion = options.editorVersion?.trim();
    let editor: CreateProjectEditorInfo;
    if (requestedVersion) {
      const found = editors.find((e) => e.version === requestedVersion);
      if (!found) {
        throw new Error(
          `Unity Editor ${requestedVersion} not found. ` +
          `Installed versions: ${editors.map((e) => e.version).join(', ')}`,
        );
      }
      editor = found;
    } else {
      editor = findHighestEditor(editors);
      warnings.push(`No Unity version specified — using highest installed: ${editor.version}`);
    }
    resolvedVersion = editor.version;

    // -- Executable resolution ---------------------------------------------
    const resolveExe = options.resolveEditorExecutableImpl ?? defaultResolveEditorExecutable;
    const editorExe = resolveExe(editor.path);
    if (!editorExe) {
      throw new Error(
        `Unity Editor executable not found for ${editor.version} (install path: ${editor.path})`,
      );
    }
    resolvedEditorExe = editorExe;

    emitProgress(options.onProgress, {
      phase: 'editor-resolved',
      message: `Resolved Unity Editor at ${editorExe}`,
      editorPath: editorExe,
      version: editor.version,
    });

    // -- Create the project -------------------------------------------------
    // Only a finite positive integer within Node's timer range is a valid
    // execFile timeout; anything else (0, negative, NaN, Infinity, a
    // fractional value which execFile rejects with ERR_OUT_OF_RANGE, or a
    // value above Node's max timer delay of 2^31-1 which would emit a
    // TimeoutOverflowWarning and clamp to 1ms — killing the editor almost
    // immediately) falls back to the default.
    const timeoutMs =
      options.timeoutMs !== undefined &&
      Number.isInteger(options.timeoutMs) &&
      options.timeoutMs > 0 &&
      options.timeoutMs <= 2147483647
        ? options.timeoutMs
        : DEFAULT_CREATE_TIMEOUT_MS;

    emitProgress(options.onProgress, {
      phase: 'creating-project',
      message: `Creating Unity project at ${projectPath} with Unity Editor ${editor.version}`,
      projectPath,
      editorPath: editorExe,
      version: editor.version,
    });

    const runEditor = options.runEditorImpl ?? runEditorCreateProject;
    await runEditor(editorExe, projectPath, timeoutMs);

    emitProgress(options.onProgress, {
      phase: 'done',
      message: 'Project created.',
    });

    return {
      kind: 'success',
      success: true,
      projectPath,
      editorVersion: editor.version,
      editorPath: editorExe,
      warnings,
    };
  } catch (err: unknown) {
    const errorObj = err instanceof Error ? err : new Error(String(err));
    return {
      kind: 'failure',
      success: false,
      projectPath: resolvedProjectPath,
      editorVersion: resolvedVersion,
      editorPath: resolvedEditorExe,
      warnings,
      errorMessage: errorObj.message,
      error: errorObj,
    };
  }
}
