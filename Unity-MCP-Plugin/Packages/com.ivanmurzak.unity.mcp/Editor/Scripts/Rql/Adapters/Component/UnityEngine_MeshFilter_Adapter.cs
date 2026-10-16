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
    /// <summary>MeshFilter: mesh instantiates a copy when read — explicit-only.</summary>
    public sealed class UnityEngine_MeshFilter_Adapter : RqlTypeAdapter<MeshFilter>
    {
        protected override void Members(MemberTable<MeshFilter> m) => m.Untyped.ExplicitOnly("mesh");
    }
}
