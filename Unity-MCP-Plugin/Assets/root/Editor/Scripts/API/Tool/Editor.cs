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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Editor
    {
        public static class Error
        {
            public static string ScriptPathIsEmpty()
                => "[Error] Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
        }

        [Description(@"Available information about 'UnityEditor.EditorApplication'.")]
        public class EditorStatsData
        {
            [Description("Whether the Editor is in Play mode.")]
            public bool IsPlaying { get; set; } = false;

            [Description("Whether the Editor is paused.")]
            public bool IsPaused { get; set; } = false;

            [Description("Is editor currently compiling scripts? (Read Only)")]
            public bool IsCompiling { get; set; } = false;

            [Description("Editor application state which is true only when the Editor is currently in or about to enter Play mode. (Read Only)")]
            public bool IsPlayingOrWillChangePlaymode { get; set; } = false;

            [Description("True if the Editor is currently refreshing the AssetDatabase. (Read Only)")]
            public bool IsUpdating { get; set; } = false;

            [Description("Path to the Unity editor contents folder. (Read Only)")]
            public string ApplicationContentsPath { get; set; } = string.Empty;

            [Description("Gets the path to the Unity Editor application. (Read Only)")]
            public string ApplicationPath { get; set; } = string.Empty;

            [Description("The time since the editor was started. (Read Only)")]
            public double TimeSinceStartup { get; set; } = 0;

            public static EditorStatsData FromEditor()
            {
                return new EditorStatsData
                {
                    IsPlaying = EditorApplication.isPlaying,
                    IsPaused = EditorApplication.isPaused,
                    IsCompiling = EditorApplication.isCompiling,
                    IsPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode,
                    IsUpdating = EditorApplication.isUpdating,
                    ApplicationContentsPath = EditorApplication.applicationContentsPath,
                    ApplicationPath = EditorApplication.applicationPath,
                    TimeSinceStartup = EditorApplication.timeSinceStartup
                };
            }
        }
    }
}
