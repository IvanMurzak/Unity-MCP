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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Extensions.Unity.PlayerPrefsEx;
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

        // PlayerPrefs key for storing selected MCP client
        private const string PlayerPrefsKey_SelectedMcpClient = "Unity_MCP_SelectedMcpClient";
        private static PlayerPrefsString _selectedMcpClientPref = new(PlayerPrefsKey_SelectedMcpClient);

        string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        void ConfigureClientsWindows(VisualElement root)
        {
            var clientConfigs = new ClientConfig[]
            {
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
                    name: "Antigravity",
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

            ConfigureClientsWithDropdown(root, clientConfigs);
        }

        void ConfigureClientsMacAndLinux(VisualElement root)
        {
            var clientConfigs = new ClientConfig[]
            {
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
                    name: "Antigravity",
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

            ConfigureClientsWithDropdown(root, clientConfigs);
        }

        void ConfigureClientsWithDropdown(VisualElement root, ClientConfig[] clientConfigs)
        {
            // Get the dropdown element
            var dropdown = root.Query<DropdownField>("mcpClientDropdown").First();
            if (dropdown == null)
            {
                Debug.LogError("mcpClientDropdown not found in UXML. Please ensure the dropdown element exists.");
                return;
            }

            // Get the container where client panels will be added
            var container = root.Query<VisualElement>("ConfigureClientsContainer").First();
            if (container == null)
            {
                Debug.LogError("ConfigureClientsContainer not found in UXML. Please ensure the container element exists.");
                return;
            }

            // Get client names from registry
            var clientNames = McpClientConfiguratorRegistry.GetClientNames();
            dropdown.choices = clientNames;

            // Load saved selection from PlayerPrefs
            var savedClientId = _selectedMcpClientPref.Value;
            var selectedIndex = 0;

            if (!string.IsNullOrEmpty(savedClientId))
            {
                selectedIndex = McpClientConfiguratorRegistry.GetIndexByClientId(savedClientId);
                if (selectedIndex < 0) selectedIndex = 0;
            }

            // Set initial dropdown value without triggering callback
            if (clientNames.Count > 0)
            {
                dropdown.SetValueWithoutNotify(clientNames[selectedIndex]);
            }

            // Load initial UI for selected client
            LoadClientUI(container, clientConfigs, selectedIndex);

            // Register callback for dropdown changes
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var newIndex = clientNames.IndexOf(evt.newValue);
                if (newIndex < 0) return;

                // Save selection to PlayerPrefs
                var configurator = McpClientConfiguratorRegistry.All[newIndex];
                _selectedMcpClientPref.Value = configurator.ClientId;

                // Load UI for the newly selected client
                LoadClientUI(container, clientConfigs, newIndex);
            });
        }

        void LoadClientUI(VisualElement container, ClientConfig[] clientConfigs, int selectedIndex)
        {
            // Clear any existing content
            container.Clear();

            if (selectedIndex < 0 || selectedIndex >= McpClientConfiguratorRegistry.All.Count)
                return;

            var configurator = McpClientConfiguratorRegistry.All[selectedIndex];

            // Load client-specific configuration UI from the configurator (now includes config panel)
            var clientSpecificUI = configurator.CreateUI(container);
            if (clientSpecificUI == null)
                return;

            container.Add(clientSpecificUI);

            // Find matching ClientConfig for this configurator and configure the embedded panel
            var clientConfig = clientConfigs.FirstOrDefault(c => c.Name == configurator.ClientName);
            if (clientConfig != null)
            {
                ConfigureClientPanel(clientSpecificUI, clientConfig);
            }
        }

        static void ConfigureClientPanel(VisualElement root, ClientConfig config)
        {
            var statusCircle = root.Q<VisualElement>("configureStatusCircle");
            var statusText = root.Q<Label>("configureStatusText");
            var btnConfigure = root.Q<Button>("btnConfigure");

            if (statusCircle == null || statusText == null || btnConfigure == null)
            {
                Debug.LogWarning($"Config panel elements not found in client UI for {config.Name}.");
                return;
            }

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
