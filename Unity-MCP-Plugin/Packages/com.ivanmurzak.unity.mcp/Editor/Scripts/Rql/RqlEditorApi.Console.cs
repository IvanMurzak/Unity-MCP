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

        /// <summary>The <c>console</c> source: captured Editor log entries, oldest first.</summary>
        public static IReadOnlyList<LogEntry> ConsoleEntries() =>
            UnityMcpPluginEditor.HasInstance && UnityMcpPluginEditor.Instance.LogCollector is { } logs
                ? logs.Query(maxEntries: MaxLogEntries, includeStackTrace: true)
                : Array.Empty<LogEntry>();

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
