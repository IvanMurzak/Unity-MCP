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
    /// Configurator for Open Code AI agent.
    /// </summary>
    public class OpenCodeConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Open Code";
        public override string AgentId => "open-code";
        public override string DownloadUrl => "https://opencode.ai/download";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/OpenCodeConfig.uxml");
        protected override string? IconFileName => "open-code-64.png";
        protected override AiAgentConfig CreateConfigWindows() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            bodyPath: "mcp"
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            bodyPath: "mcp"
        );

        protected override void OnUICreated(VisualElement root)
        {
            var textFieldGoToFolder = root.Q<TextField>("terminalGoToFolder") ?? throw new NullReferenceException("TextField 'terminalGoToFolder' not found in UI.");
            var textFieldJsonConfig = root.Q<TextField>("jsonConfig") ?? throw new NullReferenceException("TextField 'jsonConfig' not found in UI.");

            textFieldGoToFolder.value = $"cd \"{ProjectRootPath}\"";
            textFieldJsonConfig.value = ClientConfig.ExpectedFileContent;

            base.OnUICreated(root);
        }
    }
}
