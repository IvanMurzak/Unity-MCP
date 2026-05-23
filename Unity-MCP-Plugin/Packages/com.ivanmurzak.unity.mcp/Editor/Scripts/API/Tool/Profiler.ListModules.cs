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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerListModulesToolId = "profiler-list-modules";
        [AiTool
        (
            ProfilerListModulesToolId,
            Title = "Profiler / List Modules",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("List all known profiler module names with their local 'enabled' bookkeeping flag.")]
        [AiSkillBody("Returns `Tool_Profiler.AvailableModules` projected into a `ProfilerModulesData` list, " +
            "with each entry's `Enabled` field reflecting the wrapper's local bookkeeping.\n\n" +
            "## Behavior\n\n" +
            "Uses only built-in Unity APIs and in-process state. No external Unity package is required. " +
            "Pair with `profiler-enable-module` to flip the bookkeeping flag.")]
        [Description("Lists all available profiler modules and whether the wrapper considers each enabled.")]
        public ProfilerModulesData ListModules(string? nothing = null)
        {
            return MainThread.Instance.Run(() => new ProfilerModulesData
            {
                Modules = AvailableModules
                    .Select(name => new ProfilerModuleInfo
                    {
                        Name = name,
                        Enabled = EnabledModules.Contains(name)
                    })
                    .ToList()
            });
        }
    }
}
