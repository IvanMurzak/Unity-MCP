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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsGetDataToolId = "assets-get-data";
        [McpPluginTool
        (
            AssetsGetDataToolId,
            Title = "Assets / Get Data"
        )]
        [Description("Get asset data from the asset file in the Unity project. " +
            "It includes all serializable fields and properties of the asset. " +
            "Use '" + AssetsFindToolId + "' tool to find asset before using this tool.")]
        public SerializedMember GetData(AssetObjectRef assetRef)
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(assetRef.AssetPath) && string.IsNullOrEmpty(assetRef.AssetGuid))
                    throw new System.Exception(Error.NeitherProvided_AssetPath_AssetGuid());

                if (string.IsNullOrEmpty(assetRef.AssetPath))
                    assetRef.AssetPath = AssetDatabase.GUIDToAssetPath(assetRef.AssetGuid);

                UnityEngine.Object? asset = null;

                // Built-in assets fallback
                if (!string.IsNullOrEmpty(assetRef.AssetPath) && assetRef.AssetPath!.StartsWith(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath))
                {
                    var all = AssetDatabase.LoadAllAssetsAtPath(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath);
                    var targetName = System.IO.Path.GetFileNameWithoutExtension(assetRef.AssetPath);
                    foreach (var obj in all)
                    {
                        if (obj != null && obj.name == targetName)
                        {
                            var ext = System.IO.Path.GetExtension(assetRef.AssetPath);
                            if (!string.IsNullOrEmpty(ext))
                            {
                                if (ext == ".mat" && !(obj is UnityEngine.Material)) continue;
                                if (ext == ".shader" && !(obj is UnityEngine.Shader)) continue;
                                if (ext == ".compute" && !(obj is UnityEngine.ComputeShader)) continue;
                                if (ext == ".anim" && !(obj is UnityEngine.AnimationClip)) continue;
                                if (ext == ".wav" && !(obj is UnityEngine.AudioClip)) continue;
                            }
                            asset = obj;
                            break;
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(assetRef.AssetGuid))
                        assetRef.AssetGuid = AssetDatabase.AssetPathToGUID(assetRef.AssetPath);

                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetRef.AssetPath);
                }

                if (asset == null)
                    throw new System.Exception(Error.NotFoundAsset(assetRef.AssetPath!, assetRef.AssetGuid ?? "N/A"));

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                return reflector.Serialize(
                    asset,
                    name: asset.name,
                    recursive: true,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>()
                );
            });
        }
    }
}