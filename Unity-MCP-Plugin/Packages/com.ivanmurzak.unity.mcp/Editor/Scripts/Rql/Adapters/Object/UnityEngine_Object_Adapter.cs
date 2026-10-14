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
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Every UnityEngine.Object: identity ($ref "Type#id", see the version-specific <c>IdOf</c>), fake-null objects read as
    /// null, summaries show the lowercase <c>name</c>, private [SerializeField] fields are members, [Obsolete] /
    /// [NonSerialized] members are hidden.
    /// </summary>
    public sealed partial class UnityEngine_Object_Adapter : RqlTypeAdapter
    {
        public UnityEngine_Object_Adapter() : base(typeof(Object)) { }

        public override string? StableId(object value) => value is Object o ? IdOf(o) : null;

        public override bool? IsAlive(object value) => value is Object o ? o != null : (bool?)null;

        public override void ConfigureMembers(MemberTable members) =>
            members
                // Include the private [SerializeField] fields first, so the [Obsolete] / [NonSerialized] rule hides them too.
                .IncludeFields(f => f.IsPrivate && f.IsDefined(typeof(UnityEngine.SerializeField), false))
                .HideWhere(m => m.Member != null && (m.Member.IsDefined(typeof(ObsoleteAttribute), true) || m.Member.IsDefined(typeof(NonSerializedAttribute), true)))
                .Hide("hideFlags")
                .Display("name");
    }
}
