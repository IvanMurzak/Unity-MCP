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
            "ProBuilder_FlipNormals",
            Title = "Flip face normals in a ProBuilder mesh"
        )]
        [Description(@"Reverses the normal direction of selected faces, flipping them inside-out.
Useful for creating interior spaces or fixing inverted faces.

Examples:
- Flip all faces: leave faceIndices and faceDirection empty
- Flip top face only: faceDirection=Up
- Flip specific faces: faceIndices=[0, 2, 4]")]
        public string FlipNormals
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to flip. If empty and faceDirection is empty, flips all faces.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction. If empty and faceIndices is empty, flips all faces.")]
            FaceDirection? faceDirection = null
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

            var faces = proBuilderMesh.faces;
            var faceCount = faces.Count();
            if (faceCount == 0)
                return Error.MeshHasNoFaces();

            // Resolve face indices
            int[] resolvedFaceIndices;
            string selectionMethod;

            if (faceIndices != null && faceIndices.Length > 0)
            {
                resolvedFaceIndices = faceIndices;
                selectionMethod = "by index";
            }
            else if (faceDirection.HasValue)
            {
                var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                if (selectionError != null)
                    return $"[Error] {selectionError}";
                resolvedFaceIndices = selectedIndices!;
                selectionMethod = $"by direction '{faceDirection.Value}'";
            }
            else
            {
                // Flip all faces
                resolvedFaceIndices = Enumerable.Range(0, faceCount).ToArray();
                selectionMethod = "all faces";
            }

            // Validate face indices
            var invalidIndices = resolvedFaceIndices.Where(i => i < 0 || i >= faceCount).ToList();
            if (invalidIndices.Any())
            {
                return $"[Error] Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.";
            }

            // Get faces to flip
            var facesToFlip = resolvedFaceIndices.Select(i => faces[i]).ToArray();

            // Flip normals by reversing the faces
            try
            {
                foreach (var face in facesToFlip)
                {
                    face.Reverse();
                }
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to flip normals: {ex.Message}";
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Flipped normals on {resolvedFaceIndices.Length} face(s) {selectionMethod}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Face Selection: {selectionMethod}");
            sb.AppendLine($"- Faces Flipped: {resolvedFaceIndices.Length}");
            if (resolvedFaceIndices.Length <= 20)
            {
                sb.AppendLine($"- Face Indices: {string.Join(", ", resolvedFaceIndices)}");
            }
            sb.AppendLine();
            sb.AppendLine("# Mesh Info:");
            sb.AppendLine($"- Total Faces: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Total Vertices: {proBuilderMesh.vertexCount}");

            return sb.ToString();
        });
    }
}
#endif
