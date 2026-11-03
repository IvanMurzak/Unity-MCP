// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * Project-identity derivation (routing pin + deterministic local port) lives in
 * `@baizor/gamedev-cli-core` (auth-fixes T3/T7): ONE port of the C# `ProjectIdentity`, gated by the
 * SAME golden vectors as the .NET reference.
 *
 * The CLI uses the **v2** algorithm (trailing separators trimmed, `\` converted to `/`, invariant
 * lower-case) because that is what the Unity plugin uses for its local port
 * (`UnityMcpPlugin.GeneratePortFromDirectory` → `ProjectIdentity.DerivePortV2`) and for the pin the
 * configurators write. With v1, a Windows path such as `C:\Projects\Game` hashed to a different
 * port than the one the plugin listens on, so `status`, `run-tool` and the default local URL
 * pointed at a closed port.
 */

export {
  derivePinV2 as deriveProjectPin,
  derivePortV2 as generatePortFromDirectory,
  normalizeV2 as normalizeProjectRoot,
} from '@baizor/gamedev-cli-core';
