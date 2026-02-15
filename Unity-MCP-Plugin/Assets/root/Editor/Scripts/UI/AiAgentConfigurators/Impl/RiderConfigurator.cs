/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Yokesh J (https://github.com/Yokesh-4040)               │
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
using System.Reflection;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using R3;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Rider AI agent (Junie).
    /// Supports both Global and Local (Project-level) configurations.
    /// </summary>
    public class RiderConfigurator : AiAgentConfigurator
    {
        #region Constants & Enums

        private enum ConfigurationScope { Global, Local }

        private const string AGENT_DISPLAY_NAME = "Rider (Junie)";
        private const string AGENT_ID = "rider-junie";
        private const string DOWNLOAD_URL = "https://www.jetbrains.com/rider/download/";
        private const string ICON_NAME = "rider-64.png";

        private static readonly Color TOGGLE_BG_COLOR = new(0.15f, 0.15f, 0.15f, 1.0f);
        private static readonly Color TOGGLE_ACTIVE_COLOR = new(0.32f, 0.32f, 0.32f, 1.0f);
        private static readonly Color TOGGLE_INACTIVE_TEXT_COLOR = new(0.55f, 0.55f, 0.55f, 1.0f);

        #endregion

        #region Properties

        public override string AgentName => AGENT_DISPLAY_NAME;
        public override string AgentId => AGENT_ID;
        public override string DownloadUrl => DOWNLOAD_URL;
        protected override string? IconFileName => ICON_NAME;

        private ConfigurationScope Scope
        {
            get => (ConfigurationScope)EditorPrefs.GetInt($"{AgentId}_scope", (int)ConfigurationScope.Global);
            set => EditorPrefs.SetInt($"{AgentId}_scope", (int)value);
        }

        private string JunieConfigPath => GetJunieConfigPath(Scope);

        private IDisposable? _riderSubStdio;
        private IDisposable? _riderSubHttp;

        #endregion

        #region Configuration Creation

        protected override AiAgentConfig CreateConfigStdioWindows() => CreateJunieConfig(TransportMethod.stdio);
        protected override AiAgentConfig CreateConfigStdioMacLinux() => CreateJunieConfig(TransportMethod.stdio);
        protected override AiAgentConfig CreateConfigHttpWindows() => CreateJunieConfig(TransportMethod.streamableHttp);
        protected override AiAgentConfig CreateConfigHttpMacLinux() => CreateJunieConfig(TransportMethod.streamableHttp);

        private string GetJunieConfigPath(ConfigurationScope scope)
        {
            var relativePath = Path.Combine(".junie", "mcp", "mcp.json");
            return scope == ConfigurationScope.Global
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relativePath)
                : Path.Combine(ProjectRootPath, relativePath);
        }

        private AiAgentConfig CreateJunieConfig(TransportMethod transportMethod, ConfigurationScope? scopeOverride = null)
        {
            var targetScope = scopeOverride ?? Scope;
            var config = new JsonAiAgentConfig(
                name: "Unity Project",
                configPath: GetJunieConfigPath(targetScope),
                bodyPath: Consts.MCP.Server.DefaultBodyPath
            )
            .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
            .SetPropertyToRemove("disabled");

            if (transportMethod == TransportMethod.stdio)
            {
                config.SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
                      .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
                      .SetProperty("args", new JsonArray {
                          $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                          $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
                      }, requiredForConfiguration: true)
                      .SetPropertyToRemove("url");
            }  
            else
            {
                config.SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
                      .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
                      .SetPropertyToRemove("command")
                      .SetPropertyToRemove("args");
            }

            return config;
        }

        #endregion

        #region UI Lifecycle

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (Root == null) return;

            UpdateScopeToggle();
            UpdateHeaderInstructions();
            UpdateStdioContent();
            UpdateHttpContent();

            // Notify base class to refresh the status indicators since the configuration targets might have changed
            SetConfigureStatusIndicator();
        }

        #endregion

        #region UI Components

        private void UpdateScopeToggle()
        {
            var headerRow = Root!.Q<VisualElement>("agentIcon")?.parent;
            if (headerRow == null) return;

            var toggleId = $"{AgentId}-scope-toggle";
            var existing = headerRow.Q(toggleId);
            if (existing != null) headerRow.Remove(existing);

            var scopeContainer = new VisualElement { name = toggleId };
            scopeContainer.style.flexDirection = FlexDirection.Row;
            scopeContainer.style.alignItems = Align.Center;
            scopeContainer.style.backgroundColor = new StyleColor(TOGGLE_BG_COLOR);
            scopeContainer.style.borderTopLeftRadius = scopeContainer.style.borderTopRightRadius =
            scopeContainer.style.borderBottomLeftRadius = scopeContainer.style.borderBottomRightRadius = 5;
            scopeContainer.style.paddingLeft = scopeContainer.style.paddingRight =
            scopeContainer.style.paddingTop = scopeContainer.style.paddingBottom = 2;

            var btnGlobal = new Button(() => SwitchScope(ConfigurationScope.Global)) { text = "global" };
            var btnLocal = new Button(() => SwitchScope(ConfigurationScope.Local)) { text = "local" };

            ApplyScopeButtonStyle(btnGlobal, Scope == ConfigurationScope.Global);
            ApplyScopeButtonStyle(btnLocal, Scope == ConfigurationScope.Local);

            scopeContainer.Add(btnGlobal);
            scopeContainer.Add(btnLocal);
            headerRow.Add(scopeContainer);
        }

        private void ApplyScopeButtonStyle(Button btn, bool active)
        {
            var s = btn.style;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 0;
            s.marginLeft = s.marginRight = s.marginTop = s.marginBottom = 0;
            s.paddingLeft = s.paddingRight = 10;
            s.paddingTop = s.paddingBottom = 2;
            s.fontSize = 11;
            s.backgroundColor = active ? new StyleColor(TOGGLE_ACTIVE_COLOR) : new StyleColor(Color.clear);
            s.color = active ? Color.white : new StyleColor(TOGGLE_INACTIVE_TEXT_COLOR);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 4;
        }

        private void UpdateHeaderInstructions()
        {
            ContainerUnderHeader!.Clear();
            if (Scope == ConfigurationScope.Local)
            {
                var projectWarning = TemplateWarningLabel("Local-level config: after configuring, go to Rider Settings | Tools | Junie | MCP Settings and check 'ai-game-developer' to connect AI agent.");
                projectWarning.style.marginBottom = 10;
                ContainerUnderHeader.Add(projectWarning);
            }
        }

        private void UpdateStdioContent()
        {
            ContainerStdio!.Clear();

            // Manual Steps
            var manualSteps = TemplateFoldoutFirst("Manual Configuration Steps");
            var relativePath = Scope == ConfigurationScope.Global
                ? Path.Combine("~", ".junie", "mcp", "mcp.json")
                : Path.Combine(".junie", "mcp", "mcp.json");

            // Option 1: Terminal
            var terminalCmd = $"mkdir -p .junie/mcp && printf '{ConfigStdio.ExpectedFileContent.Replace("'", "'\\''")}' > {(Scope == ConfigurationScope.Global ? JunieConfigPath : relativePath)}";
            manualSteps.Add(TemplateLabelDescription("Option 1: Use Terminal (Recommended for CLI lovers)"));
            manualSteps.Add(TemplateLabelDescription("Run this command in your project root terminal:"));
            manualSteps.Add(TemplateTextFieldReadOnly(terminalCmd));

            // Option 2: Manual File
            manualSteps.Add(TemplateLabelDescription("Option 2: Manual File Creation"));
            manualSteps.Add(TemplateLabelDescription($"1. Create or open the file: {relativePath}"));
            manualSteps.Add(TemplateLabelDescription("2. Copy and paste the JSON below:"));
            manualSteps.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            // Option 3: Rider UI
            manualSteps.Add(TemplateLabelDescription("Option 3: Rider Settings"));
            manualSteps.Add(TemplateLabelDescription("Open Rider settings: Settings | Tools | Junie | MCP Settings and add a new server."));

            ContainerStdio.Add(manualSteps);

            // Troubleshooting
            var troubleshooting = TemplateFoldout("Troubleshooting");
            troubleshooting.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshooting.Add(TemplateLabelDescription("- Restart Rider after configuration changes"));
            troubleshooting.Add(TemplateLabelDescription("- If using Terminal, ensure you are in the Unity project root folder."));
            ContainerStdio.Add(troubleshooting);
        }

        private void UpdateHttpContent()
        {
            ContainerHttp!.Clear();

            var warning = TemplateWarningLabel("Apologies for inconvenience. Please use Stdio to connect. Currently in Rider only Junie will be able to connect to Unity MCP, via Stdio.");
            warning.style.color = new StyleColor(Color.yellow);
            warning.style.whiteSpace = WhiteSpace.Normal;
            warning.style.marginBottom = 10;

            ContainerHttp.Add(warning);
            ContainerHttp.Add(TemplateLabelDescription("The standard HTTP configuration is disabled due to stability issues."));
            ContainerHttp.Add(TemplateLabelDescription("Please switch to the 'Stdio' transport method at the top to configure this agent."));
        }

        protected override AiAgentConfigurator SetConfigureStatusIndicator()
        {
            base.SetConfigureStatusIndicator();

            // Access private fields from base class via reflection to avoid modifying core code
            var type = typeof(AiAgentConfigurator);
            var stdioElementField = type.GetField("_configElementStdio", BindingFlags.NonPublic | BindingFlags.Instance);
            var httpElementField = type.GetField("_configElementHttp", BindingFlags.NonPublic | BindingFlags.Instance);

            var configElementStdio = stdioElementField?.GetValue(this) as ConfigurationElements;
            var configElementHttp = httpElementField?.GetValue(this) as ConfigurationElements;

            // Subscribe to configuration events to remove config from the other scope when one is successfully configured
            if (configElementStdio != null)
                _riderSubStdio = configElementStdio.OnConfigured.Subscribe(success => { if (success) RemoveFromOtherScope(); });

            if (configElementHttp != null)
                _riderSubHttp = configElementHttp.OnConfigured.Subscribe(success => { if (success) RemoveFromOtherScope(); });

            return this;
        }

        protected override void DisposeConfigurationElements()
        {
            base.DisposeConfigurationElements();
            _riderSubStdio?.Dispose();
            _riderSubHttp?.Dispose();
            _riderSubStdio = null;
            _riderSubHttp = null;
        }

        #endregion

        #region Scope Management

        private void SwitchScope(ConfigurationScope newScope)
        {
            if (Scope == newScope) return;

            Scope = newScope;
            ResetConfigs();
            RefreshUI();
        }

        /// <summary>
        /// Resets the cached configuration objects in the base class via reflection.
        /// This avoids modifying the core AiAgentConfigurator.cs.
        /// </summary>
        private void ResetConfigs()
        {
            var type = typeof(AiAgentConfigurator);
            var stdioField = type.GetField("_configStdio", BindingFlags.NonPublic | BindingFlags.Instance);
            var httpField = type.GetField("_configHttp", BindingFlags.NonPublic | BindingFlags.Instance);
            
            stdioField?.SetValue(this, null);
            httpField?.SetValue(this, null);
        }

        private void RemoveFromOtherScope()
        {
            var otherScope = Scope == ConfigurationScope.Global ? ConfigurationScope.Local : ConfigurationScope.Global;
            var otherConfig = CreateJunieConfig(UnityMcpPlugin.TransportMethod, otherScope);
            RemoveMcpServerLocally(otherConfig);
        }

        private void RemoveMcpServerLocally(AiAgentConfig config)
        {
            if (string.IsNullOrEmpty(config.ConfigPath) || !File.Exists(config.ConfigPath)) return;

            try
            {
                var json = File.ReadAllText(config.ConfigPath);
                var rootNode = JsonNode.Parse(json);
                if (rootNode is not JsonObject rootObj) return;

                var pathSegments = Consts.MCP.Server.BodyPathSegments(config.BodyPath);

                // Navigate to the target location
                JsonObject? current = rootObj;
                foreach (var segment in pathSegments)
                {
                    current = current?[segment]?.AsObject();
                    if (current == null) return;
                }

                if (current.Remove(AiAgentConfig.DefaultMcpServerName))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(config.ConfigPath, rootObj.ToJsonString(options));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{AGENT_ID}] Failed to remove server from {config.ConfigPath}: {ex.Message}");
            }
        }

        #endregion
    }
}
