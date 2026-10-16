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
    /// <summary>Quaternion: compact "(…)" output; "(…)" / [ … ] / object literal input.</summary>
    public sealed class UnityEngine_Quaternion_Adapter : NumericTupleAdapter<Quaternion>
    {
        public override string Description => "Quaternion: (x, y, z, w); prefer eulerAngles for rotations, e.g. set transform.eulerAngles = \"(0, 90, 0)\"";
        protected override string Expected => "(x, y, z, w) or [x, y, z, w]";
        protected override int Width => 4;
        protected override float Part(Quaternion q, int i) => i switch { 0 => q.x, 1 => q.y, 2 => q.z, _ => q.w };
        protected override Quaternion? Make(float[] f) => f.Length == 4 ? new Quaternion(f[0], f[1], f[2], f[3]) : (Quaternion?)null;
    }
}
