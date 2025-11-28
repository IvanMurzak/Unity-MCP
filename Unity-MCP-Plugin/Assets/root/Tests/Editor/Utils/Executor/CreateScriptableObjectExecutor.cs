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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateScriptableObjectExecutor<T> : BaseCreateAssetExecutor<T> where T : ScriptableObject
    {
        public CreateScriptableObjectExecutor(string assetName, params string[] folders) : base(assetName, folders)
        {
            SetAction(() =>
            {
                Debug.Log($"Creating ScriptableObject: {AssetPath}");

                var so = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(so, AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Asset = AssetDatabase.LoadAssetAtPath<T>(AssetPath);

                if (Asset == null)
                {
                    Debug.LogError($"Failed to load created ScriptableObject at {AssetPath}");
                }
                else
                {
                    Debug.Log($"Created ScriptableObject: {AssetPath}");
                }

                return Asset;
            });
        }
    }
}
