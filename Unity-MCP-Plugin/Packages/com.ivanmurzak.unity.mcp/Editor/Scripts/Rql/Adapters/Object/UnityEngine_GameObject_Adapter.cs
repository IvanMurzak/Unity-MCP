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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// GameObject: components and children as members, <c>active</c> written through SetActive, <c>staticFlags</c> (the
    /// per-flag Static Editor Flags behind <c>isStatic</c>) through GameObjectUtility.
    /// </summary>
    public sealed class UnityEngine_GameObject_Adapter : RqlTypeAdapter<GameObject>
    {
        public override string Description => "GameObject: navigate with components / children; path is the hierarchy path; write active (SetActive), name, tag, layer / layerName, staticFlags";

        protected override void Members(MemberTable<GameObject> m)
        {
            m.Add("components", g => Components(g),
                    description: "attached components (filter with $ is Light etc.)", owning: true)
             .Add("children", g => Children(g), description: "direct child GameObjects", owning: true)
             .Add<bool>("active", g => g.activeSelf, (g, v) => g.SetActive(v), description: "activeSelf; assigning calls SetActive")
             // Unity rejects an unknown tag or a layer outside 0..31 by logging an error, not by throwing: checked before any
             // write, so the receipt never reports a change that did not happen and the other writes stay all-or-nothing.
             // An empty name is legal in Unity but a policy here: unity.find and paths cannot address such an object.
             .Add<string>("name", g => g.name, (g, v) => g.name = v, (g, v) => ValidateName(v), "the GameObject's name (not empty)")
             .Add<string>("tag", g => g.tag, (g, v) => g.tag = v, (g, v) => ValidateTag(v),
                 "tag: one of the project's tags (Untagged, MainCamera, Player … and the tags added in Tags and Layers)")
             .Add<int>("layer", g => g.layer, (g, v) => g.layer = v, (g, v) => ValidateLayer(v), "layer index 0..31 (by name: layerName)")
             .Add<string>("layerName", g => LayerMask.LayerToName(g.layer), (g, v) => g.layer = LayerMask.NameToLayer(v), (g, v) => ValidateLayerName(v),
                 "the layer's name, e.g. set layerName = \"Pickups\" (a defined layer; editor | call AddLayer(name) adds one)")
             .Add<string?>("path", g => g.GetPath(), description: "hierarchy path, e.g. \"Level/Props/Crate\" (for a scene object: what unity.find accepts; in a prefab: from its root)")
             .Add<List<string>>("staticFlags", g => StaticFlags(g), (g, v) => SetStaticFlags(g, v), ValidateStaticFlags,
                 "Static Editor Flags (" + StaticFlagList + "): filter with staticFlags contains \"ContributeGI\"; " +
                 "assign a list, e.g. [\"ContributeGI\", \"BatchingStatic\"] ([] clears; isStatic sets them all)")
             .Function<string, Component>("AddComponent", (g, type) => AddComponent(g, type), "type",
                 "add a component by type name (e.g. \"Rigidbody\", \"Light\", \"BoxCollider\", a script class) and return it",
                 ValidateAddComponent)
             // A target is already gone when an earlier target of the same call was its ancestor.
             .Method("Destroy", g => { if (g != null) RqlUnity.DestroyUndoable(g, $"Unity did not destroy '{g.name}'"); }, "destroy this GameObject (undoable)")
             .Function("Duplicate", g => Duplicate(g), "copy this GameObject next to it (prefab instances stay linked) and return the copy (undoable)")
             .Function<string, GameObject>("SaveAsPrefab", (g, path) => SaveAsPrefab(g, path), "path",
                 "save as a new prefab asset (\"Assets/…/X.prefab\", not an existing one; missing folders are created); this GameObject becomes its instance; returns the prefab asset",
                 ValidateSaveAsPrefab);
            // staticFlags is built on read, so element writes are assigned back.
            m.Untyped.Reference("transform").Hide("gameObject").CopyOnRead("staticFlags").ExplicitOnly("path", "layerName");
        }

        /// <summary>Null when <paramref name="name"/> is a usable GameObject name, otherwise why not.</summary>
        internal static string? ValidateName(string? name) =>
            string.IsNullOrWhiteSpace(name) ? "a GameObject name cannot be empty" : null;

        internal static string? ValidateTag(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "a tag cannot be empty (use \"Untagged\")";
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            return Array.IndexOf(tags, tag) >= 0 ? null : $"tag '{tag}' is not defined; defined tags: {string.Join(", ", tags)}";
        }

        internal static string? ValidateLayer(int layer) =>
            layer < 0 || layer > 31 ? $"layer {layer} is out of range: a layer index is 0..31" : null;

        internal static string? ValidateLayerName(string? name)
        {
            if (!string.IsNullOrEmpty(name) && LayerMask.NameToLayer(name) >= 0) return null;
            var defined = RqlEditorApi.LayerNames().Where(n => n.Length > 0);
            return $"layer '{name}' is not defined; defined layers: {string.Join(", ", defined)} (editor | call AddLayer(\"{name}\") adds it)";
        }

        // Undo.AddComponent returns null (and logs) instead of throwing when Unity refuses: a second component of a type that
        // allows one per GameObject, a conflicting component (Rigidbody2D next to a Rigidbody) or a type that cannot be added.
        // The call validator rejects the foreseeable cases for every target before the first add (dry runs included).
        private static Component AddComponent(GameObject g, string typeName)
        {
            if (ValidateAddComponent(g, typeName) is { } rejection) throw new ArgumentException(rejection); // a C# caller skips RQL's validator
            var type = UnityTypes.Find(typeName, typeof(Component))!;
            return Undo.AddComponent(g, type)
                   ?? throw new InvalidOperationException($"Unity did not add {type.Name} to '{g.name}': it allows one per GameObject, " +
                                                          "conflicts with a component already there, or cannot be added (the Console has the reason)");
        }

        internal static string? ValidateAddComponent(GameObject g, string typeName)
        {
            var type = UnityTypes.Find(typeName, typeof(Component));
            if (type == null) return $"no component type named '{typeName}'";
            if (type == typeof(Transform)) return "a Transform cannot be added: every GameObject has exactly one";
            if (g == null) return null;
            // Unity allows one Rigidbody / Rigidbody2D per GameObject natively, without [DisallowMultipleComponent].
            if ((type.IsDefined(typeof(DisallowMultipleComponent), true) || IsBody(type)) && g.GetComponent(type) != null)
                return $"'{g.name}' already has a {type.Name}, which allows one per GameObject";
            // 3D and 2D physics components cannot share a GameObject (by name: the 2D physics module may be disabled), nor
            // can the components the new one requires (a FixedJoint adds a Rigidbody).
            foreach (var added in WithRequired(type))
            {
                var physics = PhysicsFamily(added);
                if (physics == 0) continue;
                foreach (var existing in g.GetComponents<Component>())
                    if (existing != null && PhysicsFamily(existing.GetType()) == -physics)
                        return $"{type.Name} conflicts with {existing.GetType().Name} on '{g.name}': 3D and 2D physics cannot be mixed";
            }
            return null;
        }

        static bool IsBody(Type type) => type.FullName == "UnityEngine.Rigidbody" || type.FullName == "UnityEngine.Rigidbody2D";

        // The type and the components its [RequireComponent] attributes make Unity add with it (one level).
        static IEnumerable<Type> WithRequired(Type type)
        {
            yield return type;
            foreach (RequireComponent r in type.GetCustomAttributes(typeof(RequireComponent), true))
                foreach (var t in new[] { r.m_Type0, r.m_Type1, r.m_Type2 })
                    if (t != null) yield return t;
        }

        // 1 = 3D physics (Rigidbody, Collider), -1 = 2D physics (Rigidbody2D, Collider2D), 0 = neither.
        static int PhysicsFamily(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                switch (t.FullName)
                {
                    case "UnityEngine.Rigidbody": case "UnityEngine.Collider": return 1;
                    case "UnityEngine.Rigidbody2D": case "UnityEngine.Collider2D": return -1;
                }
            }
            return 0;
        }

        // The Editor's own Ctrl+D path: prefab instances stay linked with their overrides, names stay unique, undo is
        // registered by Unity. It works on the selection, which is restored afterwards.
        private static GameObject Duplicate(GameObject g)
        {
            var selection = Selection.objects;
            try
            {
                Selection.objects = new Object[] { g };
                Unsupported.DuplicateGameObjectsUsingPasteboard();
                return Selection.activeGameObject != null && Selection.activeGameObject != g
                    ? Selection.activeGameObject
                    : throw new InvalidOperationException($"could not duplicate '{g.name}'");
            }
            finally { Selection.objects = selection; }
        }

        private static GameObject SaveAsPrefab(GameObject g, string path)
        {
            path = PrefabPath(path);
            // Like CreateMaterial / CopyAsset / NewScene: missing folders are created, and removed again when the save fails.
            return RqlEditorApi.InFolderOf(path, () =>
                PrefabUtility.SaveAsPrefabAssetAndConnect(g, path, InteractionMode.UserAction, out var ok) is GameObject asset && ok
                    ? asset : throw new ArgumentException($"could not save a prefab at '{path}'"));
        }

        // The call validator: a new path (two targets saving to one path are refused by RqlUnity.ValidateBatch).
        private static string? ValidateSaveAsPrefab(GameObject g, string path) => RqlEditorApi.Rejection(() => PrefabPath(path));

        // A new ".prefab" path under Assets/ (missing folders are created by the call). An existing prefab would be replaced
        // with this GameObject: every instance of it would change.
        internal static string PrefabPath(string path)
        {
            path = RqlEditorApi.NewAssetPath(path, ".prefab");
            RqlEditorApi.EnsureFolder(System.IO.Path.GetDirectoryName(path), create: false);
            return path;
        }

        // The single-bit, non-obsolete flags of this Unity version by their current names: StaticEditorFlags has obsolete
        // aliases (LightmapStatic = ContributeGI), so the value's ToString() may print the old name.
        private static readonly (string Name, StaticEditorFlags Flag)[] StaticFlagNames = typeof(StaticEditorFlags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => !f.IsDefined(typeof(ObsoleteAttribute), false))
            .Select(f => (f.Name, Flag: (StaticEditorFlags)f.GetValue(null)))
            .Where(f => { var bits = Convert.ToInt64(f.Flag); return bits != 0 && (bits & (bits - 1)) == 0; })
            .GroupBy(f => f.Flag).Select(same => same.Last()) // an alias not marked obsolete: the later-declared name
            .ToArray();

        private static readonly StaticEditorFlags NamedStaticFlags = StaticFlagNames.Aggregate((StaticEditorFlags)0, (all, f) => all | f.Flag);

        private static string StaticFlagList => string.Join(", ", StaticFlagNames.Select(f => f.Name));

        // Read per GameObject by `where staticFlags contains …` over the whole scene: a plain loop, no LINQ.
        private static List<string> StaticFlags(GameObject g)
        {
            var set = GameObjectUtility.GetStaticEditorFlags(g);
            var names = new List<string>(StaticFlagNames.Length);
            foreach (var f in StaticFlagNames)
                if ((set & f.Flag) != 0) names.Add(f.Name);
            return names;
        }

        private static StaticEditorFlags? StaticFlag(string? name)
        {
            foreach (var f in StaticFlagNames)
                if (string.Equals(f.Name, name, StringComparison.Ordinal)) return f.Flag; // reads print these exact names
            return null;
        }

        // Checked before any write, so a list with one wrong name changes nothing.
        private static string? ValidateStaticFlags(GameObject g, List<string>? names)
        {
            if (names == null) return null;
            foreach (var name in names)
                if (StaticFlag(name) == null) return $"'{name}' is not a static editor flag; use {StaticFlagList}";
            return null;
        }

        // Only the named flags change: obsolete ones a GameObject still carries are kept as they are.
        private static void SetStaticFlags(GameObject g, List<string>? names)
        {
            var value = GameObjectUtility.GetStaticEditorFlags(g) & ~NamedStaticFlags;
            if (names != null) foreach (var name in names) value |= StaticFlag(name)!.Value;
            GameObjectUtility.SetStaticEditorFlags(g, value);
        }

        private static List<Component> Components(GameObject g)
        {
            var list = new List<Component>();
            g.GetComponents(list);
            list.RemoveAll(c => c == null); // missing scripts
            return list;
        }

        private static List<GameObject> Children(GameObject g)
        {
            var t = g.transform;
            var list = new List<GameObject>(t.childCount);
            for (int i = 0; i < t.childCount; i++) list.Add(t.GetChild(i).gameObject);
            return list;
        }
    }
}
