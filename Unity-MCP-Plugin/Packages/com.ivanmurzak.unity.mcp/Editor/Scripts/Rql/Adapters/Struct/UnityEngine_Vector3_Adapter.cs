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
    /// <summary>Vector3: compact "(…)" output; "(…)" / [ … ] / object literal input.</summary>
    public sealed class UnityEngine_Vector3_Adapter : NumericTupleAdapter<Vector3>
    {
        public override string Description => "Vector3: write as \"(x, y, z)\", [x, y, z] or {x: 1, y: 2, z: 3}";
        protected override string Expected => "(x, y, z) or [x, y, z]";
        protected override int Width => 3;
        // Component-wise arithmetic: Scale * 1.5, Position + [0, 1, 0], -Velocity (RQL spec §4.2).
        protected override bool Arithmetic => true;
        protected override float Part(Vector3 v, int i) => i switch { 0 => v.x, 1 => v.y, _ => v.z };
        protected override Vector3? Make(float[] f) => f.Length == 3 ? new Vector3(f[0], f[1], f[2]) : (Vector3?)null;
    }
}
