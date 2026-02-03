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

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Claude Desktop AI agent.
    /// </summary>
    public class ClaudeDesktopConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Claude Desktop";
        public override string AgentId => "claude-desktop";
        public override string DownloadUrl => "https://code.claude.com/docs/en/desktop";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/ClaudeDesktopConfig.uxml");
        protected override string? IconFileName => "claude-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude",
                "claude_desktop_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
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
            var textFieldJsonConfig = root.Q<TextField>("jsonConfig") ?? throw new NullReferenceException("TextField 'jsonConfig' not found in UI.");
            textFieldJsonConfig.value = ClientConfig.ExpectedFileContent;

            base.OnUICreated(root);
        }
    }
}
