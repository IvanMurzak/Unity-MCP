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
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using Consts = Common.Consts;

    public partial class MainWindowEditor : EditorWindow
    {
        string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        void ConfigureClientsWindows(VisualElement root)
        {
            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude-Desktop").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Claude",
                    "claude_desktop_config.json"
                ),
                bodyPath: Consts.MCP.Server.DefaultBodyPath);

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude-Code").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".claude.json"
                ),
                bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                    + $"{ProjectRootPath.Replace("/", "\\")}{Consts.MCP.Server.BodyPathDelimiter}"
                    + Consts.MCP.Server.DefaultBodyPath);

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: Path.Combine(
                    ".vscode",
                    "mcp.json"
                ),
                bodyPath: "servers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ),
                bodyPath: Consts.MCP.Server.DefaultBodyPath);
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude-Desktop").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    "Claude",
                    "claude_desktop_config.json"
                ),
                bodyPath: Consts.MCP.Server.DefaultBodyPath);

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Claude-Code").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".claude.json"
                ),
                bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                    + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                    + Consts.MCP.Server.DefaultBodyPath);

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-VS-Code").First(),
                configPath: Path.Combine(
                    ".vscode",
                    "mcp.json"
                ),
                bodyPath: "servers");

            ConfigureClient(root.Query<VisualElement>("ConfigureClient-Cursor").First(),
                configPath: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor",
                    "mcp.json"
                ),
                bodyPath: Consts.MCP.Server.DefaultBodyPath);
        }

        void ConfigureClient(VisualElement root, string configPath, string bodyPath = Consts.MCP.Server.DefaultBodyPath)
        {
            var statusCircle = root.Query<VisualElement>("configureStatusCircle").First();
            var statusText = root.Query<Label>("configureStatusText").First();
            var btnConfigure = root.Query<Button>("btnConfigure").First();

            var isConfiguredResult = IsMcpClientConfigured(configPath, bodyPath);

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
                var configureResult = ConfigureMcpClient(configPath, bodyPath);

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

                var targetObjs = new List<JsonObject?>();

                var mainTarget = NavigateToJsonPath(rootObj, pathSegments);
                targetObjs.Add(mainTarget);

                var isClaudeCodeOnWindows = configPath.EndsWith(".claude.json") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isClaudeCodeOnWindows && pathSegments.Length == 3 && pathSegments[0] == "projects" && pathSegments[2] == Consts.MCP.Server.DefaultBodyPath)
                {
                    var project = pathSegments[1];
                    if (project.Length > 2 && char.IsLetter(project[0]) && project[1] == ':' && project[2] == '\\')
                    {
                        var altDrive = char.IsUpper(project[0]) ? char.ToLower(project[0]) : char.ToUpper(project[0]);
                        var altProject = altDrive + project.Substring(1);
                        var altSegments = new string[] { pathSegments[0], altProject, pathSegments[2] };
                        var altTarget = NavigateToJsonPath(rootObj, altSegments);
                        targetObjs.Add(altTarget);
                    }
                }

                foreach (var targetObj in targetObjs)
                {
                    if (targetObj == null)
                        continue;

                    foreach (var kv in targetObj)
                    {
                        var command = kv.Value?["command"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(command) || !IsCommandMatch(command!))
                            continue;

                        var args = kv.Value?["args"]?.AsArray();
                        if (DoArgumentsMatch(args))
                            return true;
                    }
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
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                JsonObject rootObj;

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    try
                    {
                        var parsed = JsonNode.Parse(json);
                        rootObj = parsed?.AsObject() ?? new JsonObject();
                    }
                    catch
                    {
                        rootObj = new JsonObject();
                    }
                }
                else
                {
                    rootObj = new JsonObject();
                }

                // Get path segments and navigate to the injection target
                var bodyPaths = new List<string> { bodyPath };

                var isClaudeCodeOnWindows = configPath.EndsWith(".claude.json") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isClaudeCodeOnWindows)
                {
                    var segments = Consts.MCP.Server.BodyPathSegments(bodyPath);
                    if (segments.Length == 3 && segments[0] == "projects" && segments[2] == Consts.MCP.Server.DefaultBodyPath)
                    {
                        var project = segments[1];
                        if (project.Length > 2 && char.IsLetter(project[0]) && project[1] == ':' && project[2] == '\\')
                        {
                            var altDrive = char.IsUpper(project[0]) ? char.ToLower(project[0]) : char.ToUpper(project[0]);
                            var altProject = altDrive + project.Substring(1);
                            var altBodyPath = string.Join(Consts.MCP.Server.BodyPathDelimiter, new[] { segments[0], altProject, segments[2] });
                            bodyPaths.Add(altBodyPath);
                        }
                    }
                }

                foreach (var bp in bodyPaths)
                {
                    var pathSegments = Consts.MCP.Server.BodyPathSegments(bp);

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