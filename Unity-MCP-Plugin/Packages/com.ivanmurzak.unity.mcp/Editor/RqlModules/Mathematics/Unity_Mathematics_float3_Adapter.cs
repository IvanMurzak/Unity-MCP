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
    /// <summary>float3 (Unity.Mathematics, used by splines): compact "(…)" output; "(…)" / [ … ] / object literal input.</summary>
    public sealed class Unity_Mathematics_float3_Adapter : NumericTupleAdapter<float3>
    {
        public override string Description => "float3 (Unity.Mathematics): write as \"(x, y, z)\", [x, y, z] or {x: 1, y: 2, z: 3}";
        protected override string Expected => "(x, y, z) or [x, y, z]";
        protected override int Width => 3;
        protected override float Part(float3 v, int i) => i switch { 0 => v.x, 1 => v.y, _ => v.z };
        protected override float3? Make(float[] f) => f.Length == 3 ? new float3(f[0], f[1], f[2]) : (float3?)null;
    }
}
