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
    /// Renderer: reading Unity's material(s) in the Editor instantiates material copies into the scene (a read that changes
    /// the scene), so <c>material</c> / <c>materials</c> read and write the shared materials, as an Inspector edit does.
    /// The material arrays are copies — element writes are assigned back.
    /// </summary>
    public sealed class UnityEngine_Renderer_Adapter : RqlTypeAdapter<Renderer>
    {
        protected override void Members(MemberTable<Renderer> m) => m
            .Add("material", r => r.sharedMaterial, (r, v) => r.sharedMaterial = v, description: "same as sharedMaterial")
            .Add("materials", r => r.sharedMaterials, (r, v) => r.sharedMaterials = v, description: "same as sharedMaterials")
            .CopyOnRead("sharedMaterials", "materials");
    }
}
