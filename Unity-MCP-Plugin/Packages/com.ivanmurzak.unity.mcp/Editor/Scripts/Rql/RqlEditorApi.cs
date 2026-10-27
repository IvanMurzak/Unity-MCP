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
using System.Linq;
using Rql;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>editor</c> source: Editor state and operations that have no natural owner object, e.g.
    /// <c>editor | call CreateGameObject("Props")</c>, <c>editor | set isPaused = true</c>. Calls go through rql-mutate
    /// (mutation policy, dry-run, receipts); scene changes are undoable.
    /// </summary>
    public sealed partial class RqlEditorApi
    {
        public static readonly RqlEditorApi Instance = new RqlEditorApi();

        // Reads the requested state: the switch itself happens on the Editor's next update, so right after an assignment
        // isPlaying would still report the old state and the receipt would read "false → false".
        [RqlDescription("true in Play Mode (or while entering it); assign to enter or exit Play Mode. The switch happens after this call " +
                        "and usually reloads scripts: the next tool call may fail once while the Editor reconnects, so retry it; " +
                        "scene changes made in Play Mode are lost when it exits")]
        [RqlNoAutoSave] // entering Play Mode must not save the user's unsaved scene edits
        public bool isPlaying { get => EditorApplication.isPlayingOrWillChangePlaymode; set => EditorApplication.isPlaying = value; }

        [RqlDescription("Play Mode paused; assign to pause / resume")]
        [RqlNoAutoSave]
        public bool isPaused { get => EditorApplication.isPaused; set => EditorApplication.isPaused = value; }

        [RqlDescription("scripts are compiling")]
        public bool isCompiling => EditorApplication.isCompiling;

        [RqlDescription("the asset database is importing")]
        public bool isUpdating => EditorApplication.isUpdating;

        public string unityVersion => Application.unityVersion;

        public string projectPath => UnityMcpPluginEditor.ProjectRootPath;

        [RqlDescription("objects selected in the Editor; assign a list of refs to change the selection")]
        [RqlNoAutoSave]
        public Object[] selection { get => Selection.objects; set => Selection.objects = value ?? Array.Empty<Object>(); }

        [RqlCallable("create a GameObject (optionally under a parent, as a primitive: Cube, Sphere, Capsule, Cylinder, Plane, Quad; at a local position) and return it")]
        public GameObject CreateGameObject(string name, GameObject? parent = null, string? primitive = null, Vector3? position = null)
        {
            if (Adapters.UnityEngine_GameObject_Adapter.ValidateName(name) is string problem) throw new ArgumentException(problem);
            GameObject go;
            if (string.IsNullOrEmpty(primitive)) go = new GameObject(name);
            else if (Enum.TryParse<PrimitiveType>(primitive, ignoreCase: true, out var type)) { go = GameObject.CreatePrimitive(type); go.name = name; }
            else throw new ArgumentException($"unknown primitive '{primitive}' (Cube, Sphere, Capsule, Cylinder, Plane, Quad)");
            return Place(go, parent, position, "RQL create " + name);
        }

        /// <summary>A new object under <paramref name="parent"/> at a local <paramref name="position"/>, registered for Undo.</summary>
        private static GameObject Place(GameObject go, GameObject? parent, Vector3? position, string undoName)
        {
            if (parent != null) go.transform.SetParent(parent.transform, false);
            if (position.HasValue) go.transform.localPosition = position.Value;
            Undo.RegisterCreatedObjectUndo(go, undoName);
            return go;
        }

        [RqlDescription("the project's layer names by index 0..31 (\"\" = unused); GameObjects take one by layerName or layer")]
        public string[] layers => LayerNames();

        /// <summary>The 32 layer names by index ("" = unused).</summary>
        internal static string[] LayerNames() => Enumerable.Range(0, 32).Select(LayerMask.LayerToName).ToArray();

        [RqlDescription("the project's tags (Untagged, MainCamera … and the added ones)")]
        public string[] tags => UnityEditorInternal.InternalEditorUtility.tags;

        [RqlCallable("add a layer in the first free user layer (8..31, then 3, 6, 7) of Tags and Layers, or keep the one that has this name; returns its index (undoable)")]
        public int AddLayer(string name)
        {
            if (ValidateAddLayer(name) is { } problem) throw new ArgumentException(problem);
            var existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;
            var (manager, layerList) = TagManager("layers");
            var free = FreeUserLayer() ?? throw new ArgumentException(NoFreeLayer);
            layerList.GetArrayElementAtIndex(free).stringValue = name;
            manager.ApplyModifiedProperties();
            return free;
        }

        public string? ValidateAddLayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim() != name) return "a layer name cannot be empty or start / end with spaces";
            return LayerMask.NameToLayer(name) >= 0 || FreeUserLayer() != null ? null : NoFreeLayer;
        }

        [RqlCallable("add a tag to Tags and Layers; false when it already exists (undoable)")]
        public bool AddTag(string name)
        {
            if (ValidateAddTag(name) is { } problem) throw new ArgumentException(problem);
            if (Array.IndexOf(tags, name) >= 0) return false;
            var (manager, tagList) = TagManager("tags");
            tagList.InsertArrayElementAtIndex(tagList.arraySize);
            tagList.GetArrayElementAtIndex(tagList.arraySize - 1).stringValue = name;
            manager.ApplyModifiedProperties();
            return true;
        }

        public string? ValidateAddTag(string name) =>
            string.IsNullOrWhiteSpace(name) || name.Trim() != name ? "a tag cannot be empty or start / end with spaces" : null;

        // The layers a project may name: 8..31 first (where users expect them), then the free built-in slots 3, 6 and 7.
        private static readonly int[] UserLayers = Enumerable.Range(8, 24).Concat(new[] { 3, 6, 7 }).ToArray();
        private const string NoFreeLayer = "every user layer (3, 6, 7, 8..31) is taken";

        private static int? FreeUserLayer()
        {
            var names = LayerNames();
            foreach (var i in UserLayers)
                if (string.IsNullOrEmpty(names[i])) return i;
            return null;
        }

        // ProjectSettings/TagManager.asset as a SerializedObject (its writes register Undo), and one of its arrays.
        private static (SerializedObject Manager, SerializedProperty List) TagManager(string list)
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            return (manager, manager.FindProperty(list));
        }

        [RqlCallable("execute an Editor menu item by path, e.g. \"GameObject/Align With View\"; false when it does not exist")]
        public bool ExecuteMenuItem(string path) => EditorApplication.ExecuteMenuItem(path);
    }
}
