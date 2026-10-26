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
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Transform: parent / root are references; parent is written through Undo.SetTransformParent, because
    /// Undo.RecordObject does not capture parent changes. It is an object although it enumerates its children
    /// (UnityCoreModule registers TreatAsObject); a GameObject is accepted where a Transform is expected (the Component
    /// adapter's rule).
    /// </summary>
    public sealed class UnityEngine_Transform_Adapter : RqlTypeAdapter<Transform>
    {
        protected override void Members(MemberTable<Transform> m) => m
            .Add<Transform>("parent", t => t.parent, (t, v) => Undo.SetTransformParent(t, v, "RQL set parent"), ValidateParent,
                description: "parent Transform (null = scene root); assigning keeps the world position (undoable)")
            .Reference("root");

        /// <summary>
        /// Unity rejects these parents by logging an error and leaving the hierarchy as it was; checked before any write so
        /// the mutation fails (and changes nothing) instead of reporting a reparent that did not happen.
        /// </summary>
        internal static string? ValidateParent(Transform t, Transform? parent)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(t) && !PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                return $"'{t.name}' is inside a prefab instance: move it in the prefab (editor | call OpenPrefab(path)) or unpack the instance";
            if (parent == null) return null;
            if (parent == t || parent.IsChildOf(t))
                return $"'{parent.name}' is '{t.name}' itself or one of its children: a GameObject cannot be parented under itself";
            if (EditorUtility.IsPersistent(parent)) return $"'{parent.name}' is an asset, not an object in an open scene or prefab";
            return null;
        }
    }
}
