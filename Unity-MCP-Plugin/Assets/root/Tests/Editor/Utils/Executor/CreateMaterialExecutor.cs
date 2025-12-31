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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateMaterialExecutor : BaseCreateAssetExecutor<Material>
    {
        protected readonly string _shaderName;

        public CreateMaterialExecutor(string materialName, string shaderName, params string[] folders) : base(materialName, folders)
        {
            _shaderName = shaderName ?? throw new ArgumentNullException(nameof(shaderName));

            if (!materialName.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Material name should include a .mat file extension.", nameof(materialName));

            SetAction(() =>
            {
                Debug.Log($"Creating material at path: {AssetPath} with shader: {_shaderName}");
                Asset = new Material(Shader.Find(_shaderName));
                AssetDatabase.CreateAsset(Asset, AssetPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            });
        }
    }
}
