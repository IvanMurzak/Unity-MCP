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
    /// Configurator for Claude Code AI agent.
    /// </summary>
    public class ClaudeCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=xUYV2yxsaLs";

        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
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
                ".claude.json"
            ),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
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
                ".claude.json"
            ),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
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

            var addMcpServerCommandStdio = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(addMcpServerCommandStdio));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Start Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Check that the configuration file .claude.json exists"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            var addMcpServerCommandHttp = $"claude mcp add --transport http {AiAgentConfig.DefaultMcpServerName} {UnityMcpPlugin.Host}";

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(addMcpServerCommandHttp));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Start Claude Code"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Check that the configuration file .claude.json exists"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
