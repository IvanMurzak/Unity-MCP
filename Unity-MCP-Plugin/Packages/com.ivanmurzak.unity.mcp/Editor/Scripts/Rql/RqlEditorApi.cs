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

        [RqlDescription("true while in Play Mode; assign to enter or exit Play Mode")]
        [RqlNoAutoSave] // entering Play Mode must not save the user's unsaved scene edits
        public bool isPlaying { get => EditorApplication.isPlaying; set => EditorApplication.isPlaying = value; }

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

        [RqlCallable("execute an Editor menu item by path, e.g. \"GameObject/Align With View\"; false when it does not exist")]
        public bool ExecuteMenuItem(string path) => EditorApplication.ExecuteMenuItem(path);
    }
}
