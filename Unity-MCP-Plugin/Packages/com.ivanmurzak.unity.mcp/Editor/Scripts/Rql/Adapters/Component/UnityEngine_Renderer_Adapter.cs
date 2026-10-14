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
    /// <summary>Renderer: material(s) instantiate copies when read — explicit-only; sharedMaterials returns a copy — written back.</summary>
    public sealed class UnityEngine_Renderer_Adapter : RqlTypeAdapter<Renderer>
    {
        protected override void Members(MemberTable<Renderer> m) => m.Untyped.ExplicitOnly("material", "materials").CopyOnRead("sharedMaterials");
    }
}
