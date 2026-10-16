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
    /// [NonSerialized] members are hidden, and an asset path is accepted wherever an asset is expected.
    /// </summary>
    public sealed partial class UnityEngine_Object_Adapter : RqlTypeAdapter
    {
        public UnityEngine_Object_Adapter() : base(typeof(Object)) { }

        public override string? StableId(object value) => value is Object o ? IdOf(o) : null;

        public override bool? IsAlive(object value) => value is Object o ? o != null : (bool?)null;

        /// <summary>Text naming a project asset ("Assets/…" or "Packages/…", either slash), normalized to forward slashes.</summary>
        internal static bool TryAssetPath(string text, out string path)
        {
            path = text.Replace('\\', '/');
            return path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Wherever an asset is expected, its path works too: <c>set sharedMaterial = "Assets/Mats/Red.mat"</c>,
        /// <c>call SetTile("(0, 0, 0)", "Assets/Tiles/Grass.asset")</c>. Sub-assets of the requested type are found as well.
        /// </summary>
        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is string text) || !TryAssetPath(text, out var path)) return AdapterOutcome.NotHandled;
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(path, target);
            if (asset == null)
                foreach (var sub in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (target.IsInstanceOfType(sub)) { asset = sub; break; }
            if (asset != null) { result = asset; return AdapterOutcome.Ok; }
            var main = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            reason = main == null ? $"no asset at '{path}'" : $"'{path}' is a {main.GetType().Name}, expected {target.Name}";
            return AdapterOutcome.Failed;
        }

        public override void ConfigureMembers(MemberTable members) =>
            members
                // Include the private [SerializeField] fields first, so the [Obsolete] / [NonSerialized] rule hides them too.
                .IncludeFields(f => f.IsPrivate && f.IsDefined(typeof(UnityEngine.SerializeField), false))
                .HideWhere(m => m.Member != null && (m.Member.IsDefined(typeof(ObsoleteAttribute), true) || m.Member.IsDefined(typeof(NonSerializedAttribute), true)))
                .Hide("hideFlags")
                .Display("name");
    }
}
