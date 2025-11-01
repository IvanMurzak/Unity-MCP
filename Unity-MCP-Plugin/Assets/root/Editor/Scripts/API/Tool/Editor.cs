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
using com.IvanMurzak.McpPlugin.Common;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Editor
    {
        public static string EditorStats => @$"Editor Application information:
EditorApplication.isPlaying: {EditorApplication.isPlaying}
EditorApplication.isPaused: {EditorApplication.isPaused}
EditorApplication.isCompiling: {EditorApplication.isCompiling}
EditorApplication.isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}
EditorApplication.isUpdating: {EditorApplication.isUpdating}
EditorApplication.applicationContentsPath : {EditorApplication.applicationContentsPath}
EditorApplication.applicationPath : {EditorApplication.applicationPath}
EditorApplication.timeSinceStartup : {EditorApplication.timeSinceStartup}";

        public static class Error
        {
            public static string ScriptPathIsEmpty()
                => "[Error] Script path is empty. Please provide a valid path. Sample: \"Assets/Scripts/MyScript.cs\".";
        }
    }
}
