/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;

    /// <summary>
    /// Persistence of InputActionAsset edits. A <c>.inputactions</c> file is the source of truth: the imported asset is rebuilt
    /// from its JSON on every import, so an in-memory edit is lost unless it is written back. Every RQL write goes through
    /// <see cref="Edit"/>: record the asset for Undo, change it, write <c>ToJson()</c> to the file and re-import it. Inside
    /// <see cref="RqlUnity.Mutate"/> the write is deferred to the end of the query, so a bulk edit re-imports each file once.
    /// The re-import keeps the asset object but replaces its maps and actions, which is why the views address everything by id.
    /// A mutation that ends without saving drops the deferred edits (<see cref="Discard"/>).
    /// Undo / redo restore the in-memory asset only; <see cref="Resync"/> writes the restored state back to the file.
    /// </summary>
    [InitializeOnLoad]
    public static class InputAssetStore
    {
        public const string Extension = ".inputactions";

        private const string TrackedKey = "com.IvanMurzak.Unity.MCP.Rql.InputSystem.TrackedAssets";

        static InputAssetStore()
        {
            Undo.undoRedoPerformed -= Resync;
            Undo.undoRedoPerformed += Resync;
            RqlUnity.Saving -= Flush;
            RqlUnity.Saving += Flush;
            RqlUnity.Discarded -= Discard;
            RqlUnity.Discarded += Discard;
        }

        // .inputactions files edited during the running mutation, written by Flush.
        private static readonly HashSet<string> Pending = new HashSet<string>();

        /// <summary>Record <paramref name="asset"/> for Undo, apply <paramref name="change"/> and persist it.</summary>
        public static void Edit(InputActionAsset asset, string label, Action change)
        {
            if (asset == null) throw new InvalidOperationException("the InputActionAsset no longer exists");
            var path = AssetDatabase.GetAssetPath(asset);
            var deferred = RqlUnity.InMutation && IsInputActionsFile(path);
            // Deferred: earlier edits of this mutation are only in memory, so a failed change restores this snapshot
            // instead of the file.
            var before = deferred && Pending.Contains(path) ? ToJson(asset) : null;
            Undo.RecordObject(asset, "RQL input: " + label);
            if (!deferred)
            {
                Apply(asset, change);
                Save(asset);
                return;
            }
            Apply(asset, () => { change(); Validate(ToJson(asset)); }, before);
            Pending.Add(path);
        }

        private static void Apply(InputActionAsset asset, Action change, string? before = null)
        {
            try
            {
                change();
            }
            catch
            {
                // A half-applied change must not stay in memory: re-import restores the file state; a deferred edit
                // restores the snapshot in memory only, so the file stays untouched until Flush (or Discard re-imports it).
                if (before != null) asset.LoadFromJson(before);
                else Revert(asset);
                throw;
            }
        }

        /// <summary>Write the files edited during the mutation (<see cref="RqlUnity.Saving"/>).</summary>
        private static void Flush()
        {
            var paths = Pending.ToList();
            Pending.Clear();
            var failed = new List<string>();
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                try { if (asset != null) Save(asset); }
                catch (Exception ex) { failed.Add($"'{path}': {ex.Message}"); }
            }
            if (failed.Count > 0) throw new InvalidOperationException("could not write " + string.Join("; ", failed));
        }

        /// <summary>
        /// The mutation ended without saving (<see cref="RqlUnity.Discarded"/>: rolled back, a dry run, an error): drop its
        /// deferred edits. Re-importing each file restores the in-memory asset to the file, which was never written.
        /// </summary>
        private static void Discard()
        {
            var paths = Pending.ToList();
            Pending.Clear();
            foreach (var path in paths)
                if (File.Exists(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static bool IsInputActionsFile(string path) =>
            !string.IsNullOrEmpty(path) && path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

        private static void Write(string path, string json)
        {
            Track(path);
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Write the asset to its file (<c>.inputactions</c>: JSON + re-import; other assets: SetDirty + SaveAssets). An asset
        /// without a file (a runtime instance) keeps the change in memory.
        /// </summary>
        public static void Save(InputActionAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;
            if (!IsInputActionsFile(path))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return;
            }
            var json = ToJson(asset);
            Validate(json);
            Write(path, json);
        }

        /// <summary>Create a new <c>.inputactions</c> file with an optional first action map; returns the imported asset.</summary>
        public static InputActionAsset Create(string path, string? map)
        {
            path = NewAssetPath(path);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            string json;
            try
            {
                asset.name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(map))
                {
                    InputValidation.Name(map, "action map");
                    UnityEngine.InputSystem.InputActionSetupExtensions.AddActionMap(asset, map!);
                }
                json = ToJson(asset);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
            Write(path, json);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path)
                ?? throw new InvalidOperationException($"'{path}' was written but did not import as an InputActionAsset");
        }

        /// <summary>
        /// The asset as <c>.inputactions</c> JSON. Guards the Input System 1.x bug where ToJson() throws for an asset without
        /// action maps (its map array is null): an empty array serializes the same and keeps the control schemes.
        /// </summary>
        public static string ToJson(InputActionAsset asset)
        {
            if (GetField(asset, "m_ActionMaps") == null) SetField(asset, "m_ActionMaps", Array.Empty<UnityEngine.InputSystem.InputActionMap>());
            return asset.ToJson();
        }

        /// <summary>The written JSON must load back (the importer would otherwise produce an empty asset).</summary>
        private static void Validate(string json)
        {
            InputActionAsset? check = null;
            try
            {
                check = InputActionAsset.FromJson(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("the edited asset does not serialize to valid .inputactions JSON: " + ex.Message, ex);
            }
            finally
            {
                if (check != null) UnityEngine.Object.DestroyImmediate(check);
            }
        }

        private static void Revert(InputActionAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (IsInputActionsFile(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>After Undo / Redo: write every asset this module edited whose in-memory state differs from its file.</summary>
        public static void Resync()
        {
            foreach (var path in Tracked())
            {
                if (!File.Exists(path)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset == null) continue;
                string json;
                try { json = ToJson(asset); }
                catch (Exception ex) { Debug.LogWarning($"[RQL] could not write '{path}' after undo: {ex.Message}"); continue; }
                if (json != File.ReadAllText(path)) Write(path, json);
            }
        }

        // The edited paths survive domain reloads (SessionState), so an undo after a script reload still reaches the file.
        private static List<string> Tracked() =>
            SessionState.GetString(TrackedKey, "").Split('\n').Where(p => p.Length > 0).ToList();

        private static void Track(string path)
        {
            var tracked = Tracked();
            if (tracked.Contains(path)) return;
            tracked.Add(path);
            SessionState.SetString(TrackedKey, string.Join("\n", tracked));
        }

        /// <summary>Validate a path for a new asset: Assets/, .inputactions, nothing there yet; creates missing folders.</summary>
        public static string NewAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty");
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {Extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
                throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            RqlEditorApi.Instance.CreateFolder(Path.GetDirectoryName(path)!);
            return path;
        }

        // ------------------------------------------------------------ serialized fields without a public setter

        /// <summary>
        /// Assign an Input System serialized field that has no public setter (InputAction.m_Type / m_Interactions /
        /// m_Processors, InputActionMap.m_Name, InputActionAsset.m_ControlSchemes). These names are the .inputactions
        /// serialization format, so they are stable; a missing field fails loudly instead of silently doing nothing.
        /// </summary>
        public static void SetField(object target, string field, object? value) => Field(target, field).SetValue(target, value);

        public static object? GetField(object target, string field) => Field(target, field).GetValue(target);

        private static FieldInfo Field(object target, string field) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, field);
    }
}
