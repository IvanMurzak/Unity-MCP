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
            $"Mesh: write as a ref, an asset path, or a primitive's name ({Names}) for Unity's built-in mesh";

        private static readonly Dictionary<PrimitiveType, Mesh> Primitives = new Dictionary<PrimitiveType, Mesh>();

        // By name only (Enum.TryParse also takes "3"), any case.
        private static readonly Dictionary<string, PrimitiveType> ByName = new Dictionary<string, PrimitiveType>(StringComparer.OrdinalIgnoreCase);
        private static readonly string Names = string.Join(", ", Enum.GetNames(typeof(PrimitiveType)));

        static UnityEngine_Mesh_Adapter()
        {
            foreach (PrimitiveType p in Enum.GetValues(typeof(PrimitiveType))) ByName[p.ToString()] = p;
        }

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is string text) || UnityEngine_Object_Adapter.TryAssetPath(text, out _))
                return AdapterOutcome.NotHandled; // refs and asset paths: the Object adapter
            if (!ByName.TryGetValue(text.Trim(), out var primitive))
            {
                reason = $"'{text}' is neither an asset path nor a primitive ({Names})";
                return AdapterOutcome.Failed;
            }
            result = PrimitiveMesh(primitive);
            return AdapterOutcome.Ok;
        }

        /// <summary>The mesh GameObject.CreatePrimitive gives <paramref name="primitive"/> (cached per domain).</summary>
        internal static Mesh PrimitiveMesh(PrimitiveType primitive)
        {
            if (Primitives.TryGetValue(primitive, out var mesh) && mesh != null) return mesh;
            // The built-in resource, without creating anything (coercion also runs for dry runs). Cube and Quad live in
            // "Cube.fbx" / "Quad.fbx", the others in "New-<Name>.fbx" ("Cylinder.fbx" holds a different mesh, pCylinder1):
            // the mesh must carry the primitive's name. A throwaway primitive is the fallback for an Editor that renames them.
            var name = primitive.ToString();
            var files = primitive == PrimitiveType.Cube || primitive == PrimitiveType.Quad
                ? new[] { name + ".fbx", "New-" + name + ".fbx" }
                : new[] { "New-" + name + ".fbx", name + ".fbx" };
            foreach (var file in files)
            {
                mesh = Resources.GetBuiltinResource<Mesh>(file);
                if (mesh != null && mesh.name == name) return Primitives[primitive] = mesh;
            }
            var probe = GameObject.CreatePrimitive(primitive);
            try { mesh = probe.GetComponent<MeshFilter>().sharedMesh; }
            finally { Object.DestroyImmediate(probe); }
            return Primitives[primitive] = mesh;
        }
    }
}
