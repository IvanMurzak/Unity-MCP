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
using UnityEditorInternal;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerClearDataToolId = "profiler-clear-data";
        [McpPluginTool
        (
            ProfilerClearDataToolId,
            Title = "Profiler / Clear Data",
            DestructiveHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Discard all frames currently held by the Editor Profiler (UnityEditorInternal.ProfilerDriver.ClearAllFrames). Cannot be undone.")]
        [McpPluginSkillBody("Invokes `UnityEditorInternal.ProfilerDriver.ClearAllFrames()` on the main thread. " +
            "`UnityEditorInternal` is a built-in editor namespace — no external Unity package is required.\n\n" +
            "## Behavior\n\n" +
            "After this call, the Profiler window's frame history is empty; subsequent recording starts from frame 0. " +
            "Returns `true` on success.")]
        [Description("Clears all frames currently held by the Unity Editor Profiler.")]
        public bool ClearData(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                ProfilerDriver.ClearAllFrames();
                return true;
            });
        }
    }
}
