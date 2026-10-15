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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The RQL engine over the live Editor state: engine factory, stable references, and the Editor integration for writes
    /// (one Undo group per mutation, SetDirty / scene dirty after). Sources, functions and adapters come from modules
    /// (<see cref="UnityCoreModule"/> plus any installed module). One engine per domain (static fields reset on domain
    /// reload). Every call must run on the main thread.
    /// </summary>
    public static partial class RqlUnity
    {
        /// <summary>Most assets <c>unity.assets(...)</c> loads per call (loading assets is expensive).</summary>
        public const int MaxAssets = 200;

        private static RqlEngine? _engine;

        public static RqlEngine Engine => _engine ??= Create();

        /// <summary>Drop the engine (tests, or after changing adapters).</summary>
        public static void Reset() => _engine = null;

        public static RqlEngine Create()
        {
            var engine = new RqlEngine(AccessorMode.Compiled) { RuntimeName = "unity" };
            engine.ResolveStableRef = ResolveStableRef;

            engine.Mutations.Enabled = true;
            // One Undo group per mutation: recorded owners, created / destroyed objects and added components undo together.
            int undoGroup = -1;
            engine.Mutations.BeforeWrite = records =>
            {
                if (records.Any(ChangesProjectState)) _wrote = true; // Mutate saves only after writes that change project state
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("RQL mutation");
                undoGroup = Undo.GetCurrentGroup();
                var owners = Owners(records);
                if (owners.Count > 0) Undo.RecordObjects(owners.ToArray(), "RQL mutation");
            };
            engine.Mutations.Committed = records =>
            {
                if (undoGroup >= 0) { Undo.CollapseUndoOperations(undoGroup); undoGroup = -1; }
                var scenes = new HashSet<Scene>();
                foreach (var o in Owners(records))
                {
                    EditorUtility.SetDirty(o);
                    var scene = o is GameObject go ? go.scene : o is Component c ? c.gameObject.scene : default;
                    if (scene.IsValid() && scenes.Add(scene) && !EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
                }
            };

            // Everything else — sources, adapters, exclusions — comes from modules: the core module in this package plus any
            // module in an installed extension package or an optional-package assembly (e.g. Particle System). TypeCache
            // already indexes them, so there is no assembly scan.
            var reports = engine.AddModules(TypeCache.GetTypesDerivedFrom<IRqlModule>());
            foreach (var failed in reports.Where(r => !r.Ok)) Debug.LogWarning($"[RQL] module skipped: {failed}");
            return engine;
        }

        /// <summary>False for writes to [RqlNoAutoSave] types and calls of [RqlNoAutoSave] methods (all overloads).</summary>
        internal static bool ChangesProjectState(MutationRecord record)
        {
            var type = record.Item?.GetType();
            if (type == null) return true;
            return !NoAutoSave.GetOrAdd((type, record.Method?.Name), key =>
            {
                if (key.type.IsDefined(typeof(RqlNoAutoSaveAttribute), false)) return true;
                if (key.method == null) return false;
                var overloads = key.type.GetMethods().Where(m => m.Name == key.method).ToList();
                return overloads.Count > 0 && overloads.All(m => m.IsDefined(typeof(RqlNoAutoSaveAttribute), false));
            });
        }

        // (type, called method or null for a write) -> it changes no project state; types and attributes are fixed per domain.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type type, string? method), bool> NoAutoSave =
            new System.Collections.Concurrent.ConcurrentDictionary<(Type, string?), bool>();

        private static List<Object> Owners(IReadOnlyList<MutationRecord> records)
        {
            var owners = new List<Object>();
            var seen = new HashSet<Object>();
            // The nearest Unity object enclosing each change: a write into a plain view object (e.g. a spline knot view) is
            // recorded on the component or asset that holds it.
            foreach (var r in records)
                foreach (var candidate in r.Owners)
                    if (candidate is Object o)
                    {
                        if (o != null && seen.Add(o)) owners.Add(o);
                        break;
                    }
            return owners;
        }

        // ------------------------------------------------------------ sources

        public static List<Scene> Scenes() => SceneUtils.GetAllOpenedScenes().ToList();

        public static List<Object> SelectedObjects() => Selection.objects.Where(o => o != null).ToList();

        public static List<GameObject> RootGameObjects()
        {
            var roots = new List<GameObject>();
            foreach (var s in Scenes()) roots.AddRange(s.GetRootGameObjects());
            return roots;
        }

        /// <summary>Every GameObject of the loaded scenes, depth-first, inactive ones included (one native call per root).</summary>
        public static List<GameObject> AllGameObjects()
        {
            var all = new List<GameObject>();
            var transforms = new List<Transform>();
            foreach (var root in RootGameObjects())
            {
                root.GetComponentsInChildren(true, transforms);
                foreach (var t in transforms) all.Add(t.gameObject);
            }
            return all;
        }

        // ------------------------------------------------------------ functions

        /// <summary><c>unity.assets("t:Material", folder?)</c>: assets matching an AssetDatabase filter (at most <see cref="MaxAssets"/>).</summary>
        public static object? FindAssets(object?[] args)
        {
            if (args.Length < 1 || !(args[0] is string filter)) return new RqlError("type-mismatch", "unity.assets(filter, folder?) expects a filter string such as \"t:Material\"");
            var folders = args.Length > 1 && args[1] is string folder ? new[] { folder } : null;
            var guids = folders == null ? AssetDatabase.FindAssets(filter) : AssetDatabase.FindAssets(filter, folders);
            return Load(guids);
        }

        // Lazy: a query that needs only a few assets (… | first 3) loads only those.
        private static IEnumerable<Object> Load(string[] guids)
        {
            foreach (var guid in guids.Take(MaxAssets))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) yield return asset;
            }
        }

        /// <summary>
        /// <c>unity.find("Player")</c> / <c>unity.find("Level/Pillar_1")</c>: the GameObject with that hierarchy path or that
        /// unique name in the loaded scenes (inactive ones included); an error when there is none or several.
        /// </summary>
        public static object? FindGameObject(object?[] args)
        {
            if (args.Length != 1 || !(args[0] is string key) || key.Length == 0)
                return new RqlError("type-mismatch", "unity.find(name) expects a GameObject name or hierarchy path such as \"Level/Pillar_1\"");
            // A path match ends with the object's own name: build paths only for those candidates.
            var candidates = AllGameObjects().Where(g => key.EndsWith(g.name, StringComparison.Ordinal)).ToList();
            var byPath = candidates.Where(g => g.GetPath() == key).ToList();
            var hits = byPath.Count > 0 ? byPath : candidates.Where(g => g.name == key).ToList();
            if (hits.Count == 1) return hits[0];
            return hits.Count == 0
                ? new RqlError("not-found", $"no GameObject named '{key}' in the loaded scenes")
                : new RqlError("ambiguous", $"{hits.Count} GameObjects are named '{key}': use the hierarchy path, e.g. \"{hits[0].GetPath()}\", or a ref");
        }

        /// <summary><c>unity.asset("Assets/…/Red.mat")</c>: the main asset at a path, or null.</summary>
        public static object? LoadAsset(object?[] args)
        {
            if (args.Length != 1 || !(args[0] is string path)) return new RqlError("type-mismatch", "unity.asset(path) expects an asset path");
            return AssetDatabase.LoadMainAssetAtPath(path);
        }

    }
}
