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
using System;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>Vector3Int (tilemap cells, grid coordinates): compact "(x, y, z)" output; "(…)" / [ … ] / object literal input, whole numbers only.</summary>
    public sealed class UnityEngine_Vector3Int_Adapter : NumericTupleAdapter<Vector3Int>
    {
        public override string Description => "Vector3Int: write as \"(x, y, z)\" or [x, y, z] with whole numbers";
        protected override string Expected => "(x, y, z) or [x, y, z] with whole numbers";
        protected override int Width => 3;
        protected override float Part(Vector3Int v, int i) => i switch { 0 => v.x, 1 => v.y, _ => v.z };
        protected override float Tolerance => 0;

        protected override Vector3Int? Make(float[] f)
        {
            if (f.Length != 3) return null;
            foreach (var x in f) if (Math.Abs(x - Math.Round(x)) > 1e-6) return null;
            return new Vector3Int((int)f[0], (int)f[1], (int)f[2]);
        }
    }
}
