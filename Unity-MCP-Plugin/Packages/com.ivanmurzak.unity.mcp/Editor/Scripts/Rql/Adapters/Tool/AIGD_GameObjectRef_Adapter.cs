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
using AIGD;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// GameObjectRef tool arguments in RQL terms: a GameObject from <c>ref("GameObject#12")</c> / <c>unity.find("Name")</c>, a
    /// component (its GameObject), or text — a hierarchy path when it contains '/', otherwise a name.
    /// </summary>
    public sealed class AIGD_GameObjectRef_Adapter : RqlTypeAdapter
    {
        public AIGD_GameObjectRef_Adapter() : base(typeof(GameObjectRef)) { }

        public override string? Description =>
            "a GameObject: ref(\"GameObject#12\") from a query, unity.find(\"Name\"), or its name / hierarchy path as text (\"Root/Child\")";

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            reason = null;
            result = value switch
            {
                GameObject go => new GameObjectRef(go),
                Component c => new GameObjectRef(c.gameObject),
                string s when s.Contains("/") => new GameObjectRef { Path = s },
                string s => new GameObjectRef { Name = s },
                _ => null,
            };
            return result != null ? AdapterOutcome.Ok : AdapterOutcome.NotHandled;
        }
    }
}
