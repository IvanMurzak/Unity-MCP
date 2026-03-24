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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Tool
    {
        public const string ToolSetEnabledStateId = "tool-set-enabled-state";

        [McpPluginTool
        (
            ToolSetEnabledStateId,
            Title = "Tool / Set Enabled State",
            Enabled = false
        )]
        [Description("Enable or disable MCP tools by name. " +
            "Allows controlling which tools are available for the AI agent.")]
        public ResultData SetEnabledState
        (
            [Description("Array of tools with their desired enabled state.")]
            InputData[] tools,

            [Description("Include operation logs in the result. Default: false")]
            bool? includeLogs = false
        )
        {
            if (tools == null || tools.Length == 0)
                throw new ArgumentException(Error.ToolsArrayIsNullOrEmpty());

            return MainThread.Instance.Run(() =>
            {
                var toolManager = UnityMcpPluginEditor.Instance.Tools
                    ?? throw new InvalidOperationException(Error.ToolManagerNotAvailable());

                var logs = includeLogs == true ? new Logs() : null;
                var success = new Dictionary<string, bool>();
                var changed = false;
                var (exactLookup, caseInsensitiveLookup) = BuildToolLookup(toolManager);

                foreach (var input in tools)
                {
                    if (string.IsNullOrWhiteSpace(input.Name))
                    {
                        var key = input.Name ?? string.Empty;
                        success[key] = false;
                        logs?.Error("Tool name is null or empty.");
                        continue;
                    }

                    var resolvedName = ResolveToolName(exactLookup, caseInsensitiveLookup, input.Name!, logs);
                    if (resolvedName == null)
                    {
                        success[input.Name!] = false;
                        continue;
                    }

                    var currentState = toolManager.IsToolEnabled(resolvedName);
                    if (currentState == input.Enabled)
                    {
                        success[input.Name!] = true;
                        logs?.Info($"Tool '{resolvedName}' is already {(input.Enabled ? "enabled" : "disabled")}.");
                        continue;
                    }

                    toolManager.SetToolEnabled(resolvedName, input.Enabled);
                    success[input.Name!] = true;
                    changed = true;
                    logs?.Info($"Tool '{resolvedName}' has been {(input.Enabled ? "enabled" : "disabled")}.");
                }

                if (changed)
                    UnityMcpPluginEditor.Instance.Save();

                return new ResultData
                {
                    Logs = logs,
                    Success = success
                };
            });
        }
    }
}
