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
using System.Collections.Generic;
using System.ComponentModel;
using AIGD;

namespace AIGD
{
    public class CopyAssetsResponse
    {
        [Description("List of copied assets.")]
        public List<AssetObjectRef>? CopiedAssets { get; set; }
        [Description("List of errors encountered during copy operations.")]
        public List<string>? Errors { get; set; }
    }
}
