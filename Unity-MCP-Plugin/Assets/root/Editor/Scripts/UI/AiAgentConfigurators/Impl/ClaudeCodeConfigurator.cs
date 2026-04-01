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
using System.IO;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Claude Code AI agent.
    /// </summary>
    public class ClaudeCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";
        public override string TutorialUrl => "https://youtu.be/Sknh2p12W8c";
        public override string? SkillsPath => Path.Combine(ProjectRootPath, ".claude", "skills");

        protected override string? IconFileName => "claude-64.png";

        private static string LocalConfigPath => Path.Combine(ProjectRootPath, ".mcp.json");

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var isCloud = UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud;
            var isAuthRequired = isCloud || UnityMcpPluginEditor.AuthOption == AuthOption.required;

            // STDIO Configuration

            var startContainerStdio = TemplateFoldoutFirst("Start");
            startContainerStdio!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerStdio!.Add(TemplateLabelDescription("Launch Claude Code"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly("claude"));
            ContainerStdio!.Add(startContainerStdio);

            var manualStepsContainer = TemplateFoldout("Manual Configuration Steps");

            var tokenStdio = !string.IsNullOrEmpty(UnityMcpPluginEditor.Token) ? UnityMcpPluginEditor.Token : "<token>";
            var authArgsStdio = isAuthRequired
                ? $" {Args.Authorization}={AuthOption.required} {Args.Token}={tokenStdio}"
                : string.Empty;

            var addMcpServerCommandStdio = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{McpServerManager.ExecutableFullPath}\" port={UnityMcpPluginEditor.Port} plugin-timeout={UnityMcpPluginEditor.TimeoutMs} client-transport=stdio{authArgsStdio}";

            manualStepsContainer!.Add(TemplateLabelDescription("Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(addMcpServerCommandStdio));
            manualStepsContainer!.Add(TemplateLabelDescription("Restart or start Claude Code to apply the configuration"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Check that the configuration file .mcp.json exists"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var startContainerHttp = TemplateFoldoutFirst("Start");
            startContainerHttp!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerHttp!.Add(TemplateLabelDescription("Launch Claude Code"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly("claude"));
            ContainerHttp!.Add(startContainerHttp);

            var manualStepsContainerHttp = TemplateFoldout("Manual Configuration Steps");

            var tokenHttp = !string.IsNullOrEmpty(UnityMcpPluginEditor.Token) ? UnityMcpPluginEditor.Token : "<token>";
            var authHeaderHttp = isAuthRequired
                ? $" --header \"Authorization: Bearer {tokenHttp}\""
                : string.Empty;

            var addMcpServerCommandHttp = $"claude mcp add --transport http {AiAgentConfig.DefaultMcpServerName} {UnityMcpPluginEditor.Host}{authHeaderHttp}";

            manualStepsContainerHttp!.Add(TemplateLabelDescription("Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(addMcpServerCommandHttp));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("Restart or start Claude Code to apply the configuration"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Check that the configuration file .mcp.json exists"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
