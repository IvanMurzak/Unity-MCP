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
    /// <summary>Vector2Int (tilemap cells, grid coordinates): compact "(x, y)" output; "(…)" / [ … ] / object literal input, whole numbers only.</summary>
    public sealed class UnityEngine_Vector2Int_Adapter : NumericTupleAdapter<Vector2Int>
    {
        public override string Description => "Vector2Int: write as \"(x, y)\" or [x, y] with whole numbers";
        protected override string Expected => "(x, y) or [x, y] with whole numbers";
        protected override int Width => 2;
        protected override float Part(Vector2Int v, int i) => i switch { 0 => v.x, _ => v.y };
        protected override float Tolerance => 0;

        protected override Vector2Int? Make(float[] f)
        {
            if (f.Length != 2) return null;
            foreach (var x in f) if (Math.Abs(x - Math.Round(x)) > 1e-6) return null;
            return new Vector2Int((int)f[0], (int)f[1]);
        }
    }
}
