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
    /// <summary>MonoBehaviour: hide engine bookkeeping members so scripts show their own fields.</summary>
    public sealed class UnityEngine_MonoBehaviour_Adapter : RqlTypeAdapter<MonoBehaviour>
    {
        protected override void Members(MemberTable<MonoBehaviour> m) => m.Untyped.Hide("useGUILayout", "runInEditMode").HideExisting("didStart", "didAwake");
    }
}
