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
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Configurator for Visual Studio Code (Copilot) MCP client.
    /// </summary>
    public class VSCodeCopilotConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Visual Studio Code (Copilot)";
        public override string AgentId => "vscode-copilot";
        public override string DownloadUrl => "https://code.visualstudio.com/download";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/clients/VSCodeCopilotConfig.uxml");

        protected override AiAgentConfig CreateConfigWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".vscode", "mcp.json"),
            bodyPath: "servers"
        );

        protected override AiAgentConfig CreateConfigMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".vscode", "mcp.json"),
            bodyPath: "servers"
        );
    }
}
