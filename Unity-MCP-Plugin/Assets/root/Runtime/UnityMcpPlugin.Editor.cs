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
    public partial class UnityMcpPlugin
    {
        public static string ConfigFileName => "AI-Game-Developer-Config.json";
        public static string ConfigDirectoryPath => "ProjectSettings/Packages/com.ivanmurzak.unity.mcp";
        public static string ConfigFilePath => $"{ConfigDirectoryPath}/{ConfigFileName}";

        /// <summary>Legacy config path for migration from older versions.</summary>
        public static string LegacyAssetsFilePath => "Assets/Resources/AI-Game-Developer-Config.json";

        UnityConnectionConfig GetOrCreateConfig() => GetOrCreateConfig(out _);
        UnityConnectionConfig GetOrCreateConfig(out bool wasCreated)
        {
            wasCreated = false;
            try
            {
#if UNITY_EDITOR
                string? json = null;

                // Try new location first
                if (File.Exists(ConfigFilePath))
                {
                    json = File.ReadAllText(ConfigFilePath);
                }
                // Try legacy location (Assets/Resources/) and migrate
                else if (File.Exists(LegacyAssetsFilePath))
                {
                    json = File.ReadAllText(LegacyAssetsFilePath);
                    _logger.LogInformation("{method}: Migrating config from <i>{old}</i> to <i>{new}</i>",
                        nameof(GetOrCreateConfig), LegacyAssetsFilePath, ConfigFilePath);
                }
#else
                // In player builds, config is provided via Builder API or defaults are used.
                // No config file is available in runtime builds.
                wasCreated = true;
                return new UnityConnectionConfig();
#endif

#if UNITY_EDITOR
                UnityConnectionConfig? config = null;
                try
                {
                    config = string.IsNullOrWhiteSpace(json)
                        ? null
                        : JsonSerializer.Deserialize<UnityConnectionConfig>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file is corrupted at <i>{path}</i></color>",
                        nameof(GetOrCreateConfig), ConfigFileName, ConfigFilePath);
                }
                if (config == null)
                {
                    _logger.LogWarning("{method}: <color=orange><b>Creating {file}</b> file at <i>{path}</i></color>",
                        nameof(GetOrCreateConfig), ConfigFileName, ConfigFilePath);

                    config = new UnityConnectionConfig();
                    wasCreated = true;
                }

                return config;
#endif
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file can't be loaded from <i>{path}</i></color>",
                    nameof(GetOrCreateConfig), ConfigFileName, ConfigFilePath);
                throw;
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            Validate();
            try
            {
                if (!Directory.Exists(ConfigDirectoryPath))
                    Directory.CreateDirectory(ConfigDirectoryPath);

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
                File.WriteAllText(ConfigFilePath, json);

                // Clean up legacy config if it exists
                MigrateLegacyConfig();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "{method}: <color=red><b>{file}</b> file can't be saved at <i>{path}</i></color>",
                    nameof(Save), ConfigFileName, ConfigFilePath);
            }
#else
            // do nothing in runtime builds
            return;
#endif
        }

#if UNITY_EDITOR
        private void MigrateLegacyConfig()
        {
            if (File.Exists(LegacyAssetsFilePath))
            {
                try
                {
                    File.Delete(LegacyAssetsFilePath);
                    var metaPath = LegacyAssetsFilePath + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);

                    // Check if the Resources folder is now empty and clean it up
                    var resourcesDir = Path.GetDirectoryName(LegacyAssetsFilePath);
                    if (resourcesDir != null && Directory.Exists(resourcesDir)
                        && Directory.GetFiles(resourcesDir).Length == 0
                        && Directory.GetDirectories(resourcesDir).Length == 0)
                    {
                        Directory.Delete(resourcesDir);
                        var resourcesMetaPath = resourcesDir + ".meta";
                        if (File.Exists(resourcesMetaPath))
                            File.Delete(resourcesMetaPath);
                    }

                    UnityEditor.AssetDatabase.Refresh();
                    _logger.LogInformation("Migrated config from <i>{old}</i> to <i>{new}</i>",
                        LegacyAssetsFilePath, ConfigFilePath);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to clean up legacy config at {path}", LegacyAssetsFilePath);
                }
            }
        }
#endif
    }
}
