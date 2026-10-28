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
        /// <summary>Deprecated old name of <see cref="ToolSetVisibilityId"/>, kept working until the next major release.</summary>
        public const string ToolSetEnabledStateId = "tool-set-enabled-state";

        [AiTool
        (
            ToolSetEnabledStateId,
            Title = "Tool / Set Enabled State (deprecated: tool-set-visibility)",
            Visible = false
        )]
        [AiSkillDescription("DEPRECATED: use `tool-set-visibility`. Shows (Enabled=true) or hides (Enabled=false) MCP tools by name in batch; " +
            "a hidden tool is only left out of tools/list and still runs when called by name. " +
            "Persists the change via `UnityMcpPluginEditor.Instance.Save()` only when at least one tool actually flipped. " +
            "Returns per-input success flags plus optional operation logs.")]
        [AiSkillBody("Deprecated old name of `tool-set-visibility`, kept working until the next major release. " +
            "`Enabled` means visible: the flag only decides whether MCP clients see the tool listed (`tools/list`); " +
            "a hidden tool still runs through `tools/call`.\n\n" +
            "## Inputs\n\n" +
            "- `tools` — array of `ToolToggleInput { Name, Enabled }`. Non-empty.\n" +
            "- `includeLogs` (default `false`) — when true, returns per-step operation logs alongside the success map.\n\n" +
            "## Behavior\n\n" +
            "Same as `tool-set-visibility`: each entry is resolved against the tool manager's exact-name and case-insensitive " +
            "lookups, already-correct state short-circuits as success, and the config is saved once at the end iff a tool changed.")]
        [Description("DEPRECATED, use tool-set-visibility. Show (Enabled=true) or hide (Enabled=false) MCP tools by name; " +
            "a hidden tool is left out of tools/list but still runs when called by name.")]
        public ToolToggleResult SetEnabledState
        (
            [Description("Array of tools with their desired visibility (Enabled = visible).")]
            ToolToggleInput[] tools,

            [Description("Include operation logs in the result. Default: false")]
            bool? includeLogs = false
        )
        {
            if (tools == null || tools.Length == 0)
                throw new ArgumentException(Error.ToolsArrayIsNullOrEmpty());

            var requests = new List<(string? Name, bool Visible)>(tools.Length);
            foreach (var input in tools)
                requests.Add((input?.Name, input?.Enabled ?? false));
            return ApplyVisibility(requests, includeLogs == true);
        }
    }
}
