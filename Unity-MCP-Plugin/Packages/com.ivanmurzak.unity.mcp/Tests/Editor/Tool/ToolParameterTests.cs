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
        /// <summary>Every [AiTool] method on an [AiToolType] class in the loaded assemblies: the set McpPlugin registers.</summary>
        static List<MethodInfo> FindToolMethods()
        {
            var toolMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); }
                })
                .Where(type => type.GetCustomAttribute<AiToolTypeAttribute>() != null)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Where(method => method.GetCustomAttribute<AiToolAttribute>() != null)
                .ToList();

            Assert.IsTrue(toolMethods.Count > 0, "Should find at least one MCP tool method");
            return toolMethods;
        }

        [Test]
        public void AllMcpTools_ShouldHaveAtLeastOneParameter()
        {
            var toolMethods = FindToolMethods();

            var zeroParamTools = new List<string>();
            foreach (var method in toolMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    var attr = method.GetCustomAttribute<AiToolAttribute>()!;
                    zeroParamTools.Add($"  - {attr.Name} ({method.DeclaringType?.Name}.{method.Name})");
                }
            }

            Assert.IsEmpty(zeroParamTools,
                $"The following MCP tools have no input parameters, which breaks some MCP clients (e.g. GitHub Copilot):\n"
                + string.Join("\n", zeroParamTools));
        }

        /// <summary>
        /// Directories of MCP servers (e.g. the Claude Connectors Directory, which lists the hosted ai-game.dev relay of
        /// these tools) reject a tool without a title or without an explicit readOnlyHint / destructiveHint. A hint left
        /// unset is sent as null, so it must be assigned in the attribute, even when the value is false.
        /// </summary>
        [Test]
        public void AllMcpTools_ShouldDeclareTitleAndExplicitAnnotationHints()
        {
            var problems = new List<string>();
            foreach (var method in FindToolMethods())
            {
                var attr = method.GetCustomAttribute<AiToolAttribute>()!;
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(attr.Title)) missing.Add(nameof(AiToolAttribute.Title));
                if (!attr.ReadOnlyHintValue.HasValue) missing.Add(nameof(AiToolAttribute.ReadOnlyHint));
                if (!attr.DestructiveHintValue.HasValue) missing.Add(nameof(AiToolAttribute.DestructiveHint));
                if (!attr.IdempotentHintValue.HasValue) missing.Add(nameof(AiToolAttribute.IdempotentHint));
                if (!attr.OpenWorldHintValue.HasValue) missing.Add(nameof(AiToolAttribute.OpenWorldHint));
                if (attr.ReadOnlyHintValue == true && attr.DestructiveHintValue == true)
                    missing.Add("a consistent pair (ReadOnlyHint and DestructiveHint are both true)");

                if (missing.Count > 0)
                    problems.Add($"  - {attr.Name} ({method.DeclaringType?.Name}.{method.Name}): {string.Join(", ", missing)}");
            }

            Assert.IsEmpty(problems,
                "Every MCP tool must declare a non-empty Title and set ReadOnlyHint, DestructiveHint, IdempotentHint and "
                + "OpenWorldHint explicitly in its [AiTool] attribute:\n"
                + string.Join("\n", problems));
        }
    }
}
