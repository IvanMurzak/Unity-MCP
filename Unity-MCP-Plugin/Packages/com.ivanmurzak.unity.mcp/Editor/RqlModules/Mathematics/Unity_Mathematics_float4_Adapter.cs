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
    /// <summary>float4 (Unity.Mathematics, used by splines): compact "(…)" output; "(…)" / [ … ] / object literal input.</summary>
    public sealed class Unity_Mathematics_float4_Adapter : NumericTupleAdapter<float4>
    {
        public override string Description => "float4 (Unity.Mathematics): write as \"(x, y, z, w)\" or [x, y, z, w]";
        protected override string Expected => "(x, y, z, w) or [x, y, z, w]";
        protected override int Width => 4;
        protected override float Part(float4 v, int i) => i switch { 0 => v.x, 1 => v.y, 2 => v.z, _ => v.w };
        protected override float4? Make(float[] f) => f.Length == 4 ? new float4(f[0], f[1], f[2], f[3]) : (float4?)null;
    }
}
