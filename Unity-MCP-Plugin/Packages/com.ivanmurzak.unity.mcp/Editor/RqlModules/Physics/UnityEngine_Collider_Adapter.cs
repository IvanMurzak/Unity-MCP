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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D
{
    /// <summary>Collider: <c>material</c> instantiates a copy, so it is explicit-only (<c>sharedMaterial</c> is the asset).</summary>
    public sealed class UnityEngine_Collider_Adapter : RqlTypeAdapter<Collider>
    {
        protected override void Members(MemberTable<Collider> m) => m.Untyped.ExplicitOnly("material");
    }
}
