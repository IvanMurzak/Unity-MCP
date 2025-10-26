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
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Common;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public class TomlClientConfig : ClientConfig
    {
        public TomlClientConfig(string name, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
            : base(name, configPath, bodyPath)
        {
        }

        public override bool Configure()
        {
            return ConfigureTomlMcpClient(ConfigPath, BodyPath);
        }

        public static bool ConfigureTomlMcpClient(string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            Debug.Log($"{Consts.Log.Tag} Configuring MCP client with path: {configPath} and bodyPath: {bodyPath}");

            try
            {
                if (!File.Exists(configPath))
                {
                    // Create all necessary directories
                    Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                    // Create the file if it doesn't exist
                    File.WriteAllText(
                        path: configPath,
                        contents: Startup.Server.RawJsonConfiguration(UnityMcpPlugin.Port, bodyPath, UnityMcpPlugin.TimeoutMs).ToString());
                    return true;
                }

                var json = File.ReadAllText(configPath);
                JsonObject? rootObj = null;

                try
                {
                    // Parse the existing config as JsonObject
                    rootObj = JsonNode.Parse(json)?.AsObject();
                    if (rootObj == null)
                        throw new Exception("Config file is not a valid JSON object.");
                }
                catch
                {
                    File.WriteAllText(
                        path: configPath,
                        contents: Startup.Server.RawJsonConfiguration(UnityMcpPlugin.Port, bodyPath, UnityMcpPlugin.TimeoutMs).ToString());
                    return true;
                }

                // Get path segments and navigate to the injection target
                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Generate the configuration to inject
                var injectObj = Startup.Server.RawJsonConfiguration(UnityMcpPlugin.Port, pathSegments.Last(), UnityMcpPlugin.TimeoutMs);
                if (injectObj == null)
                    throw new Exception("Injected config is not a valid JSON object.");

                var injectMcpServers = injectObj[pathSegments.Last()]?.AsObject();
                if (injectMcpServers == null)
                    throw new Exception($"Missing '{pathSegments.Last()}' object in inject config.");

                // Navigate to or create the target location in the existing JSON
                var targetObj = McpClientUtils.EnsureJsonPathExists(rootObj, pathSegments);

                // Find all command values in injectMcpServers for duplicate removal
                var injectCommands = injectMcpServers
                    .Select(kv => kv.Value?["command"]?.GetValue<string>())
                    .Where(cmd => !string.IsNullOrEmpty(cmd))
                    .ToHashSet();

                // Remove any entry in targetObj with a matching command
                var keysToRemove = targetObj
                    .Where(kv => injectCommands.Contains(kv.Value?["command"]?.GetValue<string>()))
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                    targetObj.Remove(key);

                // Merge/overwrite entries from injectMcpServers
                foreach (var kv in injectMcpServers)
                {
                    // Clone the value to avoid parent conflict
                    targetObj[kv.Key] = kv.Value?.ToJsonString() is string jsonStr
                        ? JsonNode.Parse(jsonStr)
                        : null;
                }

                // Write back to file
                File.WriteAllText(configPath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                return McpClientUtils.IsMcpClientConfigured(configPath, bodyPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }
    }
}