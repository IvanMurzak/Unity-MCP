/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_Create",
            Title = "Create a new GameObject in opened Prefab or in a Scene"
        )]
        [Description(@"Create a new GameObject at specific path.
if needed - provide proper 'position', 'rotation' and 'scale' to reduce amount of operations.")]
        public string Create
        (
            [Description("Name of the new GameObject.")]
            string name,
            [Description("Parent GameObject reference. If not provided, the GameObject will be created at the root of the scene or prefab.")]
            GameObjectRef? parentGameObjectRef = null,
            [Description("Transform position of the GameObject.")]
            Vector3? position = null,
            [Description("Transform rotation of the GameObject. Euler angles in degrees.")]
            Vector3? rotation = null,
            [Description("Transform scale of the GameObject.")]
            Vector3? scale = null,
            [Description("World or Local space of transform.")]
            bool isLocalSpace = false,
            [Description("-1 - No primitive type; 0 - Cube; 1 - Sphere; 2 - Capsule; 3 - Cylinder; 4 - Plane; 5 - Quad.")]
            int primitiveType = -1
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(name))
                return Error.GameObjectNameIsEmpty();

            var parentGo = default(GameObject);
            if (parentGameObjectRef?.IsValid ?? false)
            {
                parentGo = parentGameObjectRef.FindGameObject(out var error);
                if (error != null)
                    return $"[Error] {error}";
            }

            position ??= Vector3.zero;
            rotation ??= Vector3.zero;
            scale ??= Vector3.one;

            var go = primitiveType switch
            {
                0 => GameObject.CreatePrimitive(PrimitiveType.Cube),
                1 => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                2 => GameObject.CreatePrimitive(PrimitiveType.Capsule),
                3 => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                4 => GameObject.CreatePrimitive(PrimitiveType.Plane),
                5 => GameObject.CreatePrimitive(PrimitiveType.Quad),
                _ => new GameObject(name)
            };
            go.name = name;

            // Set parent if provided
            if (parentGo != null)
                go.transform.SetParent(parentGo.transform, false);

            // Set the transform properties
            go.SetTransform(
                position: position,
                rotation: rotation,
                scale: scale,
                isLocalSpace: isLocalSpace);

            EditorUtility.SetDirty(go);
            EditorApplication.RepaintHierarchyWindow();

            return $"[Success] Created GameObject.\n{go.Print()}";
        });
    }
}
