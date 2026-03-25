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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CreateShaderExecutor : BaseCreateAssetExecutor<Shader>
    {
        protected readonly string _shaderSource;

        public CreateShaderExecutor(string shaderFileName, string shaderSource, params string[] folders) : base(shaderFileName, folders)
        {
            _shaderSource = shaderSource ?? throw new ArgumentNullException(nameof(shaderSource));

            if (!shaderFileName.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Shader file name must end with '.shader'.", nameof(shaderFileName));

            SetAction(() =>
            {
                Debug.Log($"Creating shader at path: {AssetPath}");
                File.WriteAllText(AssetPath, _shaderSource);
                AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport);
                Asset = AssetDatabase.LoadAssetAtPath<Shader>(AssetPath);
            });
        }
    }
}
