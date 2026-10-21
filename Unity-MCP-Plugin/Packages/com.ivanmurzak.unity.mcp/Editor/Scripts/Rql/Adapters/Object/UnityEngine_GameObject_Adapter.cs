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
        public override string Description => "GameObject: navigate with components / children; write active (SetActive), name, tag, layer";

        protected override void Members(MemberTable<GameObject> m)
        {
            m.Add("components", g => Components(g),
                    description: "attached components (filter with $ is Light etc.)", owning: true)
             .Add("children", g => Children(g), description: "direct child GameObjects", owning: true)
             .Add<bool>("active", g => g.activeSelf, (g, v) => g.SetActive(v), description: "activeSelf; assigning calls SetActive")
             .Add<List<string>>("staticFlags", g => StaticFlags(g), (g, v) => SetStaticFlags(g, v), ValidateStaticFlags,
                 "Static Editor Flags (" + StaticFlagList + "): filter with staticFlags contains \"ContributeGI\"; " +
                 "assign a list, e.g. [\"ContributeGI\", \"BatchingStatic\"] ([] clears; isStatic sets them all)")
             .Function<string, Component>("AddComponent", (g, type) => AddComponent(g, type), "type",
                 "add a component by type name (e.g. \"Rigidbody\", \"Light\", \"BoxCollider\", a script class) and return it")
             .Method("Destroy", g => Undo.DestroyObjectImmediate(g), "destroy this GameObject (undoable)")
             .Function("Duplicate", g => Duplicate(g), "copy this GameObject next to it (prefab instances stay linked) and return the copy (undoable)")
             .Function<string, GameObject>("SaveAsPrefab", (g, path) => SaveAsPrefab(g, path), "path",
                 "save as a prefab asset (\"Assets/…/X.prefab\"); this GameObject becomes its instance; returns the prefab asset");
            // staticFlags is built on read, so element writes are assigned back.
            m.Untyped.Reference("transform").Hide("gameObject").CopyOnRead("staticFlags");
        }

        private static Component AddComponent(GameObject g, string typeName)
        {
            var type = UnityTypes.Find(typeName, typeof(Component))
                       ?? throw new ArgumentException($"no component type named '{typeName}'");
            return Undo.AddComponent(g, type);
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
            var folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
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

        private static List<string> StaticFlags(GameObject g)
        {
            var set = GameObjectUtility.GetStaticEditorFlags(g);
            return StaticFlagNames.Where(f => (set & f.Flag) != 0).Select(f => f.Name).ToList();
        }

        private static StaticEditorFlags? StaticFlag(string? name)
        {
            foreach (var f in StaticFlagNames)
                if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) return f.Flag;
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
