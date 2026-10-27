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
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public static partial class RqlUnity
    {
        private static int _mutating;

        // Set by the engine's Committed hook: the running Mutate applied a set or call that changes project state.
        private static bool _wrote;

        /// <summary>
        /// Raised by <see cref="Mutate"/> once its query finished, before assets and scenes are saved: modules that defer
        /// their own file writes during a mutation (e.g. <c>.inputactions</c> files) flush them here.
        /// </summary>
        public static event Action? Saving;

        /// <summary>
        /// Raised by <see cref="Mutate"/> when its query ended without saving (nothing applied, a rolled-back set, a dry run, an
        /// error): modules drop the file writes they deferred during it (e.g. restore <c>.inputactions</c> files' state).
        /// </summary>
        public static event Action? Discarded;

        /// <summary>True while <see cref="Mutate"/> runs a query: writes may be deferred to <see cref="Saving"/>.</summary>
        public static bool InMutation => _mutating > 0;

        /// <summary>
        /// Run a set / call query and save the result to disk: changed assets, modified scenes and the open prefab. Saving
        /// runs after failed queries too when calls completed before the failure (they are not rolled back); a query that
        /// applied nothing (a parse error, no matching items, a set the engine rolled back) saves nothing, so it never
        /// persists unrelated unsaved edits. Untitled scenes (no path yet) stay in memory and are reported as a warning.
        /// </summary>
        public static QueryResult Mutate(string query, bool dryRun = false, int? maxTargets = null)
        {
            if (_mutating == 0) _wrote = false;
            _mutating++;
            QueryResult result;
            var completed = false;
            var outerBatch = _batch;
            var batch = new CallBatch();
            _batch = batch; // restored in finally; the local outlives it for Finish
            try
            {
                var budgets = maxTargets.HasValue ? new Budgets { MaxMutationTargets = maxTargets.Value } : null;
                result = Engine.Execute(query, budgets, new QueryOptions { AllowMutation = true, DryRun = dryRun });
                completed = true;
                result = batch.Finish(result);
                ExplainMutationLimit(result);
            }
            finally
            {
                _batch = outerBatch;
                _mutating--;
                if (_mutating == 0)
                {
                    CloseUndoGroup(); // a mutation that failed before Committed
                    // Execute threw: nothing gets saved. Raised here, like below, once InMutation is false again.
                    if (!completed) Raise(Discarded);
                }
            }
            if (_mutating > 0) return result;
            if (dryRun || !_wrote)
            {
                foreach (var problem in Raise(Discarded))
                    result.Diagnostics.Add(Diagnostic.Warning("not-discarded", problem));
                return result;
            }
            List<string> problems;
            // The changes are applied: a save that throws must not turn the receipt into a tool error.
            try { problems = SaveChanges(); }
            catch (Exception ex) { problems = new List<string> { $"saving failed: {ex.GetType().Name}: {ex.Message}" }; }
            foreach (var problem in problems)
                result.Diagnostics.Add(Diagnostic.Warning("not-saved", problem));
            return result;
        }

        /// <summary>Hard ceiling of <c>max_targets</c> (the engine clamps a request to it).</summary>
        public static int HardMaxTargets => Engine.Mutations.HardMaxTargets;

        // RQL's hint names its own budget option; the tool's argument for it is max_targets.
        private static void ExplainMutationLimit(QueryResult result)
        {
            var d = result.Diagnostics;
            for (int i = 0; i < d.Count; i++)
                if (d[i].Code == "mutation-limit")
                    d[i] = new Diagnostic(d[i].Severity, d[i].Code, d[i].Message, d[i].At,
                        $"narrow the query (where / first / ref(...)), or pass max_targets (up to {HardMaxTargets}) when the bulk change is intended");
        }

        // The call stage of the running Mutate (null outside one): call validators see the targets validated before them,
        // so checks that span several targets of one call (two SaveAsPrefab to one path, removing a component together with
        // the components that require it) are decided for the whole call, on dry runs too.
        private static CallBatch? _batch;

        /// <summary>The running Mutate's call batch, or null (a C# caller, a direct engine query): checks stay per target.</summary>
        internal static CallBatch? Batch => _batch;

        internal sealed class CallBatch
        {
            private readonly Dictionary<string, object> _claimedPaths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<Component> _destroying = new HashSet<Component>();
            private readonly List<(Component Target, List<Component> Blockers, string Reason)> _deferred = new List<(Component, List<Component>, string)>();
            private string? _rejection;

            /// <summary>Claims a new asset path for <paramref name="owner"/>; the earlier target that claimed it, or null.</summary>
            public object? Claim(string path, object owner)
            {
                if (_claimedPaths.TryGetValue(path, out var earlier) && !ReferenceEquals(earlier, owner)) return earlier;
                _claimedPaths[path] = owner;
                return null;
            }

            /// <summary>Components whose dependents are being removed first (two components requiring each other end the recursion).</summary>
            public readonly HashSet<Component> Removing = new HashSet<Component>();

            /// <summary>A component this call destroys (every target is validated before the first call runs).</summary>
            public bool IsDestroying(Component c) => _destroying.Contains(c);

            /// <summary>
            /// <paramref name="target"/> is destroyed by this call; its <paramref name="blockers"/> (components that require it)
            /// must be destroyed by the same call, which is known only once every target was validated.
            /// </summary>
            public void Destroying(Component target, List<Component> blockers, string reason)
            {
                _destroying.Add(target);
                if (blockers.Count > 0) _deferred.Add((target, blockers, reason));
            }

            /// <summary>The first deferred check the whole call does not meet, or null.</summary>
            public string? Unmet()
            {
                foreach (var (target, blockers, reason) in _deferred)
                    if (blockers.Exists(b => b != null && !_destroying.Contains(b)))
                        return $"cannot call Destroy() on {Engine.RefOf(target)}: {reason}; nothing was called";
                return null;
            }

            /// <summary>Before the first write (the engine skips this hook on dry runs): veto the call when a deferred check is unmet.</summary>
            public void BeforeWrite()
            {
                _rejection = Unmet();
                if (_rejection != null) throw new InvalidOperationException(_rejection);
            }

            /// <summary>The query's result with the deferred checks applied: a dry run is checked here, a real run was vetoed in BeforeWrite.</summary>
            public QueryResult Finish(QueryResult result)
            {
                var rejection = _rejection ?? (result.Ok && result.DryRun ? Unmet() : null);
                return rejection == null ? result : QueryResult.Fail(new[] { Diagnostic.Error("invalid-value", rejection) });
            }
        }

        /// <summary>Invoke every handler of <paramref name="handlers"/>, even after one threw; returns the messages of those that did.</summary>
        private static List<string> Raise(Action? handlers)
        {
            var problems = new List<string>();
            if (handlers != null)
                foreach (Action handler in handlers.GetInvocationList())
                {
                    try { handler(); }
                    catch (Exception ex) { problems.Add(ex.Message); }
                }
            return problems;
        }

        /// <summary>Write every pending change to disk; returns what could not be saved.</summary>
        public static List<string> SaveChanges()
        {
            var problems = Raise(Saving);
            AssetDatabase.SaveAssets();
            if (EditorApplication.isPlaying) return problems; // play mode scene changes are temporary by design

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty && SavePrefabStage(stage) is string prefabProblem)
                problems.Add(prefabProblem);
            SaveDirtyScenes(problems);
            return problems;
        }

        /// <summary>Save the prefab open in prefab editing mode and clear its dirty flag; the problem, or null when saved.</summary>
        internal static string? SavePrefabStage(PrefabStage stage)
        {
            if (stage.prefabContentsRoot == null) return $"the prefab '{stage.assetPath}' has no root object to save";
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out var saved);
            if (!saved) return $"the prefab '{stage.assetPath}' could not be saved";
            stage.ClearDirtiness();
            return null;
        }

        /// <summary>
        /// Save every modified loaded scene, adding what could not be saved to <paramref name="problems"/>. Untitled scenes are
        /// skipped (saving one would open the modal "Save Scene" dialog, which blocks the main thread).
        /// </summary>
        internal static void SaveDirtyScenes(List<string> problems)
        {
            foreach (var scene in Scenes())
            {
                if (!scene.isDirty) continue;
                if (string.IsNullOrEmpty(scene.path))
                    problems.Add($"scene '{Name(scene)}' has no file yet; save it with scenes | where path == \"\" | call SaveAs(\"Assets/Scenes/X.unity\")");
                else if (!EditorSceneManager.SaveScene(scene))
                    problems.Add($"scene '{scene.path}' could not be saved");
            }
        }

        private static string Name(Scene scene) => string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
    }
}
