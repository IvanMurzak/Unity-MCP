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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Claude Desktop AI agent.
    /// </summary>
    public class ClaudeDesktopConfigurator2 : AiAgentConfigurator
    {
        public override string AgentName => "Claude Desktop";
        public override string AgentId => "claude-desktop";
        public override string DownloadUrl => "https://code.claude.com/docs/en/desktop";

        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.streamableHttp,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.streamableHttp,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

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

            troubleshootingContainerStdio.Add(TemplateLabelDescription("Claude Desktop has problems with runtime updates of MCP tools. It may not recognize them. That is why need to let Claude Desktop read MCP tools at the start of Claude Desktop app."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Start Unity first and the connection status should be 'Connecting...'"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open the file 'claude_desktop_config.json'."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Restart Claude Desktop. You may need to click 'Quit' in apps tray, because simple window close is not enough."));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("Claude Desktop has problems with runtime updates of MCP tools. It may not recognize them. That is why need to let Claude Desktop read MCP tools at the start of Claude Desktop app."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Start Unity first and the connection status should be 'Connecting...'"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
