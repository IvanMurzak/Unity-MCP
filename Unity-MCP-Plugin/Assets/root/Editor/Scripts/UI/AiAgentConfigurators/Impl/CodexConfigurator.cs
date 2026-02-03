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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Codex AI agent.
    /// </summary>
    public class CodexConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Codex";
        public override string AgentId => "codex";
        public override string DownloadUrl => "https://openai.com/codex/";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/CodexConfig.uxml");
        protected override string? IconFileName => "codex-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        );

        protected override void OnUICreated(VisualElement root)
        {
            var textFieldGoToFolder = root.Q<TextField>("terminalGoToFolder") ?? throw new NullReferenceException("TextField 'terminalGoToFolder' not found in UI.");
            var textFieldConfigureCodex = root.Q<TextField>("terminalConfigureCodex") ?? throw new NullReferenceException("TextField 'terminalConfigureCodex' not found in UI.");
            var textFieldTomlConfig = root.Q<TextField>("tomlConfig") ?? throw new NullReferenceException("TextField 'tomlConfig' not found in UI.");

            var addMcpServerCommand = $"codex mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";

            textFieldGoToFolder.value = $"cd \"{ProjectRootPath}\"";
            textFieldConfigureCodex.value = addMcpServerCommand;
            textFieldTomlConfig.value = ClientConfig.ExpectedFileContent;

            base.OnUICreated(root);
        }
    }
}
