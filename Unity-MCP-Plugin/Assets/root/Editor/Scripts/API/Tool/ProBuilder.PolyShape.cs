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
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_CreatePolyShape",
            Title = "Create a ProBuilder shape from polygon points"
        )]
        [Description(@"Creates a 3D mesh from a 2D polygon outline. Perfect for:
- Floor plans and room layouts
- Custom terrain patches
- Architectural elements (walls, platforms)
- Any shape that can be defined by a 2D outline

The polygon is defined by an array of 2D points (x,z coordinates) that form the outline.
The shape is then extruded upward by the specified height.

Examples:
- Rectangle: points=[[0,0], [4,0], [4,3], [0,3]] height=2.5
- L-shape: points=[[0,0], [3,0], [3,2], [1,2], [1,3], [0,3]] height=3
- Triangle: points=[[0,0], [2,0], [1,1.7]] height=1")]
        public string CreatePolyShape
        (
            [Description("2D polygon points as [x,z] coordinates. Minimum 3 points. Points should be in clockwise or counter-clockwise order. Example: [[0,0], [4,0], [4,3], [0,3]] creates a 4x3 rectangle.")]
            float[][] points,
            [Description("Height to extrude the polygon upward. Default is 1.")]
            float height = 1f,
            [Description("Name of the new GameObject.")]
            string? name = null,
            [Description("Parent GameObject reference. If not provided, the shape will be created at the root of the scene.")]
            GameObjectRef? parentGameObjectRef = null,
            [Description("Position of the shape in world or local space.")]
            Vector3? position = null,
            [Description("Rotation of the shape in euler angles (degrees).")]
            Vector3? rotation = null,
            [Description("If true, flip the normals so the faces point inward instead of outward.")]
            bool flipNormals = false,
            [Description("If true, position/rotation are in local space relative to parent.")]
            bool isLocalSpace = false
        )
        => MainThread.Instance.Run(() =>
        {
            // Validate points
            if (points == null || points.Length < 3)
                return "[Error] At least 3 polygon points are required to create a shape.";

            // Validate each point has x,z coordinates
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null || points[i].Length < 2)
                    return $"[Error] Point at index {i} must have at least 2 coordinates [x,z].";
            }

            // Find parent if provided
            GameObject? parentGo = null;
            if (parentGameObjectRef?.IsValid ?? false)
            {
                parentGo = parentGameObjectRef.FindGameObject(out var error);
                if (error != null)
                    return $"[Error] {error}";
            }

            // Set defaults
            position ??= Vector3.zero;
            rotation ??= Vector3.zero;

            // Convert 2D points to 3D (x,z -> x,0,z)
            var points3D = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points3D[i] = new Vector3(points[i][0], 0f, points[i][1]);
            }

            // Create the ProBuilder mesh
            var go = new GameObject();
            var proBuilderMesh = go.AddComponent<ProBuilderMesh>();

            if (proBuilderMesh == null)
                return "[Error] Failed to create ProBuilderMesh component.";

            // Create the shape from polygon
            try
            {
                var result = proBuilderMesh.CreateShapeFromPolygon(points3D, height, flipNormals);
                if (result.status != ActionResult.Status.Success)
                {
                    Object.DestroyImmediate(go);
                    return $"[Error] Failed to create polygon shape: {result.notification}";
                }
            }
            catch (System.Exception ex)
            {
                Object.DestroyImmediate(go);
                return $"[Error] Failed to create polygon shape: {ex.Message}";
            }

            go.name = name ?? "ProBuilder PolyShape";

            // Set parent
            if (parentGo != null)
                go.transform.SetParent(parentGo.transform, false);

            // Apply transform
            if (isLocalSpace)
            {
                go.transform.localPosition = position.Value;
                go.transform.localEulerAngles = rotation.Value;
            }
            else
            {
                go.transform.position = position.Value;
                go.transform.eulerAngles = rotation.Value;
            }

            // Mark as dirty for saving
            EditorUtility.SetDirty(go);
            EditorApplication.RepaintHierarchyWindow();

            // Calculate bounds for info
            var meshFilter = go.GetComponent<MeshFilter>();
            var bounds = meshFilter != null && meshFilter.sharedMesh != null
                ? meshFilter.sharedMesh.bounds
                : new Bounds();

            // Build response
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Created ProBuilder PolyShape from {points.Length} points.");
            sb.AppendLine();
            sb.AppendLine("# GameObject Info:");
            sb.AppendLine($"- Name: {go.name}");
            sb.AppendLine($"- InstanceID: {go.GetInstanceID()}");
            sb.AppendLine($"- Position: {go.transform.position}");
            sb.AppendLine($"- Rotation: {go.transform.eulerAngles}");
            sb.AppendLine();
            sb.AppendLine("# Shape Info:");
            sb.AppendLine($"- Points: {points.Length}");
            sb.AppendLine($"- Height: {height}");
            sb.AppendLine($"- Flip Normals: {flipNormals}");
            sb.AppendLine($"- Bounds Size: {bounds.size}");
            sb.AppendLine();
            sb.AppendLine("# ProBuilderMesh Info:");
            sb.AppendLine($"- Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Edge Count: {proBuilderMesh.edgeCount}");
            sb.AppendLine();
            sb.AppendLine("# Input Points:");
            for (int i = 0; i < points.Length; i++)
            {
                sb.AppendLine($"  [{i}]: ({points[i][0]}, {points[i][1]})");
            }

            return sb.ToString();
        });
    }
}
#endif
