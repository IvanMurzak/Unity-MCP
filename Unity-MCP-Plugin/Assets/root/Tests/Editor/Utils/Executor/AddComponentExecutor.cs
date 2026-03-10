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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class AddComponentExecutor<T> : LazyNodeExecutor where T : UnityEngine.Component
    {
        protected readonly GameObjectRef _gameObjectRef;

        public T? Component { get; protected set; }
        public GameObject? GameObject { get; protected set; }

        public AddComponentExecutor(GameObjectRef gameObjectRef) : base()
        {
            _gameObjectRef = gameObjectRef ?? throw new ArgumentNullException(nameof(gameObjectRef));

            SetAction(() =>
            {
                var componentTypeName = typeof(T).Name;
                Debug.Log($"Adding component {componentTypeName} to GameObject");

                // Find the GameObject
                GameObject = _gameObjectRef.FindGameObject(out var error);

                if (error != null)
                {
                    Debug.LogError($"Error finding GameObject: {error}");
                    return null;
                }

                if (GameObject == null)
                {
                    Debug.LogError("GameObject not found.");
                    return null;
                }

                // Add the component
                Component = GameObject.AddComponent<T>();

                if (Component == null)
                {
                    Debug.LogWarning($"Component of type {componentTypeName} could not be added to GameObject '{GameObject.name}'.");
                    return null;
                }

                EditorUtility.SetDirty(GameObject);

                Debug.Log($"Added component {componentTypeName} to GameObject '{GameObject.name}' (Component InstanceID: {Component.GetInstanceID()})");

                return Component;
            });
        }

        protected override void PostExecute(object? input)
        {
            if (Component != null && GameObject != null)
            {
                Debug.Log($"Removing component {typeof(T).Name} from GameObject: {GameObject.name}");
                UnityEngine.Object.DestroyImmediate(Component);
                Component = null;
            }
            base.PostExecute(input);
        }
    }
}
