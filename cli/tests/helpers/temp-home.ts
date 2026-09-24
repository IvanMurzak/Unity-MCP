// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

/** The env vars `os.homedir()` / cli-core's agent registry read the user's home from. */
const HOME_VARS = ['HOME', 'USERPROFILE', 'APPDATA'] as const;

export interface TempHome {
  dir: string;
  /** `process.env` with the home variables pointed at {@link dir} — pass to a child process. */
  env: NodeJS.ProcessEnv;
  /** Point THIS process's home variables at {@link dir} (undone by {@link dispose}). */
  redirect(): void;
  /** Restore any redirected variables and delete {@link dir}. */
  dispose(): void;
}

/** A fresh, empty home directory, so a test never reads or writes the real one. */
export function createTempHome(): TempHome {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-cli-home-'));
  const vars = { HOME: dir, USERPROFILE: dir, APPDATA: path.join(dir, 'AppData', 'Roaming') };
  let saved: Record<string, string | undefined> | undefined;
  return {
    dir,
    env: { ...process.env, ...vars },
    redirect() {
      saved = Object.fromEntries(HOME_VARS.map((k) => [k, process.env[k]]));
      Object.assign(process.env, vars);
    },
    dispose() {
      if (saved) {
        for (const k of HOME_VARS) {
          if (saved[k] === undefined) delete process.env[k];
          else process.env[k] = saved[k];
        }
      }
      fs.rmSync(dir, { recursive: true, force: true });
    },
  };
}

/** Antigravity's two config files — it reads either one, depending on the install. */
export function antigravityConfigPaths(homeDir: string): string[] {
  return [
    path.join(homeDir, '.gemini', 'config', 'mcp_config.json'),
    path.join(homeDir, '.gemini', 'antigravity', 'mcp_config.json'),
  ];
}
