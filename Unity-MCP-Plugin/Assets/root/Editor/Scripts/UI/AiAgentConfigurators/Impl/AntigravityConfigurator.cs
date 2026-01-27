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
    /// Configurator for Antigravity AI agent.
    /// </summary>
    public class AntigravityConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Antigravity";
        public override string AgentId => "antigravity";
        public override string DownloadUrl => "https://antigravity.google/download";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/AntigravityConfig.uxml");
        protected override string? IconFileName => "antigravity-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "mcp_config.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override void OnUICreated(VisualElement root)
        {
            var textFieldJsonConfig = root.Q<TextField>("jsonConfig") ?? throw new NullReferenceException("TextField 'jsonConfig' not found in UI.");
            textFieldJsonConfig.value = Startup.Server.RawJsonConfigurationStdio(
                port: UnityMcpPlugin.Port,
                bodyPath: Consts.MCP.Server.DefaultBodyPath,
                timeoutMs: UnityMcpPlugin.TimeoutMs).ToString();

            base.OnUICreated(root);
        }
    }
}
