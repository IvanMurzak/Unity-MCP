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
using Rql.Execution;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Mesh values accept a primitive's name: <c>set sharedMesh = "Cylinder"</c> assigns the built-in mesh that
    /// GameObject > 3D Object > Cylinder uses. The built-in meshes are not project assets, so no path reaches them.
    /// </summary>
    public sealed class UnityEngine_Mesh_Adapter : RqlTypeAdapter<Mesh>
    {
        public override string Description =>
            "Mesh: write as a ref, an asset path, or a primitive's name (Cube, Sphere, Capsule, Cylinder, Plane, Quad) for Unity's built-in mesh";

        private static readonly Dictionary<PrimitiveType, Mesh> Primitives = new Dictionary<PrimitiveType, Mesh>();

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is string text) || UnityEngine_Object_Adapter.TryAssetPath(text, out _))
                return AdapterOutcome.NotHandled; // refs and asset paths: the Object adapter
            if (!TryPrimitive(text, out var primitive))
            {
                reason = $"'{text}' is neither an asset path nor a primitive ({string.Join(", ", Enum.GetNames(typeof(PrimitiveType)))})";
                return AdapterOutcome.Failed;
            }
            result = PrimitiveMesh(primitive);
            return AdapterOutcome.Ok;
        }

        // By name only: Enum.TryParse also takes "3".
        private static bool TryPrimitive(string text, out PrimitiveType primitive)
        {
            foreach (var name in Enum.GetNames(typeof(PrimitiveType)))
                if (string.Equals(name, text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    primitive = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), name);
                    return true;
                }
            primitive = default;
            return false;
        }

        /// <summary>The mesh GameObject.CreatePrimitive gives <paramref name="primitive"/> (cached per domain).</summary>
        internal static Mesh PrimitiveMesh(PrimitiveType primitive)
        {
            if (Primitives.TryGetValue(primitive, out var mesh) && mesh != null) return mesh;
            // Its resource name differs between Unity versions ("Cylinder.fbx" / "New-Cylinder.fbx"): take it from a
            // throwaway primitive, which never reaches the scene file (destroyed before anything saves).
            var probe = GameObject.CreatePrimitive(primitive);
            try { mesh = probe.GetComponent<MeshFilter>().sharedMesh; }
            finally { Object.DestroyImmediate(probe); }
            return Primitives[primitive] = mesh;
        }
    }
}
