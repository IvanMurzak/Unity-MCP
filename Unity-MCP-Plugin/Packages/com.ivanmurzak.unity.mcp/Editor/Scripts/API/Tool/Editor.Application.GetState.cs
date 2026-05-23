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
using AIGD;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor
    {
        public const string EditorApplicationGetStateToolId = "editor-application-get-state";
        [AiTool
        (
            EditorApplicationGetStateToolId,
            Title = "Editor / Application / Get State",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Return the current state of `UnityEditor.EditorApplication` — playmode, " +
            "paused state, compilation state, and related flags.")]
        [AiSkillBody("Returns available information about 'UnityEditor.EditorApplication'. " +
            "Use it to get information about the current state of the Unity Editor application. " +
            "Such as: playmode, paused state, compilation state, etc.\n\n" +
            "## Behavior\n\n" +
            "Snapshots Editor state via `EditorStatsData.FromEditor()` on the main thread and returns the result.")]
        [Description("Returns available information about 'UnityEditor.EditorApplication'. " +
            "Use it to get information about the current state of the Unity Editor application. " +
            "Such as: playmode, paused state, compilation state, etc.")]
        public EditorStatsData? GetApplicationState(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                return EditorStatsData.FromEditor();
            });
        }
    }
}
