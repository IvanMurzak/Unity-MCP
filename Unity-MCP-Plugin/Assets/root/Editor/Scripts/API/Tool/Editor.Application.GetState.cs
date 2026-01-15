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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor
    {
        public const string EditorApplicationGetStateToolId = "editor-application-get-state";
        [McpPluginTool
        (
            EditorApplicationGetStateToolId,
            Title = "Editor / Application / Get State"
        )]
        [Description("Returns available information about 'UnityEditor.EditorApplication'. " +
            "Use it to get information about the current state of the Unity Editor application. " +
            "Such as: playmode, paused state, compilation state, etc.")]
        public EditorStatsData? GetApplicationState()
        {
            return MainThread.Instance.Run(() =>
            {
                return EditorStatsData.FromEditor();
            });
        }
    }
}
