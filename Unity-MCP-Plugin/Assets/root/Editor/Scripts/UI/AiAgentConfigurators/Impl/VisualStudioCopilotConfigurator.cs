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
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Visual Studio (Copilot) AI Agent.
    /// </summary>
    public class VisualStudioCopilotConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Visual Studio (Copilot)";
        public override string AgentId => "vs-copilot";
        public override string DownloadUrl => "https://visualstudio.microsoft.com/downloads/";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=RGdak4T69mc";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/VisualStudioCopilotConfig.uxml");
        protected override string? IconFileName => "visual-studio-64.png";

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override void OnUICreated(VisualElement root)
        {
            var textFieldJsonConfig = root.Q<TextField>("jsonConfig") ?? throw new NullReferenceException("TextField 'jsonConfig' not found in UI.");
            textFieldJsonConfig.value = ClientConfig.ExpectedFileContent;

            base.OnUICreated(root);
        }
    }
}
