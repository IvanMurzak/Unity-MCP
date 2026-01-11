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
using System.Collections.Generic;
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using Consts = McpPlugin.Common.Consts;

    public partial class MainWindowEditor
    {
        // Template paths for both local development and UPM package environments

        public static readonly string[] ClientConfigPanelTemplatePath = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ClientConfigPanel.uxml");

        // Returns project root path (parent of Assets folder). Needed for MCP client configs to identify which Unity project they're running in.
        static string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        void ConfigureClientsWindows(VisualElement root)
        {
            ConfigureClientsFromArray(root, GetClientConfigsWindows());
        }

        /// <summary>
        /// Returns all client configurations for the current platform.
        /// </summary>
        public static ClientConfig[] GetAllClientConfigs()
        {
#if UNITY_EDITOR_WIN
            return GetClientConfigsWindows();
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            return GetClientConfigsMacAndLinux();
#else
            return GetClientConfigsWindows();
#endif
        }

        /// <summary>
        /// Returns a list of configured clients (where IsConfigured() returns true).
        /// </summary>
        public static List<ClientConfig> GetConfiguredClients()
        {
            var allConfigs = GetAllClientConfigs();
            var configured = new List<ClientConfig>();
            foreach (var config in allConfigs)
            {
                try
                {
                    if (config.IsConfigured())
                        configured.Add(config);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to check if client '{config.Name}' is configured: {ex.Message}");
                }
            }
            return configured;
        }

        private static ClientConfig[] GetClientConfigsWindows()
        {
            var projectRootPath = ProjectRootPath;
            return new ClientConfig[]
            {
                new JsonClientConfig(
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{projectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Claude Desktop",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Claude",
                        "claude_desktop_config.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Visual Studio Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new JsonClientConfig(
                    name: "Visual Studio (Copilot)",
                    configPath: Path.Combine(
                        ".vs",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new JsonClientConfig(
                    name: "Cursor",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cursor",
                        "mcp.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Gemini",
                    configPath: Path.Combine(
                        ".gemini",
                        "settings.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Antigravity (Gemini)",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".gemini",
                        "antigravity",
                        "mcp_config.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new TomlClientConfig(
                    name: "Codex",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".codex",
                        "config.toml"
                    ),
                    bodyPath: "mcp_servers"
                )
            };
        }

        private static ClientConfig[] GetClientConfigsMacAndLinux()
        {
            var projectRootPath = ProjectRootPath;
            return new ClientConfig[]
            {
                new JsonClientConfig(
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{projectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
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
                new JsonClientConfig(
                    name: "Visual Studio Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new JsonClientConfig(
                    name: "Visual Studio (Copilot)",
                    configPath: Path.Combine(
                        ".vs",
                        "mcp.json"
                    ),
                    bodyPath: "servers"
                ),
                new JsonClientConfig(
                    name: "Cursor",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cursor",
                        "mcp.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Gemini",
                    configPath: Path.Combine(
                        ".gemini",
                        "settings.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "Antigravity (Gemini)",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".gemini",
                        "antigravity",
                        "mcp_config.json"
                    ),
                    bodyPath: Consts.MCP.Server.DefaultBodyPath
                ),
                new TomlClientConfig(
                    name: "Codex",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".codex",
                        "config.toml"
                    ),
                    bodyPath: "mcp_servers"
                )
            };
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            ConfigureClientsFromArray(root, GetClientConfigsMacAndLinux());
        }

        static void ConfigureClientsFromArray(VisualElement root, ClientConfig[] clientConfigs)
        {
            // Get the container where client panels will be added
            var container = root.Query<VisualElement>("ConfigureClientsContainer").First();
            if (container == null)
            {
                Debug.LogError("ConfigureClientsContainer not found in UXML. Please ensure the container element exists.");
                return;
            }

            // Try to load the template from both possible paths (UPM package or local development)
            var clientConfigPanelTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(ClientConfigPanelTemplatePath);
            if (clientConfigPanelTemplate == null)
            {
                Debug.LogError("ClientConfigPanel template not found in specified paths. Please ensure the template file exists.");
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

        static void ConfigureClient(VisualElement root, ClientConfig config)
        {
            var statusCircle = root.Q<VisualElement>("configureStatusCircle") ?? throw new NullReferenceException("Status circle element not found in the template.");
            var statusText = root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Status text element not found in the template.");
            var btnConfigure = root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Configure button element not found in the template.");

            // Update the client name
            var clientNameLabel = root.Q<Label>("clientNameLabel") ?? throw new NullReferenceException("Client name label element not found in the template.");
            clientNameLabel.text = config.Name;

            var isConfiguredResult = config.IsConfigured();

            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
            statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

            statusCircle.AddToClassList(isConfiguredResult
                ? USS_IndicatorClass_Connected
                : USS_IndicatorClass_Disconnected);

            statusText.text = isConfiguredResult ? "Configured (stdio)" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = config.Configure();

                statusText.text = configureResult ? "Configured (stdio)" : "Not Configured";

                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
                statusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

                statusCircle.AddToClassList(configureResult
                    ? USS_IndicatorClass_Connected
                    : USS_IndicatorClass_Disconnected);

                btnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
        }
    }
}