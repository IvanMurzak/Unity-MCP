/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Project: Ivan Murzak (https://github.com/IvanMurzak/Unity-MCP)  │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if PROBUILDER_ENABLED
#nullable enable
using System.ComponentModel;
using System.Linq;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_GetMeshInfo",
            Title = "Get ProBuilder mesh information"
        )]
        [Description(@"Retrieves detailed information about a ProBuilder mesh including faces, vertices, and edges.
Use this to understand the mesh structure before performing operations like extrusion or beveling.
Face indices are used to select faces for operations like extrusion or deletion.
Edge indices are used to select edges for operations like beveling.")]
        public string GetMeshInfo
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("If true, includes detailed vertex positions for each face.")]
            bool includeVertexPositions = false,
            [Description("If true, includes edge information for each face.")]
            bool includeEdges = true,
            [Description("Maximum number of faces to include in detail. Use -1 for all faces. Default is 20.")]
            int maxFacesToShow = 20
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRef?.IsValid != true)
                return "[Error] Invalid GameObject reference provided.";

            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                return $"[Error] {error}";

            if (go == null)
                return Error.GameObjectNotFound();

            var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (proBuilderMesh == null)
                return Error.ProBuilderMeshNotFound(go.GetInstanceID());

            var sb = new StringBuilder();
            sb.AppendLine($"[Success] ProBuilder Mesh Information for '{go.name}'");
            sb.AppendLine();

            // Basic info
            sb.AppendLine("# Summary:");
            sb.AppendLine($"- GameObject InstanceID: {go.GetInstanceID()}");
            sb.AppendLine($"- Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Edge Count: {proBuilderMesh.edgeCount}");
            sb.AppendLine($"- Triangle Count: {proBuilderMesh.triangleCount}");
            sb.AppendLine();

            // Bounds
            var bounds = proBuilderMesh.mesh.bounds;
            sb.AppendLine("# Bounds:");
            sb.AppendLine($"- Center: {bounds.center}");
            sb.AppendLine($"- Size: {bounds.size}");
            sb.AppendLine($"- Min: {bounds.min}");
            sb.AppendLine($"- Max: {bounds.max}");
            sb.AppendLine();

            // Face details
            var faces = proBuilderMesh.faces;
            var positions = proBuilderMesh.positions;
            var facesToShow = maxFacesToShow < 0 ? faces.Count : System.Math.Min(maxFacesToShow, faces.Count);

            sb.AppendLine($"# Faces (showing {facesToShow} of {faces.Count}):");
            sb.AppendLine("Use face indices for extrusion or deletion operations.");
            sb.AppendLine();

            for (int i = 0; i < facesToShow; i++)
            {
                var face = faces[i];
                var faceVertices = face.distinctIndexes;
                var faceEdges = face.edges;

                // Calculate face center and normal
                var center = Vector3.zero;
                foreach (var vertIndex in faceVertices)
                {
                    center += positions[vertIndex];
                }
                center /= faceVertices.Count;

                sb.AppendLine($"## Face {i}:");
                sb.AppendLine($"  - Vertex Count: {faceVertices.Count}");
                sb.AppendLine($"  - Triangle Count: {face.indexes.Count / 3}");
                sb.AppendLine($"  - Center (approx): ({center.x:F2}, {center.y:F2}, {center.z:F2})");

                if (includeVertexPositions)
                {
                    sb.AppendLine($"  - Vertex Positions:");
                    foreach (var vertIndex in faceVertices)
                    {
                        var pos = positions[vertIndex];
                        sb.AppendLine($"    - [{vertIndex}]: ({pos.x:F3}, {pos.y:F3}, {pos.z:F3})");
                    }
                }

                if (includeEdges)
                {
                    sb.AppendLine($"  - Edges ({faceEdges.Count}):");
                    foreach (var edge in faceEdges)
                    {
                        var p1 = positions[edge.a];
                        var p2 = positions[edge.b];
                        sb.AppendLine($"    - [{edge.a} -> {edge.b}]: ({p1.x:F2},{p1.y:F2},{p1.z:F2}) to ({p2.x:F2},{p2.y:F2},{p2.z:F2})");
                    }
                }
                sb.AppendLine();
            }

            if (facesToShow < faces.Count)
            {
                sb.AppendLine($"... and {faces.Count - facesToShow} more faces. Use maxFacesToShow=-1 to see all.");
            }

            // Unique edges summary
            if (includeEdges)
            {
                var allEdges = proBuilderMesh.faces.SelectMany(f => f.edges).Distinct().ToList();
                sb.AppendLine();
                sb.AppendLine($"# Unique Edges Summary:");
                sb.AppendLine($"Total unique edges: {allEdges.Count}");
                sb.AppendLine("Use edge vertex pairs for bevel operations.");
            }

            return sb.ToString();
        });
    }
}
#endif
