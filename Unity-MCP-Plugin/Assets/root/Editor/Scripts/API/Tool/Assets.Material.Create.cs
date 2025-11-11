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
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Material_Create",
            Title = "Create Material asset"
        )]
        [Description(@"Create new material asset with default parameters. Creates folders recursively if they do not exist. Provide proper 'shaderName', to find the shader, use 'Shader.Find' method.")]
        public string CreateMaterial
        (
            [Description("Asset path. Starts with 'Assets/'. Ends with '.mat'.")]
            string assetPath,
            [Description("Name of the shader that need to be used to create the material.")]
            string shaderName
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(assetPath))
                return Error.EmptyAssetPath();

            if (!assetPath.StartsWith("Assets/"))
                return Error.AssetPathMustStartWithAssets(assetPath);

            if (!assetPath.EndsWith(".mat"))
                return Error.AssetPathMustEndWithMat(assetPath);

            var shader = UnityEngine.Shader.Find(shaderName);
            if (shader == null)
                return Error.ShaderNotFound(shaderName);

            var material = new UnityEngine.Material(shader);

            // Create all folders in the path if they do not exist
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var result = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                material,
                name: material.name,
                logger: McpPlugin.McpPlugin.Instance.Logger
            );
            return $"[Success] Material instanceID '{material.GetInstanceID()}' created at '{assetPath}'.\n{result}";
        });
    }
}
