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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            var outerDestroying = Destroying; // a Mutate nested in a call of an outer one leaves the outer stage's set in place
            try
            {
                var budgets = maxTargets.HasValue ? new Budgets { MaxMutationTargets = maxTargets.Value } : null;
                result = Engine.Execute(query, budgets, new QueryOptions { AllowMutation = true, DryRun = dryRun });
                completed = true;
            }
            finally
            {
                Destroying = outerDestroying; // after a dry run, or a stage that failed before its calls
                _mutating--;
                if (_mutating == 0)
                {
                    CloseUndoGroup(); // a mutation that failed before Committed
                    // Execute threw: nothing gets saved. Raised here, like below, once InMutation is false again.
                    if (!completed) Raise(Discarded);
                }
            }
            if (_mutating > 0) return result;
            if (MatchedNothingThroughUnknownMember(result))
            {
                // "0 results" with a warning read as a successful change: a misspelt member must fail the call.
                result.Ok = false;
                result.Diagnostics.Insert(0, Diagnostic.Error("unknown-member",
                    "nothing was changed: the query names a member that does not exist (see the warning), so no target reached set / call",
                    "fix the member name (rql-schema \"<type>\" lists a type's members)"));
            }
            if (dryRun || !_wrote)
            {
                Reimports.Clear();
                foreach (var problem in Raise(Discarded))
                    result.Diagnostics.Add(Diagnostic.Warning("not-discarded", problem));
                return result;
            }
            if (PlayModeNotice(EditorApplication.isPlaying) is { } notice)
                result.Diagnostics.Add(notice);
            List<string> problems;
            // The changes are applied: a save that throws must not turn the receipt into a tool error.
            try { problems = SaveChanges(); }
            catch (Exception ex) { problems = new List<string> { $"saving failed: {ex.GetType().Name}: {ex.Message}" }; }
            foreach (var problem in problems)
                result.Diagnostics.Add(Diagnostic.Warning("not-saved", problem));
            return result;
        }

        // Play Mode changes look applied (and are, until Play Mode exits): the receipt says they will not last.
        internal static Diagnostic? PlayModeNotice(bool isPlaying) => isPlaying
            ? Diagnostic.Warning("play-mode", "the Editor is in Play Mode: scene changes are temporary and are lost when Play Mode exits (asset changes stay)")
            : null;

        internal static bool MatchedNothingThroughUnknownMember(QueryResult result) =>
            result.Ok && (result.Rows == null || result.Rows.Count == 0) && (result.Total ?? 0) == 0
            && result.Diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Warning && d.Code == "unknown-member");

        /// <summary>Hard ceiling of <c>max_targets</c> (the engine clamps a request to it).</summary>
        public static int HardMaxTargets => Engine.Mutations.HardMaxTargets;

        /// <summary>
        /// The components the running call stage destroys, known to <see cref="ValidateBatch"/> before the first call (null
        /// otherwise): Destroy removes the ones that require a target first, and a requirement they meet does not block.
        /// </summary>
        internal static HashSet<Component>? Destroying { get; private set; }

        /// <summary>
        /// RQL's batch check (<see cref="MutationPolicy.ValidateBatch"/>): every planned change of a set / call stage, after each
        /// target passed its own validator and before anything runs, dry runs included. A reason refuses the whole stage.
        /// </summary>
        internal static string? ValidateBatch(IReadOnlyList<MutationRecord> records)
        {
            Destroying = null;
            var prefabPaths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var destroying = new HashSet<Component>();
            foreach (var record in records)
            {
                switch (record.Method?.Name)
                {
                    // Two targets saving to one prefab path (one GameObject reached twice included): the second save would fail
                    // after the first one happened.
                    case "SaveAsPrefab" when record.Item is GameObject && record.Args[0] is string text:
                        var path = UnityEngine_GameObject_Adapter.PrefabPath(text);
                        if (prefabPaths.TryGetValue(path, out var earlier))
                            return $"'{path}' is also the path of {Engine.RefOf(earlier)} in this call: give each GameObject its own prefab path";
                        prefabPaths[path] = record.Item;
                        break;
                    case "Destroy" when record.Item is Component component && component != null: // a Transform never passed its validator
                        destroying.Add(component);
                        break;
                }
            }
            if (destroying.Count == 0) return null;
            Destroying = destroying;
            var refused = UnmetRequirement(destroying);
            if (refused != null) Destroying = null; // nothing will run
            return refused;
        }

        // A component the call removes while another one that requires it stays, or components that require each other (no
        // order removes them; Unity keeps both): the reason, or null.
        private static string? UnmetRequirement(HashSet<Component> destroying)
        {
            static string Refused(Component target, string why) => $"cannot call Destroy() on {Engine.RefOf(target)}: {why}";
            var pending = new List<(Component Target, List<(Component Blocker, string Reason)> Blockers)>();
            var removed = new HashSet<Component>();
            foreach (var target in destroying)
            {
                var blockers = UnityEngine_Component_Adapter.Blockers(target);
                foreach (var (blocker, reason) in blockers)
                    if (!destroying.Contains(blocker))
                        return Refused(target, reason + " (or in the same call)");
                if (blockers.Count > 0) pending.Add((target, blockers));
                else removed.Add(target);
            }
            // Every blocker is a target too: replay the removal order (dependents first); what never frees up is a cycle.
            for (var progress = true; progress && pending.Count > 0;)
            {
                progress = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (!pending[i].Blockers.TrueForAll(b => removed.Contains(b.Blocker))) continue;
                    removed.Add(pending[i].Target);
                    pending.RemoveAt(i);
                    progress = true;
                }
            }
            return pending.Count == 0 ? null
                : Refused(pending[0].Target, pending[0].Blockers[0].Reason + " and they require each other: destroy their GameObject instead");
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
        // Importers written by the running Mutate: their settings reach the asset only through a reimport.
        internal static readonly HashSet<AssetImporter> Reimports = new HashSet<AssetImporter>();

        public static List<string> SaveChanges()
        {
            var problems = Raise(Saving);
            foreach (var importer in Reimports)
            {
                if (importer == null) continue;
                try { importer.SaveAndReimport(); }
                catch (Exception ex) { problems.Add($"'{importer.assetPath}' was not reimported: {ex.Message}"); }
            }
            Reimports.Clear();
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
