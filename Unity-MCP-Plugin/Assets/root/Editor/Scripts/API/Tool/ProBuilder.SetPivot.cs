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
    /// <summary>
    /// Pivot location options for SetPivot tool.
    /// </summary>
    public enum MeshPivotLocation
    {
        /// <summary>Center of mesh bounds</summary>
        Center,
        /// <summary>First vertex position</summary>
        FirstVertex,
        /// <summary>Custom world position</summary>
        Custom
    }

    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_SetPivot",
            Title = "Set the pivot point of a ProBuilder mesh"
        )]
        [Description(@"Changes the pivot (origin) point of a ProBuilder mesh.
The mesh geometry is adjusted so the pivot moves without changing the visual position.

Examples:
- Center the pivot: pivotLocation=Center
- Set pivot to first vertex: pivotLocation=FirstVertex
- Set custom pivot: pivotLocation=Custom, customPosition=(0, 0, 0)")]
        public string SetPivot
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Where to place the pivot.")]
            MeshPivotLocation pivotLocation = MeshPivotLocation.Center,
            [Description("Custom world position for pivot (only used when pivotLocation=Custom).")]
            Vector3? customPosition = null
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

            var oldPivot = go.transform.position;
            Vector3 newPivotWorld;

            switch (pivotLocation)
            {
                case MeshPivotLocation.Center:
                    // Get mesh bounds center in world space
                    var meshFilter = go.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        return "[Error] No mesh found on GameObject.";
                    var boundsCenter = meshFilter.sharedMesh.bounds.center;
                    newPivotWorld = go.transform.TransformPoint(boundsCenter);
                    break;

                case MeshPivotLocation.FirstVertex:
                    var positions = proBuilderMesh.positions;
                    if (positions == null || positions.Count == 0)
                        return "[Error] Mesh has no vertices.";
                    newPivotWorld = go.transform.TransformPoint(positions[0]);
                    break;

                case MeshPivotLocation.Custom:
                    if (!customPosition.HasValue)
                        return "[Error] customPosition is required when pivotLocation is Custom.";
                    newPivotWorld = customPosition.Value;
                    break;

                default:
                    return $"[Error] Unknown pivot location: {pivotLocation}";
            }

            // Calculate offset in local space
            var offset = go.transform.InverseTransformPoint(newPivotWorld);

            try
            {
                // Move all vertices by the negative offset
                var positions = proBuilderMesh.positions.ToArray();
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] -= offset;
                }
                proBuilderMesh.positions = positions;

                // Move the transform to compensate
                go.transform.position = newPivotWorld;

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to set pivot: {ex.Message}";
            }

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Set pivot to {pivotLocation}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Pivot Location: {pivotLocation}");
            sb.AppendLine($"- Old Pivot (world): ({oldPivot.x:F3}, {oldPivot.y:F3}, {oldPivot.z:F3})");
            sb.AppendLine($"- New Pivot (world): ({newPivotWorld.x:F3}, {newPivotWorld.y:F3}, {newPivotWorld.z:F3})");
            sb.AppendLine($"- Offset Applied: ({offset.x:F3}, {offset.y:F3}, {offset.z:F3})");
            sb.AppendLine();
            sb.AppendLine("# GameObject Info:");
            sb.AppendLine($"- Name: {go.name}");
            sb.AppendLine($"- New Position: {go.transform.position}");

            return sb.ToString();
        });
    }
}
#endif
