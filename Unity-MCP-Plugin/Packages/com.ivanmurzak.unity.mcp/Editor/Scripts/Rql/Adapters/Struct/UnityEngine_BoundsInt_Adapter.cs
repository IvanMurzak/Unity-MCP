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
    /// <summary>
    /// BoundsInt (Tilemap.cellBounds, grid regions): only min, max (exclusive) and size. The reflected members would also expose
    /// allPositionsWithin, an enumerator over every cell.
    /// </summary>
    public sealed class UnityEngine_BoundsInt_Adapter : RqlTypeAdapter<BoundsInt>
    {
        public override string Description => "BoundsInt: min, max (exclusive: the last cell is max - 1) and size, each \"(x, y, z)\"";

        protected override void Members(MemberTable<BoundsInt> m) => m.Only("min", "max", "size");
    }
}
