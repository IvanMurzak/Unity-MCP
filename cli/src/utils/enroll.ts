// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * Agent-driven enrollment now lives in `@baizor/gamedev-cli-core` (auth-fixes T3/T7): ONE engine-
 * agnostic `enroll` flow — redeem a one-time code, plant the plugin credential in the SHARED machine
 * store, record the AS-root server target in the committable project marker (MED-2), and upsert the
 * `/p/<pin>` routing segment into existing project-local agent configs.
 *
 * Two things changed vs. the old Unity-CLI-local port:
 *   - The pin is derived with **v2** identity (`derivePinV2`, the `\`→`/` normalization). This
 *     REPLACES the Unity CLI's local `projectRootForIdentity` `\`→`/` workaround — one algorithm for
 *     every engine, so a Windows `path.resolve` backslash root matches the plugin's forward-slash
 *     hash. `projectRootForIdentity` is therefore gone.
 *   - `runEnroll` takes an `adapter` (pass `unityAdapter`); it records the AS root via
 *     `adapter.loginServerTarget`, never a pinned hub URL.
 */

export {
  redeemEnrollmentCode,
  normalizeRedeemResponse,
  resolveEnrollCode,
  upsertProjectPinIntoConfigs,
  runEnroll,
  EnrollmentError,
  pinUrl,
} from '@baizor/gamedev-cli-core';

export type {
  RedeemedCredential,
  RedeemOptions,
  RunEnrollOptions,
  RunEnrollResult,
  PinUpsertResult,
} from '@baizor/gamedev-cli-core';
