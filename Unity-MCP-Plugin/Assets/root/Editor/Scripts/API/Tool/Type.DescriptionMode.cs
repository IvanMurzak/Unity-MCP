/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public enum DescriptionMode
    {
        /// <summary>
        /// Include descriptions on the target type and its direct properties/items.
        /// Descriptions inside <c>$defs</c> entries are excluded.
        /// </summary>
        Include,

        /// <summary>
        /// Include descriptions everywhere: the target type, its properties/items,
        /// and recursively inside all <c>$defs</c> entries.
        /// </summary>
        IncludeRecursively,

        /// <summary>
        /// Omit all <c>description</c> fields from the schema output.
        /// </summary>
        Ignore
    }
}
