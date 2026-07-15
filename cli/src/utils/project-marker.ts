// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as path from 'path';
import { MACHINE_STORE_DIR_NAME } from './machine-credentials.js';

/**
 * The tool-neutral, NON-SECRET, committable project marker at
 * `<project>/.ai-game-dev/project.json` (design 06/09, D15). It records the enrolled server
 * target (hosted vs local) and the optional user port override so ProjectIdentity resolution and
 * every config writer (engine UI, CLIs, `configure`) agree on one source of truth. Credentials
 * NEVER go here — those live only in the machine credential store (`credentials.json`).
 */

export const PROJECT_MARKER_FILE = 'project.json';

export interface ProjectMarker {
  /** The server the project is enrolled against (hosted `https://ai-game.dev` or a local URL). */
  serverTarget?: string;
  /** User's explicit local-port override (wins over the deterministic derived port). */
  portOverride?: number;
  /** Unknown fields are preserved on read/merge for forward-compatibility. */
  [key: string]: unknown;
}

export function projectMarkerDir(projectPath: string): string {
  return path.join(projectPath, MACHINE_STORE_DIR_NAME);
}

export function projectMarkerPath(projectPath: string): string {
  return path.join(projectMarkerDir(projectPath), PROJECT_MARKER_FILE);
}

/** Read the marker, or null when absent/unparsable. */
export function readProjectMarker(projectPath: string): ProjectMarker | null {
  const markerPath = projectMarkerPath(projectPath);
  if (!fs.existsSync(markerPath)) return null;
  try {
    const parsed = JSON.parse(fs.readFileSync(markerPath, 'utf-8')) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return null;
    return parsed as ProjectMarker;
  } catch {
    return null;
  }
}

/**
 * Merge `marker` into any existing marker and write it back (creating the `.ai-game-dev/`
 * directory as needed). Idempotent for the same inputs; preserves pre-existing keys. Returns the
 * absolute marker path.
 */
export function writeProjectMarker(projectPath: string, marker: ProjectMarker): string {
  const dir = projectMarkerDir(projectPath);
  fs.mkdirSync(dir, { recursive: true });
  const merged: ProjectMarker = { ...(readProjectMarker(projectPath) ?? {}), ...marker };
  const markerPath = projectMarkerPath(projectPath);
  fs.writeFileSync(markerPath, JSON.stringify(merged, null, 2) + '\n');
  return markerPath;
}
