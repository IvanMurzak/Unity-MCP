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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The tools' data layer for the whole test run: the configured toolFormat (default Rql), or the one given with
    /// <c>-toolDataLayer Json|Rql</c> on the Unity command line, so the suite can be run under each.
    /// </summary>
    [SetUpFixture]
    public class ToolDataLayerSetUp
    {
        /// <summary>The binding of the plugin's tool manager (null = ReflectorNet), for tools a test creates itself.</summary>
        public static IToolBinding? Binding => UnityMcpPluginEditor.Instance.Tools?.ToolBinding;

        public static string Active => Binding is RqlToolBinding ? "Rql" : "Json";

        [OneTimeSetUp]
        public void Apply()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-toolDataLayer");
            if (i >= 0 && i + 1 < args.Length && Enum.TryParse<ToolPresentationFormat>(args[i + 1], true, out var forced))
            {
                // Also in the config, so a plugin rebuilt during the run keeps the forced layer.
                UnityMcpPluginEditor.Instance.ConnectionConfigForTests.ToolFormat = forced;
                if (UnityMcpPluginEditor.Instance.McpPluginInstance is { } plugin)
                    UnityMcpPluginEditor.Instance.ApplyToolFormat(plugin);
            }
            Debug.Log($"[TESTS] tool data layer: {Active}");
        }
    }
}
