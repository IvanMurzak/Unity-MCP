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
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginPromptType]
    public partial class Prompt_Editor
    {
        [McpPluginPrompt(Name = "get-editor-status", Role = Role.Assistant)]
        [Description("Get current editor status.")]
        public string GetStatus()
        {
            return MainThread.Instance.Run(() =>
            {
                return $"Application.isPlaying={Application.isPlaying}";
            });
        }

        [McpPluginPrompt(Name = "set-editor-play-mode", Role = Role.User)]
        [Description("Set Editor Play Mode.")]
        public string SetPlayMode(bool isPlaying)
        {
            return $"Set Unity Editor Play Mode to '{isPlaying}'";
        }

        [McpPluginPrompt(Name = "make-number", Role = Role.User)]
        [Description("Make a number.")]
        public string MakeNumber(int abc)
        {
            return $"Made number: {abc}";
        }
    }
}