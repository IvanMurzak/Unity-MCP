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
using Unity.Mathematics;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics
{
    /// <summary>float2 (Unity.Mathematics, used by splines): compact "(…)" output; "(…)" / [ … ] / object literal input.</summary>
    public sealed class Unity_Mathematics_float2_Adapter : NumericTupleAdapter<float2>
    {
        public override string Description => "float2 (Unity.Mathematics): write as \"(x, y)\", [x, y] or {x: 1, y: 2}";
        protected override string Expected => "(x, y) or [x, y]";
        protected override int Width => 2;
        protected override float Part(float2 v, int i) => i == 0 ? v.x : v.y;
        protected override float2? Make(float[] f) => f.Length == 2 ? new float2(f[0], f[1]) : (float2?)null;
    }
}
