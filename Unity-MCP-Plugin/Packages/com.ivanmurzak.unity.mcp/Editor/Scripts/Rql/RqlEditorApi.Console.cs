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
using System;
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>A component type the project can add (componentTypes source).</summary>
    public sealed class RqlComponentType
    {
        public RqlComponentType(Type type) { name = type.Name; fullName = type.FullName ?? type.Name; @namespace = type.Namespace ?? ""; }
        public string name { get; }
        public string fullName { get; }
        public string @namespace { get; }
    }

    public sealed partial class RqlEditorApi
    {
        /// <summary>Most entries the <c>console</c> source returns (newest kept).</summary>
        public const int MaxLogEntries = 1000;

        [RqlCallable("clear the Editor Console and the captured log entries")]
        [RqlNoAutoSave]
        public void ClearConsole()
        {
            Debug.ClearDeveloperConsole();
            if (UnityMcpPluginEditor.HasInstance) UnityMcpPluginEditor.Instance.LogCollector?.Clear();
        }

        /// <summary>
        /// The <c>console</c> source: captured Editor log entries, oldest first, without the plugin's echo of its own tool
        /// responses (an agent checking "errors after my change" would otherwise find its own failed calls).
        /// </summary>
        public static IReadOnlyList<LogEntry> ConsoleEntries() =>
            UnityMcpPluginEditor.HasInstance && UnityMcpPluginEditor.Instance.LogCollector is { } logs
                ? NewestWithoutEchoes(window => logs.Query(maxEntries: window, includeStackTrace: true), MaxLogEntries)
                : (IReadOnlyList<LogEntry>)Array.Empty<LogEntry>();

        /// <summary>
        /// The newest <paramref name="max"/> entries that are not tool response echoes, oldest first. The echoes are left out
        /// before the limit: <paramref name="query"/> (the newest <c>n</c> entries, oldest first) is read with a growing
        /// window until it holds <paramref name="max"/> other entries or everything stored, so a busy session still gets
        /// <paramref name="max"/> real entries.
        /// </summary>
        internal static List<LogEntry> NewestWithoutEchoes(Func<int, IReadOnlyList<LogEntry>> query, int max)
        {
            for (var window = max; ; window = window > int.MaxValue / 2 ? int.MaxValue : window * 2)
            {
                var newest = query(window);
                var kept = newest.Where(e => !IsToolResponseEcho(e.Message)).ToList();
                if (kept.Count >= max || newest.Count < window || window == int.MaxValue)
                    return kept.Count > max ? kept.GetRange(kept.Count - max, max) : kept;
            }
        }

        // McpPlugin's McpToolManager logs every tool response it sends ("[AI] McpToolManager Error Response to AI: …").
        internal static bool IsToolResponseEcho(string? message) =>
            message != null && (message.Contains("McpToolManager Error Response to AI") || message.Contains("McpToolManager Success Response to AI"));

        /// <summary>The <c>shaders</c> source: every shader the Editor knows (built-in and project).</summary>
        public static ShaderInfo[] Shaders() => ShaderUtil.GetAllShaderInfo();

        // TypeCache cannot change within a domain (statics reset on reload): build the immutable list once.
        private static List<RqlComponentType>? _componentTypes;

        /// <summary>The <c>componentTypes</c> source: concrete component types that can be added (engine, packages, scripts).</summary>
        public static List<RqlComponentType> ComponentTypes() => new List<RqlComponentType>(_componentTypes ??=
            TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => t.IsVisible && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Select(t => new RqlComponentType(t))
                .OrderBy(t => t.name, StringComparer.Ordinal)
                .ToList());
    }
}
