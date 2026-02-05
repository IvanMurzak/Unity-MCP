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
    /// Configurator for Cursor AI agent.
    /// </summary>
    public class CursorConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Cursor";
        public override string AgentId => "cursor";
        public override string DownloadUrl => "https://cursor.com/download";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=dyk-4gTolSU";

        protected override string? IconFileName => "cursor-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cursor",
                "mcp.json"
            ),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cursor",
                "mcp.json"
            ),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cursor",
                "mcp.json"
            ),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cursor",
                "mcp.json"
            ),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file '%User%/.cursor/mcp.json'"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- '%User%/.cursor/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Open Cursor settings window, go to 'MCP Servers' to restart ai-game-developer or to get more information about the available MCP tools and the status of the server."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file '%User%/.cursor/mcp.json'"));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- '%User%/.cursor/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Open Cursor settings window, go to 'MCP Servers' to restart ai-game-developer or to get more information about the available MCP tools and the status of the server."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
