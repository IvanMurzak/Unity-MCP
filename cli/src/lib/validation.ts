import * as fs from 'fs';
import * as path from 'path';

/**
 * Small, side-effect-free input-validation helpers shared by the
 * library-facing API functions. Each helper returns a discriminated
 * union so call sites can pattern-match without throwing across the
 * public boundary.
 */

export type ValidatedPath =
  | { ok: true; projectPath: string }
  | { ok: false; error: Error };

export type ValidatedUnityProject =
  | { ok: true; projectPath: string; manifestPath: string }
  | { ok: false; manifestPath?: string; error: Error };

/**
 * Require a non-empty string and resolve it to an absolute path.
 */
export function requireProjectPath(raw: unknown): ValidatedPath {
  if (typeof raw !== 'string' || raw.length === 0) {
    return {
      ok: false,
      error: new Error('unityProjectPath is required and must be a non-empty string.'),
    };
  }
  return { ok: true, projectPath: path.resolve(raw) };
}

/**
 * Require a non-empty path AND that the path hosts a Unity project
 * (identified by the presence of `Packages/manifest.json`).
 */
export function requireUnityProject(raw: unknown): ValidatedUnityProject {
  const outer = requireProjectPath(raw);
  if (!outer.ok) return outer;
  const manifestPath = path.join(outer.projectPath, 'Packages', 'manifest.json');
  if (!fs.existsSync(manifestPath)) {
    return {
      ok: false,
      manifestPath,
      error: new Error(
        `Not a valid Unity project (missing Packages/manifest.json): ${outer.projectPath}`,
      ),
    };
  }
  return { ok: true, projectPath: outer.projectPath, manifestPath };
}

/**
 * Require that the given path exists on disk (does not need to host a
 * Unity manifest). Used by `configure()` which only needs a directory
 * to drop a config file into.
 */
export function requireExistingPath(raw: unknown): ValidatedPath {
  const outer = requireProjectPath(raw);
  if (!outer.ok) return outer;
  if (!fs.existsSync(outer.projectPath)) {
    return {
      ok: false,
      error: new Error(`Project path does not exist: ${outer.projectPath}`),
    };
  }
  return outer;
}
