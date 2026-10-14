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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>editor</c> source: Editor operations that have no natural owner object, invoked with
    /// <c>editor | call CreateGameObject("Props")</c>. Calls go through rql-mutate (mutation policy, dry-run, receipts) and
    /// are undoable.
    /// </summary>
    public sealed class RqlEditorApi
    {
        public static readonly RqlEditorApi Instance = new RqlEditorApi();

        [RqlCallable("create a GameObject (optionally under a parent; primitive = Cube, Sphere, Capsule, Cylinder, Plane, Quad) and return it")]
        public GameObject CreateGameObject(string name, GameObject? parent = null, string? primitive = null)
        {
            GameObject go;
            if (string.IsNullOrEmpty(primitive)) go = new GameObject(name);
            else if (Enum.TryParse<PrimitiveType>(primitive, ignoreCase: true, out var type)) { go = GameObject.CreatePrimitive(type); go.name = name; }
            else throw new ArgumentException($"unknown primitive '{primitive}' (Cube, Sphere, Capsule, Cylinder, Plane, Quad)");
            if (parent != null) go.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "RQL create " + name);
            return go;
        }
    }
}
