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
using System;
using System.ComponentModel;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "ProBuilder_CreateShape",
            Title = "Create a ProBuilder shape"
        )]
        [Description(@"Creates a new ProBuilder mesh shape in the scene. ProBuilder shapes are editable 3D meshes
that can be modified using other ProBuilder tools like extrusion, beveling, etc.
Available shape types: Cube, Stair, CurvedStair, Prism, Cylinder, Plane, Door, Pipe, Cone, Sprite, Arch, Sphere, Torus.")]
        public string CreateShape
        (
            [Description("The type of shape to create. Options: Cube, Stair, CurvedStair, Prism, Cylinder, Plane, Door, Pipe, Cone, Sprite, Arch, Sphere, Torus.")]
            string shapeType,
            [Description("Name of the new GameObject.")]
            string? name = null,
            [Description("Parent GameObject reference. If not provided, the shape will be created at the root of the scene.")]
            GameObjectRef? parentGameObjectRef = null,
            [Description("Position of the shape in world or local space.")]
            Vector3? position = null,
            [Description("Rotation of the shape in euler angles (degrees).")]
            Vector3? rotation = null,
            [Description("Scale of the shape.")]
            Vector3? scale = null,
            [Description("Size of the shape (width, height, depth). Default is (1, 1, 1).")]
            Vector3? size = null,
            [Description("If true, position/rotation/scale are in local space relative to parent.")]
            bool isLocalSpace = false
        )
        => MainThread.Instance.Run(() =>
        {
            // Parse shape type
            if (!Enum.TryParse<ShapeType>(shapeType, ignoreCase: true, out var parsedShapeType))
            {
                var validTypes = string.Join(", ", Enum.GetNames(typeof(ShapeType)));
                return $"[Error] Invalid shape type '{shapeType}'. Valid types: {validTypes}";
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
            scale ??= Vector3.one;
            size ??= Vector3.one;

            // Create the ProBuilder shape
            var proBuilderMesh = ShapeGenerator.CreateShape(parsedShapeType, PivotLocation.Center);

            if (proBuilderMesh == null)
                return $"[Error] Failed to create ProBuilder shape of type '{shapeType}'.";

            var go = proBuilderMesh.gameObject;
            go.name = name ?? $"ProBuilder {shapeType}";

            // Set parent
            if (parentGo != null)
                go.transform.SetParent(parentGo.transform, false);

            // Apply transform
            if (isLocalSpace)
            {
                go.transform.localPosition = position.Value;
                go.transform.localEulerAngles = rotation.Value;
                go.transform.localScale = scale.Value;
            }
            else
            {
                go.transform.position = position.Value;
                go.transform.eulerAngles = rotation.Value;
                go.transform.localScale = scale.Value;
            }

            // Apply size by scaling vertices if size is different from default
            if (size.Value != Vector3.one)
            {
                var positions = proBuilderMesh.positions;
                var bounds = proBuilderMesh.mesh.bounds;
                var currentSize = bounds.size;

                // Calculate scale factors
                var scaleFactors = new Vector3(
                    currentSize.x > 0 ? size.Value.x / currentSize.x : 1,
                    currentSize.y > 0 ? size.Value.y / currentSize.y : 1,
                    currentSize.z > 0 ? size.Value.z / currentSize.z : 1
                );

                var newPositions = new Vector3[positions.Count];
                for (int i = 0; i < positions.Count; i++)
                {
                    newPositions[i] = Vector3.Scale(positions[i], scaleFactors);
                }
                proBuilderMesh.positions = newPositions;
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();
            }

            // Mark as dirty for saving
            EditorUtility.SetDirty(go);
            EditorApplication.RepaintHierarchyWindow();

            // Build response with mesh info
            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Created ProBuilder {shapeType} shape.");
            sb.AppendLine($"# GameObject Info:");
            sb.AppendLine($"- Name: {go.name}");
            sb.AppendLine($"- InstanceID: {go.GetInstanceID()}");
            sb.AppendLine($"- Position: {go.transform.position}");
            sb.AppendLine($"- Rotation: {go.transform.eulerAngles}");
            sb.AppendLine($"- Scale: {go.transform.localScale}");
            sb.AppendLine();
            sb.AppendLine($"# ProBuilderMesh Info:");
            sb.AppendLine($"- Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Edge Count: {proBuilderMesh.edgeCount}");

            return sb.ToString();
        });
    }
}
#endif
