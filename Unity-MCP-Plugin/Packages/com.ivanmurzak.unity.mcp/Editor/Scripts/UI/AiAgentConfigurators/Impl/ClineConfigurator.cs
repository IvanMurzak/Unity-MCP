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
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Cline AI agent (VS Code extension).
    /// Uses global configuration file in VS Code's globalStorage.
    /// </summary>
    public class ClineConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Cline";
        public override string AgentId => "cline";
        public override string DownloadUrl => "https://cline.bot/";
        public override string? SkillsPath => Path.Combine(ProjectRootPath, ".cline", "skills"); // https://docs.cline.bot/customization/skills

        protected override string? IconFileName => "cline-64.png";

        private static string GlobalConfigPathWindows => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Code",
            "User",
            "globalStorage",
            "saoudrizwan.claude-dev",
            "settings",
            "cline_mcp_settings.json"
        );

        private static string GlobalConfigPathMac => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "Application Support",
            "Code",
            "User",
            "globalStorage",
            "saoudrizwan.claude-dev",
            "settings",
            "cline_mcp_settings.json"
        );

        private static string GlobalConfigPathLinux => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "Code",
            "User",
            "globalStorage",
            "saoudrizwan.claude-dev",
            "settings",
            "cline_mcp_settings.json"
        );

        private static string GlobalConfigPathMacLinux =>
#if UNITY_EDITOR_OSX
            GlobalConfigPathMac;
#else
            GlobalConfigPathLinux;
#endif

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: GlobalConfigPathWindows,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: GlobalConfigPathMacLinux,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: GlobalConfigPathWindows,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("streamableHttp"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: GlobalConfigPathMacLinux,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("streamableHttp"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var configPathStdio = ConfigStdio.ConfigPath;
            var configPathHttp = ConfigHttp.ConfigPath;

            ContainerUnderHeader!.Add(TemplateWarningLabel("IMPORTANT: Cline uses global configuration shared across all projects."));

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file:"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(configPathStdio));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Restart VS Code or reload the Cline extension."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure the configuration file has no JSON syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Open Cline settings in VS Code, go to 'MCP Servers' to check server status."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- The configuration is global and shared across all VS Code projects."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart VS Code after configuration changes."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file:"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(configPathHttp));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Restart VS Code or reload the Cline extension."));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure the configuration file has no JSON syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Open Cline settings in VS Code, go to 'MCP Servers' to check server status."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- The configuration is global and shared across all VS Code projects."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart VS Code after configuration changes."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
