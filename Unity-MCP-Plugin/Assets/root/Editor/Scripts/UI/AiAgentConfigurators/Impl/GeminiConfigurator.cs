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
    /// Configurator for Gemini AI agent.
    /// </summary>
    public class GeminiConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Gemini";
        public override string AgentId => "gemini";
        public override string DownloadUrl => "https://geminicli.com/docs/get-started/installation/";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/GeminiConfig.uxml");
        protected override string? IconFileName => "gemini-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        );

        protected override void OnUICreated(VisualElement root)
        {
            var textFieldGoToFolder = root.Q<TextField>("terminalGoToFolder") ?? throw new NullReferenceException("TextField 'terminalGoToFolder' not found in UI.");
            var textFieldConfigureGemini = root.Q<TextField>("terminalConfigureGemini") ?? throw new NullReferenceException("TextField 'terminalConfigureGemini' not found in UI.");
            var textFieldJsonConfig = root.Q<TextField>("jsonConfig") ?? throw new NullReferenceException("TextField 'jsonConfig' not found in UI.");

            var addMcpServerCommand = $"gemini mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";

            textFieldGoToFolder.value = $"cd \"{ProjectRootPath}\"";
            textFieldConfigureGemini.value = addMcpServerCommand;
            textFieldJsonConfig.value = Startup.Server.RawJsonConfigurationStdio(
                port: UnityMcpPlugin.Port,
                bodyPath: Consts.MCP.Server.DefaultBodyPath,
                timeoutMs: UnityMcpPlugin.TimeoutMs).ToString();

            base.OnUICreated(root);
        }
    }
}
