/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using AIGD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Tool
    {
        public const string ToolSetVisibilityId = "tool-set-visibility";

        [AiTool
        (
            ToolSetVisibilityId,
            Title = "Tool / Set Visibility",
            Visible = false
        )]
        [AiSkillDescription("Show or hide MCP tools by name in batch: a visible tool is listed to MCP clients (tools/list), a hidden one " +
            "is left out of the list to save context but still runs when called by name. " +
            "Persists the change via `UnityMcpPluginEditor.Instance.Save()` only when at least one tool actually flipped. " +
            "Returns per-input success flags plus optional operation logs.")]
        [AiSkillBody("Show or hide MCP tools by name. Visibility only decides which tools MCP clients see listed " +
            "(`tools/list`), which keeps their context small; it is not an access control: a hidden tool still runs " +
            "through `tools/call`.\n\n" +
            "## Inputs\n\n" +
            "- `tools` — array of `ToolVisibilityInput { Name, Visible }`. Non-empty.\n" +
            "- `includeLogs` (default `false`) — when true, returns per-step operation logs alongside the success map.\n\n" +
            "## Behavior\n\n" +
            "Each entry is resolved against the tool manager's exact-name and case-insensitive lookups. " +
            "Already-correct state short-circuits as success without writing. The plugin's config is saved once at the " +
            "end iff at least one tool actually changed state.\n\n" +
            "`tool-set-enabled-state` is the deprecated old name of this tool (input `{ Name, Enabled }`); it works the same.")]
        [Description("Show or hide MCP tools by name. A hidden tool is left out of tools/list but still runs when called by name.")]
        public ToolToggleResult SetVisibility
        (
            [Description("Array of tools with their desired visibility.")]
            ToolVisibilityInput[] tools,

            [Description("Include operation logs in the result. Default: false")]
            bool? includeLogs = false
        )
        {
            if (tools == null || tools.Length == 0)
                throw new ArgumentException(Error.ToolsArrayIsNullOrEmpty());

            return ApplyVisibility(tools.Select(t => (t?.Name, t?.Visible ?? false)), includeLogs == true);
        }

        /// <summary>Shared by <see cref="SetVisibility"/> and its deprecated alias <see cref="SetEnabledState"/>.</summary>
        static ToolToggleResult ApplyVisibility(IEnumerable<(string? Name, bool Visible)> requests, bool includeLogs)
        {
            return MainThread.Instance.Run(() =>
            {
                var toolManager = UnityMcpPluginEditor.Instance.Tools
                    ?? throw new InvalidOperationException(Error.ToolManagerNotAvailable());

                var logs = includeLogs ? new Logs() : null;
                var success = new Dictionary<string, bool>();
                var changed = false;
                var (exactLookup, caseInsensitiveLookup) = BuildToolLookup(toolManager);

                foreach (var (name, visible) in requests)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        success[name ?? string.Empty] = false;
                        logs?.Error("Tool name is null or empty.");
                        continue;
                    }

                    var resolvedName = ResolveToolName(exactLookup, caseInsensitiveLookup, name!, logs);
                    if (resolvedName == null)
                    {
                        success[name!] = false;
                        continue;
                    }

                    if (toolManager.IsToolVisible(resolvedName) == visible)
                    {
                        success[name!] = true;
                        logs?.Info($"Tool '{resolvedName}' is already {(visible ? "visible" : "hidden")}.");
                        continue;
                    }

                    toolManager.SetToolVisible(resolvedName, visible);
                    success[name!] = true;
                    changed = true;
                    logs?.Info($"Tool '{resolvedName}' is now {(visible ? "visible" : "hidden")}.");
                }

                if (changed)
                    UnityMcpPluginEditor.Instance.Save();

                return new ToolToggleResult
                {
                    Logs = logs,
                    Success = success
                };
            });
        }
    }
}
