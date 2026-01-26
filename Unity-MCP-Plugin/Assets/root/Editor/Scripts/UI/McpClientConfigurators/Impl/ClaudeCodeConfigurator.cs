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

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Configurator for Claude Code AI agent.
    /// </summary>
    public class ClaudeCodeConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/ClaudeCodeConfig.uxml");
        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json"
            ),
            bodyPath: $"projects{Consts.MCP.Server.BodyPathDelimiter}"
                + $"{ProjectRootPath}{Consts.MCP.Server.BodyPathDelimiter}"
                + Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
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

            root.Q<TextField>("terminalGoToFolder").value = $"cd \"{ProjectRootPath}\"";

            var addMcpServerCommand = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} client-transport=stdio";
            root.Q<TextField>("terminalConfigureClaudeCode").value = addMcpServerCommand;
        }
    }
}
