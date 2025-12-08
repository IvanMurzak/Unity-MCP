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
            "ProBuilder_Extrude",
            Title = "Extrude ProBuilder faces"
        )]
        [Description(@"Extrudes selected faces of a ProBuilder mesh along their normals.
Use ProBuilder_GetMeshInfo first to get face indices.
Extrusion creates new geometry by pushing faces outward (or inward with negative distance).")]
        public string Extrude
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to extrude. Use ProBuilder_GetMeshInfo to get valid face indices.")]
            int[] faceIndices,
            [Description("Distance to extrude the faces. Positive values extrude outward along face normals, negative values extrude inward.")]
            float distance = 0.5f,
            [Description("Extrusion method: 0 = Individual (each face extrudes independently), 1 = FaceNormal (faces extrude as a group along averaged normal), 2 = VertexNormal (vertices move along their normals).")]
            int extrudeMethod = 1
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
            var faceCount = faces.Count();
            if (faceCount == 0)
                return Error.MeshHasNoFaces();

            // Validate face indices
            var invalidIndices = faceIndices.Where(i => i < 0 || i >= faceCount).ToList();
            if (invalidIndices.Any())
            {
                return $"[Error] Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.";
            }

            // Get the faces to extrude
            var facesToExtrude = faceIndices.Select(i => faces[i]).ToArray();

            // Parse extrude method
            var method = extrudeMethod switch
            {
                0 => ExtrudeMethod.IndividualFaces,
                1 => ExtrudeMethod.FaceNormal,
                2 => ExtrudeMethod.VertexNormal,
                _ => ExtrudeMethod.FaceNormal
            };

            // Perform extrusion
            Face[]? newFaces = null;
            try
            {
                newFaces = proBuilderMesh.Extrude(facesToExtrude, method, distance);
            }
            catch (System.Exception ex)
            {
                return Error.ExtrusionFailed(ex.Message);
            }

            if (newFaces == null || newFaces.Length == 0)
            {
                return Error.ExtrusionFailed("No new faces were created. The operation may not be valid for this mesh configuration.");
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Extruded {facesToExtrude.Length} face(s) by {distance} units.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Extruded Face Indices: {string.Join(", ", faceIndices)}");
            sb.AppendLine($"- Extrude Method: {method}");
            sb.AppendLine($"- Distance: {distance}");
            sb.AppendLine($"- New Faces Created: {newFaces.Length}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Total Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Total Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Total Edge Count: {proBuilderMesh.edgeCount}");
            sb.AppendLine();

            // Show new face indices
            var allFaces = proBuilderMesh.faces;
            var newFaceIndices = new List<int>();
            var allFaceCount = allFaces.Count();
            for (int i = 0; i < allFaceCount; i++)
            {
                if (newFaces.Contains(allFaces[i]))
                    newFaceIndices.Add(i);
            }

            if (newFaceIndices.Any())
            {
                sb.AppendLine($"# New Face Indices (for further operations):");
                sb.AppendLine($"  {string.Join(", ", newFaceIndices)}");
            }

            return sb.ToString();
        });
    }
}
#endif
