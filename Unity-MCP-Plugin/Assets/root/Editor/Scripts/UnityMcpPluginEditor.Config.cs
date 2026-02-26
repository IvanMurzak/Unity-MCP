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
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPluginEditor
    {
        public static string ResourcesFileName => "AI-Game-Developer-Config";
        public static string AssetsFilePath => $"UserSettings/{ResourcesFileName}.json";
#if UNITY_EDITOR
        public static UnityEngine.TextAsset AssetFile => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(AssetsFilePath);
#endif

        UnityConnectionConfig GetOrCreateConfig() => GetOrCreateConfig(out _);
        UnityConnectionConfig GetOrCreateConfig(out bool wasCreated)
        {
            wasCreated = false;
            try
            {
                // Both Edit mode and Play mode read from the same UserSettings JSON file
                var json = File.Exists(AssetsFilePath) ? File.ReadAllText(AssetsFilePath) : null;

                UnityConnectionConfig? config = null;
                try
                {
                    config = string.IsNullOrWhiteSpace(json)
                        ? null
                        : JsonSerializer.Deserialize<UnityConnectionConfig>(json!, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file is corrupted at <i>{path}</i></color>",
                        nameof(GetOrCreateConfig), ResourcesFileName, AssetsFilePath);
                }
                if (config == null)
                {
                    _logger.LogWarning("{method}: <color=orange><b>Creating {file}</b> file at <i>{path}</i></color>",
                        nameof(GetOrCreateConfig), ResourcesFileName, AssetsFilePath);
                    config = new UnityConnectionConfig();
                    wasCreated = true;
                }
                if (string.IsNullOrEmpty(config.Token))
                {
                    config.Token = GenerateToken();
                    wasCreated = true;
                }

                return config;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file can't be loaded from <i>{path}</i></color>",
                    nameof(GetOrCreateConfig), ResourcesFileName, AssetsFilePath);
                throw;
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
                    Directory.CreateDirectory(directory!);

                unityConnectionConfig ??= new UnityConnectionConfig();

                var enabledToolNames = Tools?.GetAllTools()
                    ?.Select(t => new UnityConnectionConfig.McpFeature(t.Name, Tools.IsToolEnabled(t.Name)))
                    ?.ToList();

                var enabledPromptNames = Prompts?.GetAllPrompts()
                    ?.Select(p => new UnityConnectionConfig.McpFeature(p.Name, Prompts.IsPromptEnabled(p.Name)))
                    ?.ToList();

                var enabledResourceNames = Resources?.GetAllResources()
                    ?.Select(r => new UnityConnectionConfig.McpFeature(r.Name, Resources.IsResourceEnabled(r.Name)))
                    ?.ToList();

                unityConnectionConfig.Tools = enabledToolNames != null && enabledToolNames.Count > 0
                    ? enabledToolNames
                    : UnityConnectionConfig.DefaultTools;

                unityConnectionConfig.Prompts = enabledPromptNames != null && enabledPromptNames.Count > 0
                    ? enabledPromptNames
                    : UnityConnectionConfig.DefaultPrompts;

                unityConnectionConfig.Resources = enabledResourceNames != null && enabledResourceNames.Count > 0
                    ? enabledResourceNames
                    : UnityConnectionConfig.DefaultResources;

                var json = JsonSerializer.Serialize(unityConnectionConfig, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(AssetsFilePath, json);

                var assetFile = AssetFile;
                if (assetFile != null)
                    UnityEditor.EditorUtility.SetDirty(assetFile);
                else
                    UnityEditor.AssetDatabase.ImportAsset(AssetsFilePath);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file can't be saved at <i>{path}</i></color>",
                    nameof(Save), ResourcesFileName, AssetsFilePath);
            }
#else
            // do nothing in runtime builds
            return;
#endif
        }
    }
}
