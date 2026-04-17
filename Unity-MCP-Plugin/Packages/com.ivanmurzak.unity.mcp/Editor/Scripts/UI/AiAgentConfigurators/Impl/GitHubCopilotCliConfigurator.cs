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
    /// Configurator for Copilot CLI AI agent.
    /// Uses project-local `.mcp.json` in the Unity project root. Copilot CLI v1.0.12+
    /// discovers workspace-local MCP configs from the working directory up to the git
    /// root, so the same `.mcp.json` is shared with Claude Code.
    /// </summary>
    public class GitHubCopilotCliConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "GitHub Copilot CLI";
        public override string AgentId => "github-copilot-cli";
        public override string DownloadUrl => "https://github.com/features/copilot/cli";
        public override string? SkillsPath => Path.Combine(ProjectRootPath, ".claude", "skills");

        protected override string? IconFileName => "github-copilot-64.png";

        private static string LocalConfigPath => Path.Combine(ProjectRootPath, ".mcp.json");

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetProperty("tools", new JsonArray { "*" }, requiredForConfiguration: false)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetProperty("tools", new JsonArray { "*" }, requiredForConfiguration: false)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("tools", new JsonArray { "*" }, requiredForConfiguration: false)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("tools", new JsonArray { "*" }, requiredForConfiguration: false)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var startContainerStdio = TemplateFoldoutFirst("Start");
            startContainerStdio!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerStdio!.Add(TemplateLabelDescription("Launch GitHub Copilot CLI"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly("copilot"));
            ContainerStdio!.Add(startContainerStdio);

            var manualStepsContainer = TemplateFoldout("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file '.mcp.json' in the Unity project root"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Copilot CLI is launched from the Unity project root (the folder containing '.mcp.json')"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Requires GitHub Copilot CLI v1.0.12+ which discovers '.mcp.json' at project level"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Copilot CLI after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var startContainerHttp = TemplateFoldoutFirst("Start");
            startContainerHttp!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerHttp!.Add(TemplateLabelDescription("Launch GitHub Copilot CLI"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly("copilot"));
            ContainerHttp!.Add(startContainerHttp);

            var manualStepsContainerHttp = TemplateFoldout("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file '.mcp.json' in the Unity project root"));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Copilot CLI is launched from the Unity project root (the folder containing '.mcp.json')"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Requires GitHub Copilot CLI v1.0.12+ which discovers '.mcp.json' at project level"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Copilot CLI after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
