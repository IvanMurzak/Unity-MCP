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
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        public static string ResourcesFileName => "Unity-MCP-ConnectionConfig";
        public static string AssetsFilePath => $"Assets/Resources/{ResourcesFileName}.json";
#if UNITY_EDITOR
        public static TextAsset AssetFile => UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(AssetsFilePath);
        public static void InvalidateAssetFile() => UnityEditor.AssetDatabase.ImportAsset(AssetsFilePath, UnityEditor.ImportAssetOptions.ForceUpdate);
#endif

        UnityConnectionConfig GetOrCreateConfig() => GetOrCreateConfig(out _);
        UnityConnectionConfig GetOrCreateConfig(out bool wasCreated)
        {
            wasCreated = false;
            try
            {
#if UNITY_EDITOR
                var json = Application.isPlaying
                    ? Resources.Load<TextAsset>(ResourcesFileName).text
                    : File.Exists(AssetsFilePath)
                        ? File.ReadAllText(AssetsFilePath)
                        : null;
#else
                var json = Resources.Load<TextAsset>(ResourcesFileName).text;
#endif
                UnityConnectionConfig? config = null;
                try
                {
                    config = string.IsNullOrWhiteSpace(json)
                        ? null
                        : JsonSerializer.Deserialize<UnityConnectionConfig>(json);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "{tag} {class}.{method}: <color=red><b>{file}</b> file is corrupted at <i>{path}</i></color>",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(UnityMcpPlugin), ResourcesFileName, AssetsFilePath);
                }
                if (config == null)
                {
                    _logger.LogWarning("{tag} {class}.{method}: <color=orange><b>Creating {file}</b> file at <i>{path}</i></color>",
                        Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(UnityMcpPlugin), ResourcesFileName, AssetsFilePath);

                    config = new UnityConnectionConfig();
                    wasCreated = true;
                }

                return config;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{tag} {class}.{method}: <color=red><b>{file}</b> file can't be loaded from <i>{path}</i></color>",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(UnityMcpPlugin), ResourcesFileName, AssetsFilePath);
                throw e;
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            Validate();
            try
            {
                var directory = Path.GetDirectoryName(AssetsFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                unityConnectionConfig ??= new UnityConnectionConfig();
                var json = JsonSerializer.Serialize(unityConnectionConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AssetsFilePath, json);

                var assetFile = AssetFile;
                if (assetFile != null)
                    UnityEditor.EditorUtility.SetDirty(assetFile);
                else
                    UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{tag} {class}.{method}: <color=red><b>{file}</b> file can't be saved at <i>{path}</i></color>",
                    Consts.Log.Tag, nameof(UnityMcpPlugin), nameof(Save), ResourcesFileName, AssetsFilePath);
            }
#else
            // do nothing in runtime builds
            return;
#endif
        }
    }
}
