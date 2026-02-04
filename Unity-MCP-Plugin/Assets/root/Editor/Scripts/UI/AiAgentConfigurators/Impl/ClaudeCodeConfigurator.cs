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
    public class ClaudeCodeConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=xUYV2yxsaLs";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/ClaudeCodeConfig.uxml");
        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
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
            transportMethod: TransportMethod.stdio,
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
            var textFieldGoToFolder = root.Q<TextField>("terminalGoToFolder") ?? throw new NullReferenceException("TextField 'terminalGoToFolder' not found in UI.");
            var textFieldConfigureClaudeCode = root.Q<TextField>("terminalConfigureClaudeCode") ?? throw new NullReferenceException("TextField 'terminalConfigureClaudeCode' not found in UI.");

            var addMcpServerCommand = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";

            textFieldGoToFolder.value = $"cd \"{ProjectRootPath}\"";
            textFieldConfigureClaudeCode.value = addMcpServerCommand;

            base.OnUICreated(root);
        }
    }
}
