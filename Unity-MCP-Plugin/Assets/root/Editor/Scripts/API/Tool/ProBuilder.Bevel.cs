/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
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
            "ProBuilder_Bevel",
            Title = "Bevel ProBuilder edges"
        )]
        [Description(@"Bevels selected edges of a ProBuilder mesh, creating chamfered corners.
Use ProBuilder_GetMeshInfo to identify edges by their vertex pairs.
Beveling replaces sharp edges with angled faces for a smoother appearance.")]
        public string Bevel
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is defined by two vertex indices [vertexA, vertexB]. Example: [[0,1], [2,3]] bevels edges from vertex 0 to 1 and from vertex 2 to 3.")]
            int[][] edges,
            [Description("Bevel amount from 0 (no bevel) to 1 (maximum bevel reaching face center). Recommended values: 0.05 to 0.2.")]
            float amount = 0.1f
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

            if (edges == null || edges.Length == 0)
                return Error.NoEdgesProvided();

            // Validate and convert edges
            var edgeList = new List<Edge>();
            var vertexCount = proBuilderMesh.vertexCount;

            foreach (var edgeDef in edges)
            {
                if (edgeDef == null || edgeDef.Length != 2)
                {
                    return "[Error] Each edge must be defined as an array of exactly 2 vertex indices. Example: [0, 1]";
                }

                var vertA = edgeDef[0];
                var vertB = edgeDef[1];

                if (vertA < 0 || vertA >= vertexCount)
                    return $"[Error] Vertex index {vertA} is out of range. Valid range: 0 to {vertexCount - 1}.";
                if (vertB < 0 || vertB >= vertexCount)
                    return $"[Error] Vertex index {vertB} is out of range. Valid range: 0 to {vertexCount - 1}.";

                edgeList.Add(new Edge(vertA, vertB));
            }

            // Clamp amount to valid range
            amount = Mathf.Clamp(amount, 0.001f, 0.999f);

            // Perform bevel
            List<Face>? newFaces = null;
            try
            {
                newFaces = UnityEngine.ProBuilder.MeshOperations.Bevel.BevelEdges(proBuilderMesh, edgeList, amount);
            }
            catch (System.Exception ex)
            {
                return Error.BevelFailed(ex.Message);
            }

            if (newFaces == null || newFaces.Count == 0)
            {
                return Error.BevelFailed("No new faces were created. The edges may not be valid for beveling or may already be at maximum bevel.");
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Beveled {edgeList.Count} edge(s) with amount {amount}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Edges Beveled: {edgeList.Count}");
            sb.AppendLine($"- Bevel Amount: {amount}");
            sb.AppendLine($"- New Faces Created: {newFaces.Count}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Total Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Total Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Total Edge Count: {proBuilderMesh.edgeCount}");

            return sb.ToString();
        });
    }
}
#endif
