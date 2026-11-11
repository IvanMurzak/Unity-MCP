// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

const created: string[] = [];

/**
 * A fresh directory under the OS temp dir, deleted by the next {@link removeTempDirs}.
 * A test file that uses it registers `afterEach(removeTempDirs)`, so the directory is removed
 * even when the test fails (a shared `/tmp` is often a size-capped RAM disk).
 */
export function makeTempDir(prefix: string): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), prefix));
  created.push(dir);
  return dir;
}

/** Delete every directory {@link makeTempDir} created since the last call. */
export function removeTempDirs(): void {
  for (const dir of created.splice(0)) fs.rmSync(dir, { recursive: true, force: true });
}
