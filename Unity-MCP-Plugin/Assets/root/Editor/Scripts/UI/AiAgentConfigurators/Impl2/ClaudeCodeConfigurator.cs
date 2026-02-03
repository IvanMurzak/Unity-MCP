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
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Claude Code AI agent.
    /// </summary>
    public class ClaudeCodeConfigurator2 : AiAgentConfigurator
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
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        );

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            var addMcpServerCommand = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(addMcpServerCommand));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Start Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerStdio!.Add(manualStepsContainer);

            // HTTP Configuration

            // TODO: implement
        }
    }
}
