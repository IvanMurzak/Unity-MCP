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
using System.ComponentModel;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsMaterialCreateToolId = "assets-material-create";
        [McpPluginTool
        (
            AssetsMaterialCreateToolId,
            Title = "Assets / Create Material"
        )]
        [Description("Create new material asset with default parameters. " +
            "Creates folders recursively if they do not exist. " +
            "Provide proper 'shaderName' - use '" + Tool_Assets_Shader.AssetsShaderListAllToolId + "' tool to find available shaders.")]
        public AssetObjectRef CreateMaterial
        (
            [Description("Asset path. Starts with 'Assets/'. Ends with '.mat'.")]
            string assetPath,
            [Description("Name of the shader that need to be used to create the material.")]
            string shaderName
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(assetPath))
                    throw new ArgumentException(Error.EmptyAssetPath(), nameof(assetPath));

                if (!assetPath.StartsWith("Assets/"))
                    throw new ArgumentException(Error.AssetPathMustStartWithAssets(assetPath), nameof(assetPath));

                if (!assetPath.EndsWith(".mat"))
                    throw new ArgumentException(Error.AssetPathMustEndWithMat(assetPath), nameof(assetPath));

                var shader = UnityEngine.Shader.Find(shaderName);
                if (shader == null)
                    throw new ArgumentException(Error.ShaderNotFound(shaderName), nameof(shaderName));

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

                 EditorUtils.RepaintAllEditorWindows();

                return new AssetObjectRef(material);
            });
        }
    }
}
