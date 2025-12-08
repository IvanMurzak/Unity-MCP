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
            "ProBuilder_ConvertToProBuilder",
            Title = "Convert mesh to ProBuilder"
        )]
        [Description(@"Converts a regular Unity mesh (MeshFilter) to an editable ProBuilder mesh.
This allows you to use ProBuilder tools on imported meshes or Unity primitives.
The original mesh data is preserved and made editable.")]
        public string ConvertToProBuilder
        (
            [Description("Reference to the GameObject with a MeshFilter component to convert.")]
            GameObjectRef gameObjectRef,
            [Description("If true, attempts to preserve quads in the mesh. If false, all faces become triangles.")]
            bool preserveQuads = true
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

            // Check if already a ProBuilder mesh
            var existingProBuilder = go.GetComponent<ProBuilderMesh>();
            if (existingProBuilder != null)
            {
                return $"[Error] GameObject '{go.name}' already has a ProBuilderMesh component. No conversion needed.";
            }

            // Get the mesh filter
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return $"[Error] GameObject '{go.name}' does not have a MeshFilter component. Cannot convert to ProBuilder.";
            }

            var sourceMesh = meshFilter.sharedMesh;
            if (sourceMesh == null)
            {
                return $"[Error] MeshFilter on '{go.name}' has no mesh assigned.";
            }

            // Perform conversion
            ProBuilderMesh proBuilderMesh;
            try
            {
                proBuilderMesh = go.AddComponent<ProBuilderMesh>();
                var importer = new MeshImporter(proBuilderMesh);
                importer.Import(go, new MeshImportSettings
                {
                    quads = preserveQuads,
                    smoothing = true,
                    smoothingAngle = 30f
                });

                // Rebuild to ensure mesh is valid
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();
            }
            catch (System.Exception ex)
            {
                // Clean up if failed
                if (go.GetComponent<ProBuilderMesh>() != null)
                    Object.DestroyImmediate(go.GetComponent<ProBuilderMesh>());

                return Error.MeshImportFailed(ex.Message);
            }

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Converted '{go.name}' to ProBuilder mesh.");
            sb.AppendLine();
            sb.AppendLine("# Original Mesh:");
            sb.AppendLine($"- Name: {sourceMesh.name}");
            sb.AppendLine($"- Vertex Count: {sourceMesh.vertexCount}");
            sb.AppendLine($"- Triangle Count: {sourceMesh.triangles.Length / 3}");
            sb.AppendLine();
            sb.AppendLine("# ProBuilder Mesh:");
            sb.AppendLine($"- GameObject InstanceID: {go.GetInstanceID()}");
            sb.AppendLine($"- Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Edge Count: {proBuilderMesh.edgeCount}");
            sb.AppendLine($"- Preserve Quads: {preserveQuads}");
            sb.AppendLine();
            sb.AppendLine("The mesh is now editable with ProBuilder tools (extrude, bevel, etc.).");

            return sb.ToString();
        });
    }
}
#endif
