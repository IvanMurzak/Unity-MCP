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
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Transform: parent / root are references; parent is written through Undo.SetTransformParent, because
    /// Undo.RecordObject does not capture parent changes. It is an object although it enumerates its children
    /// (UnityCoreModule registers TreatAsObject); a GameObject is accepted where a Transform is expected (the Component
    /// adapter's rule). <c>LookAt(target[, worldUp])</c> turns it toward a GameObject or a world position.
    /// </summary>
    public sealed class UnityEngine_Transform_Adapter : RqlTypeAdapter<Transform>
    {
        protected override void Members(MemberTable<Transform> m)
        {
            m.Add<Transform>("parent", t => t.parent, (t, v) => Undo.SetTransformParent(t, v, "RQL set parent"), ValidateParent,
                    description: "parent Transform (null = scene root); assigning keeps the world position (undoable)")
             .Reference("root");
            AddLookAt(m.Untyped, o => (Transform)o);
        }

        /// <summary>
        /// The <c>LookAt(target[, worldUp])</c> call on <paramref name="table"/>'s type (Transform, and GameObject through its
        /// Transform). Typed parameters: the call stage coerces the target (a GameObject / component, or a position literal)
        /// through <see cref="TransformOrPosition_Adapter"/>.
        /// </summary>
        internal static void AddLookAt(MemberTable table, Func<object, Transform> transformOf) =>
            table.AddMethod("LookAt",
                new[] { new CallableParameter("target", typeof(TransformOrPosition)), new CallableParameter("worldUp", typeof(Vector3), true, Vector3.up) },
                typeof(void),
                (o, a) => { LookAt(transformOf(o), (TransformOrPosition)a[0]!, (Vector3)a[1]!); return null; },
                "rotate so the forward (blue) axis points at a target: a GameObject (unity.find(\"Player\"), ref(…)) or a world position " +
                "\"(x, y, z)\"; worldUp (default (0, 1, 0)) orients the up axis (undoable)",
                (o, a) => ValidateLookAt(transformOf(o), (TransformOrPosition)a[0]!, (Vector3)a[1]!));

        private static void LookAt(Transform t, TransformOrPosition target, Vector3 worldUp)
        {
            if (ValidateLookAt(t, target, worldUp) is { } problem) throw new ArgumentException(problem); // a C# caller skips RQL's validator
            Undo.RecordObject(t, "RQL LookAt"); // called on a GameObject, the mutation recorded the GameObject, not its Transform
            t.LookAt(target.WorldPosition, worldUp);
        }

        /// <summary>
        /// Unity silently keeps the rotation when the direction or worldUp is zero: refused before any target is rotated, so
        /// the receipt never reports a turn that did not happen.
        /// </summary>
        internal static string? ValidateLookAt(Transform t, TransformOrPosition target, Vector3 worldUp)
        {
            if (target.Transform == t) return $"'{t.name}' cannot look at itself";
            if (worldUp.sqrMagnitude < 1e-12f) return "worldUp cannot be (0, 0, 0)";
            var direction = target.WorldPosition - t.position;
            if (direction.sqrMagnitude < 1e-12f) return $"the target is at '{t.name}''s own position: there is no direction to look in";
            return null;
        }

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

    /// <summary>A call argument that is a Transform (a GameObject or component gives its Transform) or a world position.</summary>
    public readonly struct TransformOrPosition
    {
        public TransformOrPosition(Transform transform) { Transform = transform; Position = default; }
        public TransformOrPosition(Vector3 position) { Transform = null; Position = position; }

        public Transform? Transform { get; }
        public Vector3 Position { get; }

        public Vector3 WorldPosition => Transform != null ? Transform.position : Position;

        public override string ToString() => Transform != null ? Transform.name : Position.ToString();
    }

    /// <summary>Coerces a call argument to <see cref="TransformOrPosition"/>: a Unity object to its Transform, anything else to a Vector3.</summary>
    public sealed class TransformOrPosition_Adapter : RqlTypeAdapter<TransformOrPosition>
    {
        public override string Description => "a GameObject / component (unity.find(\"Player\"), ref(…)) or a world position \"(x, y, z)\"";

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            if (value is TransformOrPosition) { result = value; reason = null; return AdapterOutcome.Ok; }
            if (value is UnityEngine.Object o)
            {
                if (o != null && kernel.TryCoerce(o, typeof(Transform), out var t, out _) && t is Transform transform && transform != null)
                {
                    result = new TransformOrPosition(transform);
                    reason = null;
                    return AdapterOutcome.Ok;
                }
                reason = o == null ? "the target object was destroyed" : $"{o.GetType().Name} '{o.name}' has no Transform: pass a GameObject or a position \"(x, y, z)\"";
                return AdapterOutcome.Failed;
            }
            if (kernel.TryCoerce(value, typeof(Vector3), out var p, out _) && p is Vector3 position)
            {
                result = new TransformOrPosition(position);
                reason = null;
                return AdapterOutcome.Ok;
            }
            reason = "expected a GameObject (unity.find(\"Name\"), ref(…)) or a world position \"(x, y, z)\"";
            return AdapterOutcome.Failed;
        }
    }
}
