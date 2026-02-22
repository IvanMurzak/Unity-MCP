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
    /// Configurator for Claude Desktop AI agent.
    /// </summary>
    public class ClaudeDesktopConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Claude Desktop";
        public override string AgentId => "claude-desktop";
        public override string DownloadUrl => "https://code.claude.com/docs/en/desktop";

        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Token}={UnityMcpPlugin.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Token}={UnityMcpPlugin.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude",
                "claude_desktop_config.json"
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
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Claude",
                "claude_desktop_config.json"
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

            ContainerUnderHeader!.Add(TemplateWarningLabel("IMPORTANT: Highly recommended to use Claude Code instead, they share the same subscription plan."));
            ContainerUnderHeader!.Add(TemplateLabelDescription("Claude Desktop app is finicky, requires restart each time after Unity launched with active connecting."));

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open the file 'claude_desktop_config.json'."));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Restart Claude Desktop. You may need to click 'Quit' in apps tray, because simple window close is not enough."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Claude Desktop may launch two MCP server instances instead of one. If you must use Claude Desktop, manually terminate one of the instances. This behavior is unreliable — consider switching to Claude Code."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Claude Desktop may not detect runtime updates to MCP tools. Ensure Claude Desktop reads the MCP tools on startup."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Start Unity first; the connection status should read 'Connecting...'"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            ContainerHttp!.Clear();
            ContainerHttp!.Add(TemplateAlertLabel("CRITICAL: Claude Desktop does not support HTTP transport, only STDIO."));
        }
    }
}
