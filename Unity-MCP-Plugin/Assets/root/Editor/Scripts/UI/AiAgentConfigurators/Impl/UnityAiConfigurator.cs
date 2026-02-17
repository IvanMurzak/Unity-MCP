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
    /// Configurator for Unity AI agent.
    /// </summary>
    public class UnityAiConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Unity AI";
        public override string AgentId => "unity-ai";
        public override string DownloadUrl => "https://unity.com/features/ai";

        protected override string? IconFileName => "unity-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                "UserSettings",
                "mcp.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                "UserSettings",
                "mcp.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                "UserSettings",
                "mcp.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                "UserSettings",
                "mcp.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file 'UserSettings/mcp.json'"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- 'UserSettings/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Open Unity AI settings window, go to 'MCP Servers' to restart ai-game-developer or to get more information about the available MCP tools and the status of the server."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            ContainerHttp!.Add(TemplateAlertLabel("Please consider to switch to STDIO transport for local development."));

            ContainerHttp!.Add(TemplateWarningLabel("Unity AI agent is cloud based. To use HTTP transport you must to host MCP server in a cloud with https public access. You may use docker for that. Avoid using 'localhost' in your url."));

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file 'UserSettings/mcp.json'"));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- 'UserSettings/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Open Unity AI settings window, go to 'MCP Servers' to restart ai-game-developer or to get more information about the available MCP tools and the status of the server."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
