/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>Color: "(r, g, b, a)" output; "(r, g, b[, a])", [r, g, b[, a]], "#RRGGBB[AA]" or {r, g, b, a} input.</summary>
    public sealed class UnityEngine_Color_Adapter : NumericTupleAdapter<Color>
    {
        public override string Description => "Color: write as \"(r, g, b, a)\" with 0..1 components, [r, g, b] or \"#RRGGBB\"";
        protected override string Expected => "(r, g, b, a), [r, g, b] or #RRGGBB";
        protected override int Width => 4;
        protected override float Part(Color c, int i) => i switch { 0 => c.r, 1 => c.g, 2 => c.b, _ => c.a };
        protected override Color? Make(float[] f) => f.Length == 4 ? new Color(f[0], f[1], f[2], f[3]) : f.Length == 3 ? new Color(f[0], f[1], f[2]) : (Color?)null;
        protected override Color? ParseExtra(string text) =>
            text.TrimStart().StartsWith("#", System.StringComparison.Ordinal) && ColorUtility.TryParseHtmlString(text.Trim(), out var c) ? c : (Color?)null;
    }
}
