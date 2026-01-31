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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [Description("Profiler modules list data.")]
        public class ProfilerModulesData
        {
            [Description("List of all available profiler modules with their enabled status.")]
            public List<ProfilerModuleInfo>? Modules { get; set; }

            [Description("Total number of available modules.")]
            public int TotalModules { get; set; }

            [Description("Number of currently enabled modules.")]
            public int EnabledCount { get; set; }
        }

        [McpPluginTool
        (
            "Profiler_ListModules",
            Title = "List Profiler Modules"
        )]
        [Description(@"Lists all available profiler modules and their enabled status.
Note: Module states are tracked locally. Use Unity's Profiler window for actual module status.")]
        public ResponseCallValueTool<ProfilerModulesData?> ListModules()
        {
            return MainThread.Instance.Run(() =>
            {
                var modules = AvailableModules
                    .Select(name => new ProfilerModuleInfo
                    {
                        Name = name,
                        Enabled = enabledModules.Contains(name)
                    })
                    .ToList();

                var data = new ProfilerModulesData
                {
                    Modules = modules,
                    TotalModules = modules.Count,
                    EnabledCount = modules.Count(m => m.Enabled)
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<ProfilerModulesData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

