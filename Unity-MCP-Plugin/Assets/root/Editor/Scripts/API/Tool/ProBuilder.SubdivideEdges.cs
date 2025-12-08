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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_SubdivideEdges",
            Title = "Subdivide edges in a ProBuilder mesh"
        )]
        [Description(@"Inserts new vertices on edges, subdividing them into smaller segments.
Useful for adding detail to specific edges for further manipulation.

Examples:
- Subdivide all edges of top face: faceDirection=""up"", subdivisions=2
- Subdivide specific edges: edges=[[0,1], [2,3]], subdivisions=1")]
        public string SubdivideEdges
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is [vertexA, vertexB]. Use ProBuilder_GetMeshInfo to get vertex indices.")]
            int[][]? edges = null,
            [Description("Semantic face selection - subdivide all edges of faces facing this direction.")]
            FaceDirection? faceDirection = null,
            [Description("Number of subdivisions per edge. 1 = splits edge in half, 2 = splits into thirds, etc. Default is 1.")]
            int subdivisions = 1
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

            if (subdivisions < 1)
                return "[Error] Subdivisions must be at least 1.";

            // Resolve edges from either direct indices or semantic direction
            List<Edge> edgesToSubdivide;
            string selectionMethod;

            if (edges != null && edges.Length > 0)
            {
                // Validate edge definitions
                foreach (var edge in edges)
                {
                    if (edge == null || edge.Length < 2)
                        return "[Error] Each edge must have exactly 2 vertex indices [vertexA, vertexB].";
                }

                edgesToSubdivide = edges.Select(e => new Edge(e[0], e[1])).ToList();
                selectionMethod = "by vertex indices";
            }
            else if (faceDirection.HasValue)
            {
                var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                if (selectionError != null)
                    return $"[Error] {selectionError}";

                // Get all edges from the selected faces
                var faces = proBuilderMesh.faces;
                edgesToSubdivide = new List<Edge>();
                foreach (var faceIndex in selectedIndices!)
                {
                    edgesToSubdivide.AddRange(faces[faceIndex].edges);
                }
                // Remove duplicates
                edgesToSubdivide = edgesToSubdivide.Distinct().ToList();
                selectionMethod = $"from faces facing '{faceDirection.Value}'";
            }
            else
            {
                return "[Error] Either edges or faceDirection must be provided.";
            }

            if (edgesToSubdivide.Count == 0)
                return "[Error] No edges found to subdivide.";

            var originalVertexCount = proBuilderMesh.vertexCount;
            var originalEdgeCount = proBuilderMesh.edgeCount;

            // Perform subdivision
            List<Edge>? newEdges = null;
            try
            {
                newEdges = proBuilderMesh.AppendVerticesToEdge(edgesToSubdivide, subdivisions);
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to subdivide edges: {ex.Message}";
            }

            if (newEdges == null || newEdges.Count == 0)
            {
                return "[Error] Subdivision failed - no new edges created. The edges may be invalid for this mesh.";
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Subdivided {edgesToSubdivide.Count} edge(s) {selectionMethod} with {subdivisions} subdivision(s).");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Edge Selection: {selectionMethod}");
            sb.AppendLine($"- Edges Subdivided: {edgesToSubdivide.Count}");
            sb.AppendLine($"- Subdivisions Per Edge: {subdivisions}");
            sb.AppendLine($"- New Edges Created: {newEdges.Count}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Vertex Count: {originalVertexCount} → {proBuilderMesh.vertexCount} (+{proBuilderMesh.vertexCount - originalVertexCount})");
            sb.AppendLine($"- Edge Count: {originalEdgeCount} → {proBuilderMesh.edgeCount} (+{proBuilderMesh.edgeCount - originalEdgeCount})");
            sb.AppendLine($"- Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine();
            sb.AppendLine("Note: Use ProBuilder_GetMeshInfo to see updated vertex/edge indices.");

            return sb.ToString();
        });
    }
}
#endif
