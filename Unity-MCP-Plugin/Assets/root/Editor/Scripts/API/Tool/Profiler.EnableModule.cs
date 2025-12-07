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
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_EnableModule",
            Title = "Enable/Disable Profiler Module"
        )]
        [Description(@"Enables or disables a specific profiler module.
Available modules: CPU, GPU, Rendering, Memory, Audio, Video, Physics, Physics2D, NetworkMessages, NetworkOperations, UI, UIDetails, GlobalIllumination, VirtualTexturing.
Note: Module enabling/disabling is tracked locally. Use Unity's Profiler window for actual module control.")]
        public string EnableModule
        (
            [Description("The name of the profiler module (e.g., 'CPU', 'GPU', 'Memory').")]
            string moduleName,
            [Description("Whether to enable (true) or disable (false) the module.")]
            bool enabled = true
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(moduleName))
                return Error.ModuleNameIsRequired();

            if (!AvailableModules.Contains(moduleName))
                return Error.UnknownModule(moduleName);

            if (enabled)
                enabledModules.Add(moduleName);
            else
                enabledModules.Remove(moduleName);

            var status = enabled ? "enabled" : "disabled";
            return $"[Success] Profiler module '{moduleName}' has been {status}.\nNote: Module enabling/disabling is tracked locally. Use Unity's Profiler window for actual module control.";
        });
    }
}

