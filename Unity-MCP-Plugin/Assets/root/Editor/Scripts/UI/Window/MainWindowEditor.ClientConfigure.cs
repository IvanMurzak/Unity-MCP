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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
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
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{ProjectRootPath.Replace("/", "\\")}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "VS Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
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

            ConfigureClientsFromArray(root, clientConfigs);
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            var clientConfigs = new ClientConfig[]
            {
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
                    name: "Claude Code",
                    configPath: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".claude.json"
                    ),
                    bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                        + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                        + Consts.MCP.Server.DefaultBodyPath
                ),
                new JsonClientConfig(
                    name: "VS Code (Copilot)",
                    configPath: Path.Combine(
                        ".vscode",
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

            ConfigureClientsFromArray(root, clientConfigs);
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

            statusText.text = isConfiguredResult ? "Configured" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = config.Configure();

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
    }
}