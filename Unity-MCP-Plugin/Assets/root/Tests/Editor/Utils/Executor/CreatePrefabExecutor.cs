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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreatePrefabExecutor : BaseCreateAssetExecutor<GameObject>
    {
        private readonly GameObjectRef? _sourceGameObjectRef;

        public CreatePrefabExecutor(string assetName, GameObjectRef? sourceGameObjectRef = null, params string[] folders) : base(assetName, folders)
        {
            _sourceGameObjectRef = sourceGameObjectRef;

            SetAction<object?, object?>((input) =>
            {
                Debug.Log($"Creating Prefab: {AssetPath}");

                GameObject? sourceGo = null;

                if (_sourceGameObjectRef?.IsValid(out _) == true)
                {
                    sourceGo = _sourceGameObjectRef.FindGameObject(out var error);
                    if (error != null) Debug.LogError(error);
                }
                else if (input is GameObject go)
                {
                    sourceGo = go;
                }

                if (sourceGo == null)
                {
                    Debug.LogError("Source GameObject for Prefab creation not found.");
                    return null;
                }

                PrefabUtility.SaveAsPrefabAsset(sourceGo, AssetPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);

                if (Asset == null)
                {
                    Debug.LogError($"Failed to load created Prefab at {AssetPath}");
                }
                else
                {
                    Debug.Log($"Created Prefab: {AssetPath}");
                }

                return Asset;
            });
        }
    }
}
