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
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
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
        [Description(@"Retrieves information about a ProBuilder mesh including faces, vertices, and edges.
Use detail=""summary"" for a token-efficient overview showing face directions.
Use detail=""full"" for detailed face-by-face information.

TIP: With semantic face selection (faceDirection parameter) in Extrude/DeleteFaces/SetFaceMaterial,
you often don't need GetMeshInfo at all - just use faceDirection=""up"" etc. directly.")]
        public string GetMeshInfo
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Detail level: 'summary' for condensed face direction info (token-efficient), 'full' for detailed face-by-face data.")]
            string detail = "summary",
            [Description("If true, includes detailed vertex positions for each face (only with detail='full').")]
            bool includeVertexPositions = false,
            [Description("If true, includes edge information for each face (only with detail='full').")]
            bool includeEdges = true,
            [Description("Maximum number of faces to include in detail (only with detail='full'). Use -1 for all faces.")]
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
            var meshFilter = go.GetComponent<MeshFilter>();
            var bounds = meshFilter != null ? meshFilter.sharedMesh.bounds : new Bounds();
            sb.AppendLine("# Bounds:");
            sb.AppendLine($"- Center: {bounds.center}");
            sb.AppendLine($"- Size: {bounds.size}");
            sb.AppendLine($"- Min: {bounds.min}");
            sb.AppendLine($"- Max: {bounds.max}");
            sb.AppendLine();

            // Summary mode - condensed face direction info
            if (detail.ToLowerInvariant() == "summary")
            {
                var directionSummary = FaceSelectionHelper.GetFaceDirectionSummary(proBuilderMesh);
                var faces = proBuilderMesh.faces;
                var positions = proBuilderMesh.positions;

                sb.AppendLine("# Face Directions (use with faceDirection parameter):");
                foreach (var kvp in directionSummary)
                {
                    if (kvp.Value.Count > 0)
                    {
                        var dirName = kvp.Key;
                        var faceList = kvp.Value;
                        var centerStr = "";

                        // Show center of first face in this direction
                        if (faceList.Count > 0 && faceList[0] < faces.Count)
                        {
                            var center = FaceSelectionHelper.GetFaceCenter(faces[faceList[0]], positions);
                            centerStr = $" (first at {center.x:F1}, {center.y:F1}, {center.z:F1})";
                        }

                        sb.AppendLine($"- {dirName}: faces [{string.Join(", ", faceList)}]{centerStr}");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("TIP: Use faceDirection=\"up\" in Extrude/DeleteFaces/SetFaceMaterial instead of face indices.");
                sb.AppendLine("Use detail=\"full\" for detailed per-face information.");
            }
            else
            {
                // Full mode - detailed face-by-face info
                var faces = proBuilderMesh.faces;
                var positions = proBuilderMesh.positions;
                var faceCount = faces.Count();
                var facesToShow = maxFacesToShow < 0 ? faceCount : System.Math.Min(maxFacesToShow, faceCount);

                sb.AppendLine($"# Faces (showing {facesToShow} of {faceCount}):");
                sb.AppendLine("Use face indices for operations, or use faceDirection for semantic selection.");
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
                    var vertCount = faceVertices.Count();
                    center /= vertCount;

                    sb.AppendLine($"## Face {i}:");
                    sb.AppendLine($"  - Vertex Count: {vertCount}");
                    sb.AppendLine($"  - Triangle Count: {face.indexes.Count() / 3}");
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
                        sb.AppendLine($"  - Edges ({faceEdges.Count()}):");
                        foreach (var edge in faceEdges)
                        {
                            var p1 = positions[edge.a];
                            var p2 = positions[edge.b];
                            sb.AppendLine($"    - [{edge.a} -> {edge.b}]: ({p1.x:F2},{p1.y:F2},{p1.z:F2}) to ({p2.x:F2},{p2.y:F2},{p2.z:F2})");
                        }
                    }
                    sb.AppendLine();
                }

                if (facesToShow < faceCount)
                {
                    sb.AppendLine($"... and {faceCount - facesToShow} more faces. Use maxFacesToShow=-1 to see all.");
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
            }

            return sb.ToString();
        });
    }
}
#endif
