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

        /// <summary>Targets one rql-mutate call may change without <c>max_targets</c>.</summary>
        public const int DefaultMaxTargets = 200;

        private static RqlEngine? _engine;

        public static RqlEngine Engine => _engine ??= Create();

        /// <summary>Drop the engine (tests, or after changing adapters).</summary>
        public static void Reset() => _engine = null;

        public static RqlEngine Create()
        {
            // ref("Name") hints name the finder, no-match hints the schema tool; the tools take no budgets argument (only
            // rql-mutate's max_targets, which the mutation-limit hint names), so no hint advises raising budgets.*.
            var engine = new RqlEngine(AccessorMode.Compiled)
            {
                RuntimeName = "unity",
                FindHint = "unity.find(\"…\")",
                SchemaHint = API.Tool_Rql.SchemaToolId,
                EnumAliases = UnityTypes.EnumAliases,
            };
            // Streams of components and assets are polymorphic: a member that some Unity type has is not a typo (it does not
            // fail an empty mutation), even when the types reachable from the sources (GameObject, Component …) lack it.
            engine.IsKnownMember = name => UnityTypes.IsKnownMember(engine, name);
            engine.Defaults.BudgetsAdjustable = false;
            engine.ResolveStableRef = ResolveStableRef;

            engine.Mutations.Enabled = true;
            // One mutation is one Undo step, so the limit guards breadth only (RQL spec §8: editor hosts default to ~200).
            engine.Mutations.MaxTargets = DefaultMaxTargets;
            engine.Mutations.LimitArgument = "max_targets";
            // Checks that span the targets of one set / call stage (two SaveAsPrefab to one path; a component removed with or
            // without the components that require it), before anything runs, dry runs included.
            engine.Mutations.ValidateBatch = ValidateBatch;
            // One Undo group per mutation: recorded owners, created / destroyed objects and added components undo together.
            engine.Mutations.BeforeWrite = records =>
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("RQL mutation");
                _undoGroup = Undo.GetCurrentGroup();
                var owners = Owners(records, out _);
                if (owners.Count > 0) Undo.RecordObjects(owners.ToArray(), "RQL mutation");
            };
            // Committed gets exactly the applied records: every set after all writes succeeded, the completed calls when a
            // later call threw. A set the engine rolled back never gets here, so it does not trigger Mutate's save.
            engine.Mutations.Committed = records =>
            {
                Destroying = null;
                if (records.Any(ChangesProjectState)) _wrote = true; // Mutate saves only after writes that change project state
                CloseUndoGroup();
                var scenes = new HashSet<Scene>();
                var owners = Owners(records, out var sceneSettings);
                foreach (var o in owners)
                {
                    if (sceneSettings != null && sceneSettings.Contains(o)) continue; // written through the active scene below
                    EditorUtility.SetDirty(o);
                    if (o is AssetImporter importer && InMutation) Reimports.Add(importer); // import settings apply on reimport (SaveChanges)
                    var scene = o is GameObject go ? go.scene : o is Component c ? c.gameObject.scene : default;
                    if (scene.IsValid() && scenes.Add(scene) && !EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
                }
                // Scene-level settings (IRqlUndoOwners) are written on the active scene only; the other loaded scenes' objects are
                // recorded for Undo but not dirtied, so their scenes are not saved for nothing.
                var active = SceneManager.GetActiveScene();
                if (sceneSettings != null && active.IsValid() && scenes.Add(active) && !EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(active);
            };

            // Everything else — sources, adapters, exclusions — comes from modules: the core module in this package plus any
            // module in an installed extension package or an optional-package assembly (e.g. Particle System). TypeCache
            // already indexes them, so there is no assembly scan.
            var reports = engine.AddModules(TypeCache.GetTypesDerivedFrom<IRqlModule>());
            foreach (var failed in reports.Where(r => !r.Ok)) Debug.LogWarning($"[RQL] module skipped: {failed}");
            return engine;
        }

        // The open "RQL mutation" Undo group (BeforeWrite), or -1.
        private static int _undoGroup = -1;

        /// <summary>
        /// Collapse the running mutation's Undo group into one step. Also called by <see cref="Mutate"/> when the engine
        /// never reached Committed (a rolled-back set, a first call that threw): Undo operations its setters / calls
        /// registered before the failure (e.g. Undo.SetTransformParent and its rollback) then form one step as well.
        /// </summary>
        private static void CloseUndoGroup()
        {
            if (_undoGroup < 0) return;
            Undo.CollapseUndoOperations(_undoGroup);
            _undoGroup = -1;
        }

        /// <summary>
        /// False for writes to [RqlNoAutoSave] types or [RqlNoAutoSave] properties / fields of the item (the first member of
        /// the written path) and for calls of [RqlNoAutoSave] methods (all overloads).
        /// </summary>
        internal static bool ChangesProjectState(MutationRecord record)
        {
            var type = record.Item?.GetType();
            if (type == null) return true;
            var isCall = record.Method != null;
            var member = isCall ? record.Method!.Name : FirstMember(record.Member);
            return !NoAutoSave.GetOrAdd((type, member, isCall), key =>
            {
                if (key.type.IsDefined(typeof(RqlNoAutoSaveAttribute), false)) return true;
                if (string.IsNullOrEmpty(key.member)) return false;
                if (!key.isCall)
                    return key.type.GetMember(key.member!, System.Reflection.MemberTypes.Property | System.Reflection.MemberTypes.Field,
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Any(m => m.IsDefined(typeof(RqlNoAutoSaveAttribute), false));
                var overloads = key.type.GetMethods().Where(m => m.Name == key.member).ToList();
                return overloads.Count > 0 && overloads.All(m => m.IsDefined(typeof(RqlNoAutoSaveAttribute), false));
            });
        }

        // "selection" / "transform.position" / "properties[\"_Color\"]" -> the member written on the item.
        private static string FirstMember(string path)
        {
            var end = path.IndexOfAny(MemberPathBreaks);
            return end < 0 ? path : path.Substring(0, end);
        }

        private static readonly char[] MemberPathBreaks = { '.', '[' };

        // (type, called method or first written member, call?) -> it changes no project state; fixed per domain.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type type, string? member, bool isCall), bool> NoAutoSave =
            new System.Collections.Concurrent.ConcurrentDictionary<(Type, string?, bool), bool>();

        /// <param name="sceneSettings">The owners named by an <see cref="IRqlUndoOwners"/> (scene-level settings), or null.</param>
        private static List<Object> Owners(IReadOnlyList<MutationRecord> records, out HashSet<Object>? sceneSettings)
        {
            var owners = new List<Object>();
            var seen = new HashSet<Object>();
            HashSet<IRqlUndoOwners>? views = null;
            sceneSettings = null;
            // The nearest Unity object enclosing each change: a write into a plain view object (e.g. a spline knot view) is
            // recorded on the component or asset that holds it, a write into an IRqlUndoOwners on the objects it names.
            foreach (var r in records)
                foreach (var candidate in r.Owners)
                {
                    if (candidate is Object o)
                    {
                        if (o != null && seen.Add(o)) owners.Add(o);
                        break;
                    }
                    if (candidate is IRqlUndoOwners settings)
                    {
                        // Once per view object, not per written member: naming the objects may scan (FindObjectsOfTypeAll).
                        sceneSettings ??= new HashSet<Object>();
                        if ((views ??= new HashSet<IRqlUndoOwners>()).Add(settings))
                            foreach (var s in settings.UndoOwners)
                                if (s != null && seen.Add(s)) { owners.Add(s); sceneSettings.Add(s); }
                        break;
                    }
                }
            return owners;
        }

        /// <summary>
        /// Destroy <paramref name="o"/> undoably; Unity logs instead of throwing when it keeps the object (a Transform, a
        /// component another one requires), so a survivor fails with <paramref name="failure"/>.
        /// </summary>
        internal static void DestroyUndoable(Object o, string failure)
        {
            Undo.DestroyObjectImmediate(o);
            if (o != null) throw new InvalidOperationException(failure + " (the Console has the reason)");
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
            var folders = args.Length > 1 && args[1] is string folder ? new[] { RqlEditorApi.Normalize(folder) } : null;
            // FindAssets only logs a missing folder and finds nothing, which reads as "no such assets".
            if (folders != null && !AssetDatabase.IsValidFolder(folders[0])) return new RqlError("not-found", $"unity.assets: no folder '{folders[0]}'");
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
                : new RqlError("ambiguous", $"{hits.Count} GameObjects are named '{key}': use one of them {Candidates(hits)}");
        }

        // The ambiguous hits as follow-up arguments: their hierarchy path where it is unique, else their ref.
        private static string Candidates(List<GameObject> hits)
        {
            const int Shown = 5;
            var paths = hits.Select(g => g.GetPath() ?? g.name).ToList();
            var parts = hits.Take(Shown).Select((g, i) =>
                paths.Count(p => p == paths[i]) == 1 ? $"unity.find(\"{paths[i]}\")" : $"ref(\"{Engine.RefOf(g)}\") ({paths[i]})");
            return string.Join(", ", parts) + (hits.Count > Shown ? $" … (+{hits.Count - Shown})" : "");
        }

        /// <summary><c>unity.asset("Assets/…/Red.mat")</c>: the main asset at a path; an error when there is none.</summary>
        public static object? LoadAsset(object?[] args)
        {
            if (args.Length != 1 || !(args[0] is string path)) return new RqlError("type-mismatch", "unity.asset(path) expects an asset path");
            // A missing asset as null would let `set sharedMaterial = unity.asset(<typo>)` clear the field silently.
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return asset != null
                ? asset
                : new RqlError("not-found", $"no asset at '{path}' (a project path such as \"Assets/…/Red.mat\"; search with unity.assets(\"t:Material Red\"))");
        }

    }
}
