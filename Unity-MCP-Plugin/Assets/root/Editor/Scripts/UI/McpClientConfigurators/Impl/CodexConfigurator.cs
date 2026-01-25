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
using com.IvanMurzak.Unity.MCP.Editor.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Configurator for Codex MCP client.
    /// </summary>
    public class CodexConfigurator : McpClientConfiguratorBase
    {
        public override string ClientName => "Codex";
        public override string ClientId => "codex";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/clients/CodexConfig.uxml");
    }
}
