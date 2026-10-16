// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * Project-identity derivation (routing pin + deterministic local port) now lives in
 * `@baizor/gamedev-cli-core` (auth-fixes T3/T7): ONE port of the C# `ProjectIdentity`, gated by the
 * SAME golden vectors as the .NET reference. This module re-exports the **v1** algorithm under the
 * historical names the CLI has always used, so existing call sites (`utils/config.ts`,
 * `utils/connection.ts`) and the golden-vector parity tests keep matching byte-for-byte.
 *
 * The **v2** algorithm (the `\`→`/` normalization that fixes B5) is what the configurators emit —
 * `setup-mcp` / `enroll` derive their pins with `derivePinV2` inside cli-core; import `derivePinV2` /
 * `derivePortV2` directly from `@baizor/gamedev-cli-core` when the v2 pin is needed.
 */

export {
  derivePin as deriveProjectPin,
  derivePort as generatePortFromDirectory,
  normalize as normalizeProjectRoot,
} from '@baizor/gamedev-cli-core';
