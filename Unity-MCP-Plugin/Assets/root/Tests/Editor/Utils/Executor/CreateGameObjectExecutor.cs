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
using System;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateGameObjectExecutor : LazyNodeExecutor
    {
        protected readonly string _name;
        protected readonly GameObjectRef? _parentGameObjectRef;
        protected readonly Vector3? _position;
        protected readonly Vector3? _rotation;
        protected readonly Vector3? _scale;
        protected readonly bool _isLocalSpace;
        protected readonly int _primitiveType;
        protected readonly bool _isActive;

        public GameObjectRef? GameObjectRef { get; protected set; }
        public GameObject? GameObject { get; protected set; }

        public CreateGameObjectExecutor(
            string name,
            GameObjectRef? parentGameObjectRef = null,
            Vector3? position = null,
            Vector3? rotation = null,
            Vector3? scale = null,
            bool isLocalSpace = false,
            int primitiveType = -1,
            bool isActive = true) : base()
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _parentGameObjectRef = parentGameObjectRef;
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _isLocalSpace = isLocalSpace;
            _primitiveType = primitiveType;
            _isActive = isActive;

            SetAction(() =>
            {
                Debug.Log($"Creating GameObject: {_name}");

                // Find parent if provided
                GameObject? parentGo = null;
                if (_parentGameObjectRef?.IsValid(out _) == true)
                {
                    parentGo = _parentGameObjectRef.FindGameObject(out var error);
                    if (error != null)
                    {
                        Debug.LogError($"Error finding parent GameObject: {error}");
                        return null;
                    }
                }

                // Create GameObject based on primitive type
                GameObject = _primitiveType switch
                {
                    0 => GameObject.CreatePrimitive(PrimitiveType.Cube),
                    1 => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                    2 => GameObject.CreatePrimitive(PrimitiveType.Capsule),
                    3 => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                    4 => GameObject.CreatePrimitive(PrimitiveType.Plane),
                    5 => GameObject.CreatePrimitive(PrimitiveType.Quad),
                    _ => new GameObject()
                };

                GameObject.name = _name;

                // Set parent if provided
                if (parentGo != null)
                {
                    GameObject.transform.SetParent(parentGo.transform, false);
                }

                // Set transform properties
                var pos = _position ?? Vector3.zero;
                var rot = _rotation ?? Vector3.zero;
                var scl = _scale ?? Vector3.one;

                if (_isLocalSpace)
                {
                    GameObject.transform.localPosition = pos;
                    GameObject.transform.localEulerAngles = rot;
                    GameObject.transform.localScale = scl;
                }
                else
                {
                    GameObject.transform.position = pos;
                    GameObject.transform.eulerAngles = rot;
                    GameObject.transform.localScale = scl;
                }

                // Set active state
                GameObject.SetActive(_isActive);

                // Create GameObjectRef
                GameObjectRef = new GameObjectRef(GameObject.GetInstanceID());

                EditorUtility.SetDirty(GameObject);
                EditorUtils.RepaintAllEditorWindows();

                Debug.Log($"Created GameObject: {_name} (InstanceID: {GameObject.GetInstanceID()})");

                return GameObject;
            });
        }

        protected override void PostExecute(object? input)
        {
            if (GameObject != null)
            {
                Debug.Log($"Destroying GameObject: {GameObject.name}");
                UnityEngine.Object.DestroyImmediate(GameObject);
                GameObject = null;
                GameObjectRef = null;
            }
            base.PostExecute(input);
        }
    }
}
