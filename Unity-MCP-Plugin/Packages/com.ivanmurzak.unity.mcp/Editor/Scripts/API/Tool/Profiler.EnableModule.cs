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
        public const string ProfilerEnableModuleToolId = "profiler-enable-module";
        [McpPluginTool
        (
            ProfilerEnableModuleToolId,
            Title = "Profiler / Enable Module",
            Enabled = false
        )]
        [McpPluginSkillDescription("Toggle the wrapper's local 'enabled' flag for a named profiler module. Bookkeeping only — Unity's runtime API does not expose direct module control; for real module visibility use the Profiler window.")]
        [McpPluginSkillBody("Adds or removes the given module name from the wrapper's `EnabledModules` set. " +
            "This is local bookkeeping consumed by `profiler-get-status` and `profiler-list-modules`; Unity's " +
            "runtime API does not allow programmatic toggling of Profiler-window modules from a built-in " +
            "namespace, so this tool intentionally does not pretend to.\n\n" +
            "## Inputs\n\n" +
            "- `moduleName` (required) — one of the names returned by `profiler-list-modules`.\n" +
            "- `enabled` (default `true`) — set to `false` to mark the module disabled.\n\n" +
            "## Errors\n\n" +
            "- Returns an `[Error]` string when `moduleName` is empty or unknown.")]
        [Description("Enables or disables a profiler module name in the wrapper's local bookkeeping set.")]
        public string EnableModule
        (
            [Description("Profiler module name (e.g. 'CPU', 'GPU', 'Memory').")]
            string moduleName,
            [Description("True to mark the module enabled in local bookkeeping; false to mark disabled.")]
            bool enabled = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(moduleName))
                    return Error.ModuleNameIsRequired();

                if (!AvailableModules.Contains(moduleName))
                    return Error.UnknownModule(moduleName);

                if (enabled)
                    EnabledModules.Add(moduleName);
                else
                    EnabledModules.Remove(moduleName);

                var status = enabled ? "enabled" : "disabled";
                return $"[Success] Profiler module '{moduleName}' marked {status} in local bookkeeping. " +
                    "Use Unity's Profiler window for actual module visibility control.";
            });
        }
    }
}
