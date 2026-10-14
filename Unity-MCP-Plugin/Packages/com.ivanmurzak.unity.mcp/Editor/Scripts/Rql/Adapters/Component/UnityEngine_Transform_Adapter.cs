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
    /// Transform: parent / root are references. It is an object although it enumerates its children (UnityCoreModule
    /// registers TreatAsObject); a GameObject is accepted where a Transform is expected (the Component adapter's rule).
    /// </summary>
    public sealed class UnityEngine_Transform_Adapter : RqlTypeAdapter<Transform>
    {
        protected override void Members(MemberTable<Transform> m) => m.Untyped.Reference("parent", "root");
    }
}
