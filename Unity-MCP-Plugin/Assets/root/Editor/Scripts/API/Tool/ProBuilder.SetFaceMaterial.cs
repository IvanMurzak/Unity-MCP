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
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_SetFaceMaterial",
            Title = "Set material on ProBuilder faces"
        )]
        [Description(@"Assigns a material to specific faces of a ProBuilder mesh.
You can select faces by index OR by direction (semantic selection).
This enables multi-material meshes where different faces have different materials.

Examples:
- Set material on top face: faceDirection=""up""
- Set material on specific faces: faceIndices=[0, 2, 4]")]
        public string SetFaceMaterial
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Path to the material asset (e.g., 'Assets/Materials/MyMaterial.mat') or material name.")]
            string materialPath,
            [Description("Array of face indices to apply the material to. Use this OR faceDirection, not both. Use ProBuilder_GetMeshInfo to get valid face indices.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction: up, down, left, right, forward, back. Use this OR faceIndices, not both.")]
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

            if (string.IsNullOrEmpty(materialPath))
                return "[Error] Material path is empty. Please provide a valid material path.";

            // Resolve face indices from either direct indices or semantic direction
            int[] resolvedFaceIndices;
            string selectionMethod;

            if (faceIndices != null && faceIndices.Length > 0)
            {
                resolvedFaceIndices = faceIndices;
                selectionMethod = "by index";
            }
            else if (!string.IsNullOrEmpty(faceDirection))
            {
                var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection, out var selectionError);
                if (selectionError != null)
                    return $"[Error] {selectionError}";
                resolvedFaceIndices = selectedIndices!;
                selectionMethod = $"by direction '{faceDirection}'";
            }
            else
            {
                return "[Error] Either faceIndices or faceDirection must be provided.";
            }

            // Try to load the material
            Material? material = null;

            // First try as asset path
            if (materialPath.StartsWith("Assets/"))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }

            // If not found, try to find by name
            if (material == null)
            {
                var guids = AssetDatabase.FindAssets($"t:Material {materialPath}");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }

            if (material == null)
            {
                return $"[Error] Material not found at path '{materialPath}'. Ensure the path is correct or the material exists in the project.";
            }

            var faces = proBuilderMesh.faces;
            var faceCount = faces.Count();
            if (faceCount == 0)
                return Error.MeshHasNoFaces();

            // Validate face indices
            var invalidIndices = resolvedFaceIndices.Where(i => i < 0 || i >= faceCount).ToList();
            if (invalidIndices.Any())
            {
                return $"[Error] Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.";
            }

            // Get current materials on the renderer
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return "[Error] No MeshRenderer found on the GameObject.";
            }

            var materials = renderer.sharedMaterials.ToList();

            // Find or add the material to the materials list
            var materialIndex = materials.IndexOf(material);
            if (materialIndex < 0)
            {
                materialIndex = materials.Count;
                materials.Add(material);
                renderer.sharedMaterials = materials.ToArray();
            }

            // Assign the submesh index to the selected faces
            var selectedFaces = resolvedFaceIndices.Select(i => faces[i]).ToArray();
            foreach (var face in selectedFaces)
            {
                face.submeshIndex = materialIndex;
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Applied material '{material.name}' to {resolvedFaceIndices.Length} face(s) {selectionMethod}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Material: {material.name}");
            sb.AppendLine($"- Material Index: {materialIndex}");
            sb.AppendLine($"- Face Selection: {selectionMethod}");
            sb.AppendLine($"- Faces Updated: {string.Join(", ", resolvedFaceIndices)}");
            sb.AppendLine();
            sb.AppendLine("# Mesh Materials:");
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                var mat = renderer.sharedMaterials[i];
                sb.AppendLine($"  [{i}]: {(mat != null ? mat.name : "null")}");
            }

            return sb.ToString();
        });
    }
}
#endif
