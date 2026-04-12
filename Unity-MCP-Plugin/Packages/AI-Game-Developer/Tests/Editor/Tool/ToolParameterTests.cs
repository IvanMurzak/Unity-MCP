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
using System.Linq;
using System.Reflection;
using com.IvanMurzak.McpPlugin;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolParameterTests
    {
        [Test]
        public void AllMcpTools_ShouldHaveAtLeastOneParameter()
        {
            var toolMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); }
                })
                .Where(type => type.GetCustomAttribute<McpPluginToolTypeAttribute>() != null)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Where(method => method.GetCustomAttribute<McpPluginToolAttribute>() != null)
                .ToList();

            Assert.IsTrue(toolMethods.Count > 0, "Should find at least one MCP tool method");

            var zeroParamTools = new List<string>();
            foreach (var method in toolMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    var attr = method.GetCustomAttribute<McpPluginToolAttribute>()!;
                    zeroParamTools.Add($"  - {attr.Name} ({method.DeclaringType?.Name}.{method.Name})");
                }
            }

            Assert.IsEmpty(zeroParamTools,
                $"The following MCP tools have no input parameters, which breaks some MCP clients (e.g. GitHub Copilot):\n"
                + string.Join("\n", zeroParamTools));
        }
    }
}
