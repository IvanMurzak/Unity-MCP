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
using UnityEngine;
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

        /// <summary>
        /// Literal kinds (RQL spec §4.5): a data ScriptableObject is a value, built fresh from a typed literal
        /// (<c>set data = {$type: "ItemData", price: 3}</c>); every other UnityEngine.Object (GameObjects, components, other
        /// assets) stays an engine object that only a ref, <c>unity.find(…)</c> or an asset path passes. Material: its adapter.
        /// </summary>
        public override LiteralKind? LiteralKindOf(Type type) => IsDataScriptableObject(type) ? LiteralKind.Value : (LiteralKind?)null;

        public override object? CreateForLiteral(Type type, object? outer)
        {
            if (!IsDataScriptableObject(type)) return null;
            RefuseAssetOuter(type, outer, "create the asset first (e.g. script-execute AssetDatabase.CreateAsset) and pass its path");
            return ScriptableObject.CreateInstance(type);
        }

        /// <summary>
        /// A ScriptableObject that holds game data: not an Editor type (windows, inspectors, settings singletons) and not one
        /// of Unity's own (Timeline, Input System, render pipeline assets …), which have dedicated creation paths.
        /// </summary>
        internal static bool IsDataScriptableObject(Type type)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters) return false;
            if (typeof(UnityEditor.EditorWindow).IsAssignableFrom(type) || typeof(UnityEditor.Editor).IsAssignableFrom(type)) return false;
            for (var t = type; t != null && t != typeof(ScriptableObject); t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(UnityEditor.ScriptableSingleton<>)) return false;
            return !(type.Assembly.GetName().Name ?? "").StartsWith("Unity", StringComparison.Ordinal);
        }

        /// <summary>
        /// A new object written into an asset (a Material into a ScriptableObject asset's field) would not be saved with it:
        /// the asset would lose the reference on reload. Refused while the mutation is planned, so nothing changes.
        /// </summary>
        internal static void RefuseAssetOuter(Type type, object? outer, string advice)
        {
            if (outer is Object owner && owner != null && UnityEditor.EditorUtility.IsPersistent(owner))
                throw new InvalidOperationException($"a new {type.Name} written into the asset '{owner.name}' would not be saved with it; {advice}");
        }

        /// <summary>The advice ending RQL's engine-object refusal (spec §4.5): how to pass an existing object of that type.</summary>
        internal static string RefusalHint(Type type) =>
            typeof(GameObject).IsAssignableFrom(type) || typeof(Component).IsAssignableFrom(type)
                ? "use unity.find(\"Name\") or ref(\"Type#id\") for an object in the scene"
                : "use an asset path (\"Assets/…\") or unity.asset(\"Assets/…\") for an existing asset, or ref(\"Type#id\")";

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

        public override void ConfigureMembers(MemberTable members)
        {
            members
                // Include the private [SerializeField] fields first, so the [Obsolete] / [NonSerialized] rule hides them too.
                .IncludeFields(f => f.IsPrivate && f.IsDefined(typeof(UnityEngine.SerializeField), false))
                .HideWhere(m => m.Member != null && (m.Member.IsDefined(typeof(ObsoleteAttribute), true) || m.Member.IsDefined(typeof(NonSerializedAttribute), true)))
                .Hide("hideFlags")
                .Display("name");
            // Read only when named (it asks the AssetDatabase): unity.assets("t:Material") | select name assetPath. A type's own
            // assetPath member (AssetImporter, a script field) keeps its meaning.
            if (!members.Contains("assetPath"))
                members.Add("assetPath", typeof(string), o => AssetPathOf((Object)o), description: "the asset's project path (\"Assets/…/Red.mat\"); null for scene objects")
                       .ExplicitOnly("assetPath");
            // The import settings of an asset file (TextureImporter, ModelImporter, AudioImporter …): written like any object,
            // then saved and reimported by RqlUnity.SaveChanges.
            // Components are never asset files of their own.
            if (!members.Contains("importer") && !typeof(UnityEditor.AssetImporter).IsAssignableFrom(members.Type)
                && !typeof(UnityEngine.Component).IsAssignableFrom(members.Type))
                members.Add("importer", typeof(UnityEditor.AssetImporter), o => ImporterOf((Object)o),
                           description: "the asset file's import settings (TextureImporter, ModelImporter, AudioImporter …; writes are saved and the file reimported); null for scene objects")
                       .ExplicitOnly("importer");
        }

        private static string? AssetPathOf(Object o) =>
            o != null && UnityEditor.AssetDatabase.GetAssetPath(o) is { Length: > 0 } path ? path : null;

        // A sub-asset (a sprite of a sheet, a mesh inside an .fbx) shares its file's importer.
        private static UnityEditor.AssetImporter? ImporterOf(Object o) =>
            AssetPathOf(o) is { } path ? UnityEditor.AssetImporter.GetAtPath(path) : null;
    }
}
