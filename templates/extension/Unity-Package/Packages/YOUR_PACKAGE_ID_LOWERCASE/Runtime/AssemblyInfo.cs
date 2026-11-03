/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

// Unity logs "Assembly for Assembly Definition File ... will not be compiled, because it has no scripts associated
// with it" on every compilation for an assembly definition without scripts. This file keeps the extension's runtime
// assembly (which its Editor and test assemblies reference) a real, empty assembly; runtime code of the extension
// belongs in this folder.
