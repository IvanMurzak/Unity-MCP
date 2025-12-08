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
            "ProBuilder_MergeObjects",
            Title = "Merge multiple ProBuilder meshes into one"
        )]
        [Description(@"Combines multiple ProBuilder meshes into a single mesh.
Useful for optimizing draw calls or creating a unified object from parts.
The first mesh in the list becomes the target that others merge into.

Example: Merge a table made of separate leg and top meshes into one object.")]
        public string MergeObjects
        (
            [Description("Array of GameObject references with ProBuilderMesh components to merge. First object becomes the merge target.")]
            GameObjectRef[] gameObjectRefs,
            [Description("If true, delete the source GameObjects after merging (except the target). Default is true.")]
            bool deleteSourceObjects = true
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRefs == null || gameObjectRefs.Length < 2)
                return "[Error] At least 2 GameObjects are required for merging.";

            // Find all GameObjects and their ProBuilderMesh components
            var meshes = new List<ProBuilderMesh>();
            var gameObjects = new List<GameObject>();

            for (int i = 0; i < gameObjectRefs.Length; i++)
            {
                var goRef = gameObjectRefs[i];
                if (goRef?.IsValid != true)
                    return $"[Error] Invalid GameObject reference at index {i}.";

                var go = goRef.FindGameObject(out var error);
                if (error != null)
                    return $"[Error] {error}";

                if (go == null)
                    return $"[Error] GameObject not found at index {i}.";

                var pbMesh = go.GetComponent<ProBuilderMesh>();
                if (pbMesh == null)
                    return $"[Error] GameObject '{go.name}' at index {i} does not have a ProBuilderMesh component.";

                meshes.Add(pbMesh);
                gameObjects.Add(go);
            }

            var targetMesh = meshes[0];
            var targetGo = gameObjects[0];
            var originalNames = gameObjects.Select(g => g.name).ToList();

            // Calculate totals before merge
            var totalFacesBefore = meshes.Sum(m => m.faceCount);
            var totalVerticesBefore = meshes.Sum(m => m.vertexCount);

            // Perform merge
            List<ProBuilderMesh>? resultMeshes = null;
            try
            {
                resultMeshes = CombineMeshes.Combine(meshes, targetMesh);
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to merge meshes: {ex.Message}";
            }

            if (resultMeshes == null || resultMeshes.Count == 0)
            {
                return "[Error] Merge failed - no meshes returned.";
            }

            // Delete source objects if requested (skip the target)
            var deletedCount = 0;
            if (deleteSourceObjects)
            {
                for (int i = 1; i < gameObjects.Count; i++)
                {
                    if (gameObjects[i] != null)
                    {
                        Object.DestroyImmediate(gameObjects[i]);
                        deletedCount++;
                    }
                }
            }

            // Rebuild mesh
            foreach (var mesh in resultMeshes)
            {
                mesh.ToMesh();
                mesh.Refresh();
                EditorUtility.SetDirty(mesh);
                EditorUtility.SetDirty(mesh.gameObject);
            }

            EditorApplication.RepaintHierarchyWindow();

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Merged {meshes.Count} ProBuilder meshes into {resultMeshes.Count} mesh(es).");
            sb.AppendLine();
            sb.AppendLine("# Source Objects:");
            for (int i = 0; i < originalNames.Count; i++)
            {
                var status = i == 0 ? "(target)" : (deleteSourceObjects ? "(deleted)" : "(kept)");
                sb.AppendLine($"  [{i}]: {originalNames[i]} {status}");
            }
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Target Object: {targetGo.name}");
            sb.AppendLine($"- Target InstanceID: {targetGo.GetInstanceID()}");
            sb.AppendLine($"- Result Mesh Count: {resultMeshes.Count}");
            sb.AppendLine($"- Objects Deleted: {deletedCount}");
            sb.AppendLine();
            sb.AppendLine("# Merged Mesh Info:");
            sb.AppendLine($"- Total Faces: {totalFacesBefore} → {resultMeshes.Sum(m => m.faceCount)}");
            sb.AppendLine($"- Total Vertices: {totalVerticesBefore} → {resultMeshes.Sum(m => m.vertexCount)}");

            if (resultMeshes.Count > 1)
            {
                sb.AppendLine();
                sb.AppendLine("Note: Multiple meshes were created due to vertex limit. New meshes:");
                for (int i = 1; i < resultMeshes.Count; i++)
                {
                    sb.AppendLine($"  - {resultMeshes[i].gameObject.name} (InstanceID: {resultMeshes[i].gameObject.GetInstanceID()})");
                }
            }

            return sb.ToString();
        });
    }
}
#endif
