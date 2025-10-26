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
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using Consts = Common.Consts;

    public partial class MainWindowEditor : EditorWindow
    {
        // Template paths for both local development and UPM package environments
        const string ClientConfigPanelTemplatePathPackage = "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/ClientConfigPanel.uxml";
        const string ClientConfigPanelTemplatePathLocal = "Assets/root/Editor/UI/uxml/ClientConfigPanel.uxml";

        string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        void ConfigureClientsWindows(VisualElement root)
        {
            var clientConfigs = new ClientConfig[]
            {
                new ClientConfig(
                    name: "Claude Desktop",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Claude",
                        "claude_desktop_config.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new ClientConfig(
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{ProjectRootPath.Replace("/", "\\")}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new ClientConfig(
                    name: "VS Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new ClientConfig(
                    name: "Cursor",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cursor",
                        "mcp.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                )
            };

            ConfigureClientsFromArray(root, clientConfigs);
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            var clientConfigs = new ClientConfig[]
            {
                new ClientConfig(
                    name: "Claude Desktop",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library",
                        "Application Support",
                        "Claude",
                        "claude_desktop_config.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new ClientConfig(
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new ClientConfig(
                    name: "VS Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new ClientConfig(
                    name: "Cursor",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cursor",
                        "mcp.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                )
            };

            ConfigureClientsFromArray(root, clientConfigs);
        }

        void ConfigureClientsFromArray(VisualElement root, ClientConfig[] clientConfigs)
        {
            // Get the container where client panels will be added
            var container = root.Query<VisualElement>("ConfigureClientsContainer").First();

            if (container == null)
            {
                Debug.LogError("ConfigureClientsContainer not found in UXML. Please ensure the container element exists.");
                return;
            }

            // Try to load the template from both possible paths (UPM package or local development)
            var clientConfigPanelTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ClientConfigPanelTemplatePathPackage);

            if (clientConfigPanelTemplate == null)
            {
                // Fallback to local development path
                clientConfigPanelTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ClientConfigPanelTemplatePathLocal);
            }

            if (clientConfigPanelTemplate == null)
            {
                Debug.LogError($"Failed to load ClientConfigPanel template from either path:\n" +
                              $"- Package: {ClientConfigPanelTemplatePathPackage}\n" +
                              $"- Local: {ClientConfigPanelTemplatePathLocal}");
                return;
            }

            // Clear any existing dynamic panels
            container.Clear();

            // Clone and configure a panel for each client
            foreach (var config in clientConfigs)
            {
                // Clone the template using Unity's built-in method
                var panel = clientConfigPanelTemplate.CloneTree();

                // Configure the panel with the client's configuration
                ConfigureClient(panel, config);

                // Add the configured panel to the container
                container.Add(panel);
            }
        }

        void ConfigureClient(VisualElement root, ClientConfig config)
        {
            var statusCircle = root.Q<VisualElement>("configureStatusCircle");
            var statusText = root.Q<Label>("configureStatusText");
            var btnConfigure = root.Q<Button>("btnConfigure");

            // Update the client name
            var clientNameLabel = root.Q<Label>("clientNameLabel");
            clientNameLabel.text = config.Name;

            var isConfiguredResult = IsMcpClientConfigured(config.ConfigPath, config.BodyPath);

            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

            statusCircle.AddToClassList(isConfiguredResult
                ? USS_IndicatorClass_Connected
                : USS_IndicatorClass_Disconnected);

            statusText.text = isConfiguredResult ? "Configured" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = ConfigureMcpClient(config.ConfigPath, config.BodyPath);

                statusText.text = configureResult ? "Configured" : "Not Configured";

                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

                statusCircle.AddToClassList(configureResult
                    ? USS_IndicatorClass_Connected
                    : USS_IndicatorClass_Disconnected);

                btnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
        }

        public bool IsMcpClientConfigured(string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                var json = File.ReadAllText(configPath);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var rootObj = JsonNode.Parse(json)?.AsObject();
                if (rootObj == null)
                    return false;

                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Navigate to the target location using bodyPath segments
                var targetObj = NavigateToJsonPath(rootObj, pathSegments);
                if (targetObj == null)
                    return false;

                foreach (var kv in targetObj)
                {
                    var command = kv.Value?["command"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(command) || !IsCommandMatch(command!))
                        continue;

                    var args = kv.Value?["args"]?.AsArray();
                    return DoArgumentsMatch(args);
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading config file: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        bool IsCommandMatch(string command)
        {
            // Normalize both paths for comparison
            try
            {
                var normalizedCommand = Path.GetFullPath(command.Replace('/', Path.DirectorySeparatorChar));
                var normalizedTarget = Path.GetFullPath(Startup.Server.ExecutableFullPath.Replace('/', Path.DirectorySeparatorChar));
                return string.Equals(normalizedCommand, normalizedTarget, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If normalization fails, fallback to string comparison
                return string.Equals(command, Startup.Server.ExecutableFullPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        bool DoArgumentsMatch(JsonArray? args)
        {
            if (args == null)
                return false;

            var targetPort = UnityMcpPlugin.Port.ToString();
            var targetTimeout = UnityMcpPlugin.TimeoutMs.ToString();

            var foundPort = false;
            var foundTimeout = false;

            // Check for both positional and named argument formats
            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i]?.GetValue<string>();
                if (string.IsNullOrEmpty(arg))
                    continue;

                // Check positional format
                if (i == 0 && arg == targetPort)
                    foundPort = true;
                else if (i == 1 && arg == targetTimeout)
                    foundTimeout = true;

                // Check named format
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.Port}=") && arg.Substring(Consts.MCP.Server.Args.Port.Length + 1) == targetPort)
                    foundPort = true;
                else if (arg!.StartsWith($"{Consts.MCP.Server.Args.PluginTimeout}=") && arg.Substring(Consts.MCP.Server.Args.PluginTimeout.Length + 1) == targetTimeout)
                    foundTimeout = true;
            }

            return foundPort && foundTimeout;
        }

        JsonObject? NavigateToJsonPath(JsonObject rootObj, string[] pathSegments)
        {
            JsonObject? current = rootObj;

            foreach (var segment in pathSegments)
            {
                if (current == null)
                    return null;

                current = current[segment]?.AsObject();
            }

            return current;
        }

        JsonObject EnsureJsonPathExists(JsonObject rootObj, string[] pathSegments)
        {
            JsonObject current = rootObj;

            foreach (var segment in pathSegments)
            {
                if (current[segment]?.AsObject() is JsonObject existingObj)
                {
                    current = existingObj;
                }
                else
                {
                    var newObj = new JsonObject();
                    current[segment] = newObj;
                    current = newObj;
                }
            }

            return current;
        }

        public bool ConfigureMcpClient(string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
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
                var targetObj = EnsureJsonPathExists(rootObj, pathSegments);

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

                return IsMcpClientConfigured(configPath, bodyPath);
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