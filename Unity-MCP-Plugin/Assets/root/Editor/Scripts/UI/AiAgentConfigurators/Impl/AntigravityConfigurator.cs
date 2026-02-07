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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Antigravity AI agent.
    /// </summary>
    public class AntigravityConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Antigravity";
        public override string AgentId => "antigravity";
        public override string DownloadUrl => "https://antigravity.google/download";

        protected override string? IconFileName => "antigravity-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .AddIdentityKey("serverUrl")
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("serverUrl")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .AddIdentityKey("serverUrl")
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("serverUrl")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .AddIdentityKey("serverUrl")
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("serverUrl", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .AddIdentityKey("serverUrl")
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("serverUrl", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file '%User%/.gemini/antigravity/mcp_config.json'"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Antigravity after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file '%User%/.gemini/antigravity/mcp_config.json'"));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Antigravity after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
