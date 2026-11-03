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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME
{
    // Aliases inside the namespace block: see the name-clash rule in YOUR_FEATURE_NAMEModule.cs.
    using GameObject = UnityEngine.GameObject;
    using Light = UnityEngine.Light;
    using LightType = UnityEngine.LightType;
    using Vector3 = UnityEngine.Vector3;

    /// <summary>
    /// The <c>YOUR_FEATURE_NAME_LOWERCASE</c> source (template example): operations that have no owner object yet, such as
    /// creating one, e.g. <c>YOUR_FEATURE_NAME_LOWERCASE | call CreateLight("Lamp", "Spot", "(0, 3, 0)")</c>. Public instance
    /// methods marked [RqlCallable] are callable; parameters are coerced by RQL (strings to enums, "(x, y, z)" to Vector3 …)
    /// and optional parameters may be omitted. Operations on an existing object belong in that type's adapter instead.
    /// </summary>
    public sealed class YOUR_FEATURE_NAMEEditorApi
    {
        public static readonly YOUR_FEATURE_NAMEEditorApi Instance = new YOUR_FEATURE_NAMEEditorApi();

        [RqlCallable("create a GameObject with a Light of the given type (Point, Spot, Directional …) " +
                     "at position \"(x, y, z)\"; returns the GameObject (undoable)")]
        public GameObject CreateLight(string name = "Light", LightType type = LightType.Point, Vector3? position = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is empty");

            var go = new GameObject(name);
            go.AddComponent<Light>().type = type;
            if (position.HasValue) go.transform.position = position.Value;
            Undo.RegisterCreatedObjectUndo(go, "RQL create light " + name);
            return go;
        }
    }
}
