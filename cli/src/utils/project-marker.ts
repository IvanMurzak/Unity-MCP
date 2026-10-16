// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * The tool-neutral, NON-SECRET, committable project marker (`<project>/.ai-game-dev/project.json`)
 * now lives in `@baizor/gamedev-cli-core` (auth-fixes T7): ONE implementation shared by the three
 * engine CLIs. It records the enrolled server target (hosted vs local) and the optional user port
 * override; credentials NEVER go here (those live only in the machine credential store). This module
 * is a thin re-export so the CLI keeps a stable local import path.
 */

export {
  readProjectMarker,
  writeProjectMarker,
  projectMarkerPath,
  projectMarkerDir,
  PROJECT_MARKER_FILE,
} from '@baizor/gamedev-cli-core';

export type { ProjectMarker } from '@baizor/gamedev-cli-core';
