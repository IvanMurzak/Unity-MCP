// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * The shared machine credential store now lives in `@baizor/gamedev-cli-core` (auth-fixes T7 / b2):
 * ONE implementation of `~/.ai-game-dev/credentials.json` across the three engine CLIs and the C#
 * plugin — atomic, corruption-safe writes (temp + fsync + rename), DPAPI on Windows / `0600`-`0700`
 * on POSIX, plus the `rotate()` used by the proactive refresh loop. This module is a thin re-export
 * so the rest of the CLI keeps importing from a stable local path.
 */

export {
  MachineCredentialStore,
  MACHINE_STORE_DIR_NAME,
  CREDENTIALS_FILE_NAME,
  CREDENTIALS_SCHEMA_VERSION,
} from '@baizor/gamedev-cli-core';

export type { MachineCredentials } from '@baizor/gamedev-cli-core';
