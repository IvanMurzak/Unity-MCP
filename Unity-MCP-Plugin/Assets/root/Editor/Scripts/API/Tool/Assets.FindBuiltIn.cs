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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsFindBuiltInToolId = "assets-find-built-in";
        [McpPluginTool
        (
            AssetsFindBuiltInToolId,
            Title = "Assets / Find (Built-in)"
        )]
        [Description("Search the built-in assets of the Unity Editor located in the built-in resources: " +
            ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath + ". " +
            "Doesn't support GUIDs since built-in assets do not have them.")]
        public List<AssetObjectRef> FindBuiltIn
        (
            [Description("The name of the asset to filter by.")]
            string? name = null,
            [Description("The type of the asset to filter by.")]
            System.Type? type = null,
            [Description("Maximum number of assets to return. If the number of found assets exceeds this limit, the result will be truncated.")]
            int maxResults = 10
        )
        {
            if (maxResults <= 0)
                throw new System.ArgumentException($"{nameof(maxResults)} must be greater than zero.");

            return MainThread.Instance.Run(() =>
            {
                var response = new List<AssetObjectRef>();

                var all = AssetDatabase.LoadAllAssetsAtPath(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath);
                foreach (var obj in all)
                {
                    if (response.Count >= maxResults)
                        break;

                    if (obj == null)
                        continue;

                    if (string.IsNullOrEmpty(obj.name))
                        continue;

                    if (type != null && obj.GetType() != type && !obj.GetType().IsSubclassOf(type))
                        continue;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var words = name!.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        var match = false;
                        foreach (var word in words)
                        {
                            if (obj.name.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                match = true;
                                break;
                            }
                        }

                        if (!match)
                            continue;
                    }

                    var assetObjRef = new AssetObjectRef(obj);

                    // Unity built-in assets do not have GUIDs
                    assetObjRef.AssetGuid = null;

                    // Distinguish built-in assets: if the path doesn't end with the asset name, append it.
                    // This handles cases where multiple built-in assets map to "Resources/unity_builtin_extra".
                    if (!string.IsNullOrEmpty(assetObjRef.AssetPath) && !assetObjRef.AssetPath!.EndsWith("/" + obj.name))
                    {
                        var extension = obj switch
                        {
                            UnityEngine.Material => ".mat",
                            UnityEngine.Shader => ".shader",
                            UnityEngine.ComputeShader => ".compute",
                            UnityEngine.AnimationClip => ".anim",
                            UnityEngine.AudioClip => ".wav",
                            _ => string.Empty
                        };

                        assetObjRef.AssetPath = $"{assetObjRef.AssetPath}/{obj.name}{extension}";
                    }

                    response.Add(assetObjRef);
                }

                return response;
            });
        }
    }
}