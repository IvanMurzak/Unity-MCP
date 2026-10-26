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
        public override string Description => "GameObject: navigate with components / children; write active (SetActive), name, tag, layer, staticFlags";

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
             .Add<int>("layer", g => g.layer, (g, v) => g.layer = v, (g, v) => ValidateLayer(v), "layer index 0..31")
             .Add<List<string>>("staticFlags", g => StaticFlags(g), (g, v) => SetStaticFlags(g, v), ValidateStaticFlags,
                 "Static Editor Flags (" + StaticFlagList + "): filter with staticFlags contains \"ContributeGI\"; " +
                 "assign a list, e.g. [\"ContributeGI\", \"BatchingStatic\"] ([] clears; isStatic sets them all)")
             .Function<string, Component>("AddComponent", (g, type) => AddComponent(g, type), "type",
                 "add a component by type name (e.g. \"Rigidbody\", \"Light\", \"BoxCollider\", a script class) and return it")
             // A target is already gone when an earlier target of the same call was its ancestor.
             .Method("Destroy", g => { if (g != null) RqlUnity.DestroyUndoable(g, $"Unity did not destroy '{g.name}'"); }, "destroy this GameObject (undoable)")
             .Function("Duplicate", g => Duplicate(g), "copy this GameObject next to it (prefab instances stay linked) and return the copy (undoable)")
             .Function<string, GameObject>("SaveAsPrefab", (g, path) => SaveAsPrefab(g, path), "path",
                 "save as a new prefab asset (\"Assets/…/X.prefab\", not an existing one); this GameObject becomes its instance; returns the prefab asset");
            // staticFlags is built on read, so element writes are assigned back.
            m.Untyped.Reference("transform").Hide("gameObject").CopyOnRead("staticFlags");
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

        // Undo.AddComponent returns null (and logs) instead of throwing when Unity refuses: a second component of a type that
        // allows one per GameObject, a conflicting component (Rigidbody2D next to a Rigidbody) or a type that cannot be added.
        private static Component AddComponent(GameObject g, string typeName)
        {
            var type = UnityTypes.Find(typeName, typeof(Component))
                       ?? throw new ArgumentException($"no component type named '{typeName}'");
            if (type == typeof(Transform)) throw new ArgumentException("a Transform cannot be added: every GameObject has exactly one");
            return Undo.AddComponent(g, type)
                   ?? throw new InvalidOperationException($"Unity did not add {type.Name} to '{g.name}': it allows one per GameObject, " +
                                                          "conflicts with a component already there, or cannot be added (the Console has the reason)");
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
            // An existing prefab would be replaced with this GameObject: every instance of it would change.
            path = RqlEditorApi.NewAssetPath(path, ".prefab");
            var folder = RqlEditorApi.Normalize(System.IO.Path.GetDirectoryName(path) ?? "");
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder)) throw new ArgumentException($"folder '{folder}' does not exist (editor | call CreateFolder(\"{folder}\"))");
            return PrefabUtility.SaveAsPrefabAssetAndConnect(g, path, InteractionMode.UserAction, out var ok) is GameObject asset && ok
                ? asset : throw new ArgumentException($"could not save a prefab at '{path}'");
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
