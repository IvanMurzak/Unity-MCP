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
            "ProBuilder_DeleteFaces",
            Title = "Delete ProBuilder faces"
        )]
        [Description(@"Deletes selected faces from a ProBuilder mesh.
Use ProBuilder_GetMeshInfo first to get face indices.
Deleting faces creates holes in the mesh or removes geometry entirely.")]
        public string DeleteFaces
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to delete. Use ProBuilder_GetMeshInfo to get valid face indices.")]
            int[] faceIndices
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

            if (faceIndices == null || faceIndices.Length == 0)
                return Error.NoFacesProvided();

            var faces = proBuilderMesh.faces;
            if (faces.Count == 0)
                return Error.MeshHasNoFaces();

            // Validate face indices
            var invalidIndices = faceIndices.Where(i => i < 0 || i >= faces.Count).ToList();
            if (invalidIndices.Any())
            {
                return $"[Error] Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faces.Count - 1}.";
            }

            // Check if we're deleting all faces
            if (faceIndices.Length >= faces.Count)
            {
                return "[Error] Cannot delete all faces from a mesh. At least one face must remain.";
            }

            var originalFaceCount = proBuilderMesh.faceCount;
            var originalVertexCount = proBuilderMesh.vertexCount;

            // Get the faces to delete
            var facesToDelete = faceIndices.Select(i => faces[i]).ToArray();

            // Perform deletion
            try
            {
                proBuilderMesh.DeleteFaces(facesToDelete);
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to delete faces: {ex.Message}";
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Deleted {faceIndices.Length} face(s) from the mesh.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Deleted Face Indices: {string.Join(", ", faceIndices)}");
            sb.AppendLine($"- Faces Removed: {originalFaceCount - proBuilderMesh.faceCount}");
            sb.AppendLine($"- Vertices Removed: {originalVertexCount - proBuilderMesh.vertexCount}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Total Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Total Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Total Edge Count: {proBuilderMesh.edgeCount}");
            sb.AppendLine();
            sb.AppendLine("Note: Face indices have changed after deletion. Use ProBuilder_GetMeshInfo to get updated indices.");

            return sb.ToString();
        });
    }
}
#endif
