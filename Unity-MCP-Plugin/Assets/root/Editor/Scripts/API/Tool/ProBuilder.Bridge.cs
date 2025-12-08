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
using System.ComponentModel;
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
            "ProBuilder_Bridge",
            Title = "Bridge two edges in a ProBuilder mesh"
        )]
        [Description(@"Creates a new face connecting two edges.
Useful for connecting separate parts of geometry or filling gaps.

Example:
- edgeA=[0,1], edgeB=[4,5] creates a quad face between the two edges")]
        public string Bridge
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("First edge as [vertexA, vertexB].")]
            int[] edgeA,
            [Description("Second edge as [vertexA, vertexB].")]
            int[] edgeB,
            [Description("If true, allows creation of non-manifold geometry (edges shared by more than 2 faces).")]
            bool allowNonManifold = false
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

            // Validate edges
            if (edgeA == null || edgeA.Length < 2)
                return "[Error] edgeA must have exactly 2 vertex indices [vertexA, vertexB].";
            if (edgeB == null || edgeB.Length < 2)
                return "[Error] edgeB must have exactly 2 vertex indices [vertexA, vertexB].";

            var edge1 = new Edge(edgeA[0], edgeA[1]);
            var edge2 = new Edge(edgeB[0], edgeB[1]);

            var originalFaceCount = proBuilderMesh.faceCount;

            // Perform bridge
            Face? newFace = null;
            try
            {
                newFace = proBuilderMesh.Bridge(edge1, edge2, allowNonManifold);
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to bridge edges: {ex.Message}";
            }

            if (newFace == null)
            {
                return "[Error] Bridge failed - could not create face between the specified edges. Ensure the edges are valid and not already connected.";
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Find new face index
            var faces = proBuilderMesh.faces;
            var newFaceIndex = -1;
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i] == newFace)
                {
                    newFaceIndex = i;
                    break;
                }
            }

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Created bridge face between edges [{edgeA[0]},{edgeA[1]}] and [{edgeB[0]},{edgeB[1]}].");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Edge A: [{edgeA[0]} → {edgeA[1]}]");
            sb.AppendLine($"- Edge B: [{edgeB[0]} → {edgeB[1]}]");
            sb.AppendLine($"- New Face Index: {newFaceIndex}");
            sb.AppendLine($"- Allow Non-Manifold: {allowNonManifold}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Face Count: {originalFaceCount} → {proBuilderMesh.faceCount} (+{proBuilderMesh.faceCount - originalFaceCount})");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Edge Count: {proBuilderMesh.edgeCount}");

            return sb.ToString();
        });
    }
}
#endif
