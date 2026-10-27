// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * Project-identity derivation (routing pin + deterministic local port) now lives in
 * `@baizor/gamedev-cli-core` is the shared port of the C# `ProjectIdentity`. The Unity plugin and
 * `setup-mcp` use v2, which also normalizes Windows separators. Keep the CLI's historical names
 * on that same version so status, wait-for-ready, and fallback connections probe the plugin's port.
 */

export {
  derivePinV2 as deriveProjectPin,
  derivePortV2 as generatePortFromDirectory,
  normalizeV2 as normalizeProjectRoot,
} from '@baizor/gamedev-cli-core';
