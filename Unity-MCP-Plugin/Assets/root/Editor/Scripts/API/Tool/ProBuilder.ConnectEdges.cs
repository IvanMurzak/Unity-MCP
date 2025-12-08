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
            "ProBuilder_ConnectEdges",
            Title = "Connect edges in a ProBuilder mesh"
        )]
        [Description(@"Inserts new edges connecting the midpoints of selected edges within faces.
If a face has more than 2 edges to connect, a center vertex is added.
This is useful for creating new edge loops and adding geometry detail.

Examples:
- Connect opposite edges of top face: faceDirection=""up""
- Connect specific edges: edges=[[0,1], [2,3]]")]
        public string ConnectEdges
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is [vertexA, vertexB]. Use ProBuilder_GetMeshInfo to get vertex indices.")]
            int[][]? edges = null,
            [Description("Semantic face selection - connect edges of faces facing this direction: up, down, left, right, forward, back.")]
            string? faceDirection = null
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

            // Resolve edges from either direct indices or semantic direction
            List<Edge> edgesToConnect;
            string selectionMethod;

            if (edges != null && edges.Length > 0)
            {
                // Validate edge definitions
                foreach (var edge in edges)
                {
                    if (edge == null || edge.Length < 2)
                        return "[Error] Each edge must have exactly 2 vertex indices [vertexA, vertexB].";
                }

                edgesToConnect = edges.Select(e => new Edge(e[0], e[1])).ToList();
                selectionMethod = "by vertex indices";
            }
            else if (!string.IsNullOrEmpty(faceDirection))
            {
                var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection, out var selectionError);
                if (selectionError != null)
                    return $"[Error] {selectionError}";

                // Get all edges from the selected faces
                var faces = proBuilderMesh.faces;
                edgesToConnect = new List<Edge>();
                foreach (var faceIndex in selectedIndices!)
                {
                    edgesToConnect.AddRange(faces[faceIndex].edges);
                }
                // Remove duplicates
                edgesToConnect = edgesToConnect.Distinct().ToList();
                selectionMethod = $"from faces facing '{faceDirection}'";
            }
            else
            {
                return "[Error] Either edges or faceDirection must be provided.";
            }

            if (edgesToConnect.Count < 2)
                return "[Error] At least 2 edges are required for connection.";

            var originalFaceCount = proBuilderMesh.faceCount;
            var originalEdgeCount = proBuilderMesh.edgeCount;

            // Perform connection
            Face[]? newFaces = null;
            Edge[]? newEdges = null;
            try
            {
                var result = ConnectElements.Connect(proBuilderMesh, edgesToConnect);
                newFaces = result.item1;
                newEdges = result.item2;
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to connect edges: {ex.Message}";
            }

            if ((newFaces == null || newFaces.Length == 0) && (newEdges == null || newEdges.Length == 0))
            {
                return "[Error] Connection failed - no new geometry created. The edges may not be suitable for connection.";
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Connected {edgesToConnect.Count} edge(s) {selectionMethod}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Edge Selection: {selectionMethod}");
            sb.AppendLine($"- Edges Connected: {edgesToConnect.Count}");
            sb.AppendLine($"- New Faces Created: {newFaces?.Length ?? 0}");
            sb.AppendLine($"- New Edges Created: {newEdges?.Length ?? 0}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Face Count: {originalFaceCount} → {proBuilderMesh.faceCount} (+{proBuilderMesh.faceCount - originalFaceCount})");
            sb.AppendLine($"- Edge Count: {originalEdgeCount} → {proBuilderMesh.edgeCount} (+{proBuilderMesh.edgeCount - originalEdgeCount})");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine();
            sb.AppendLine("Note: Face indices have changed. Use ProBuilder_GetMeshInfo to see updated indices.");

            return sb.ToString();
        });
    }
}
#endif
