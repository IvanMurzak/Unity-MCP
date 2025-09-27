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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginPromptType]
    public partial class Prompt_DebuggingTesting
    {
        [McpPluginPrompt(Name = "add-debug-visualization", Role = Role.User)]
        [Description("Create debug visualization using Gizmos, Debug.DrawLine, and visual debugging tools.")]
        public string AddDebugVisualization()
        {
            return "Add debug visualization using OnDrawGizmos, OnDrawGizmosSelected, Debug.DrawLine, and Debug.DrawRay for visual debugging of game logic and physics.";
        }

        [McpPluginPrompt(Name = "setup-performance-profiling", Role = Role.User)]
        [Description("Add profiler markers and performance tracking to identify bottlenecks.")]
        public string SetupPerformanceProfiling()
        {
            return "Add Profiler.BeginSample/EndSample markers, setup custom profiler counters, and create performance monitoring tools for identifying bottlenecks.";
        }

        [McpPluginPrompt(Name = "create-test-scene", Role = Role.User)]
        [Description("Generate dedicated test scene for testing specific features and functionality.")]
        public string CreateTestScene()
        {
            return "Create dedicated test scene with testing GameObjects, test data, and isolated environment for testing specific features without affecting main scenes.";
        }

        [McpPluginPrompt(Name = "add-logging-system", Role = Role.User)]
        [Description("Implement structured logging system with different log levels and categories.")]
        public string AddLoggingSystem()
        {
            return "Create structured logging system with log levels (Debug, Info, Warning, Error), categories, conditional compilation, and log file output options.";
        }

        [McpPluginPrompt(Name = "create-unit-tests", Role = Role.User)]
        [Description("Generate unit tests using Unity Test Framework for critical game systems.")]
        public string CreateUnitTests()
        {
            return "Create unit tests using Unity Test Framework, setup test assemblies, write EditMode and PlayMode tests for critical game systems and components.";
        }

        [McpPluginPrompt(Name = "setup-debug-ui", Role = Role.User)]
        [Description("Create debug UI overlay with runtime controls and information display.")]
        public string SetupDebugUI()
        {
            return "Create debug UI overlay with runtime controls, variable inspection, performance metrics display, and debugging tools accessible during gameplay.";
        }

        [McpPluginPrompt(Name = "add-assertion-checks", Role = Role.User)]
        [Description("Add Debug.Assert statements and validation checks throughout the codebase.")]
        public string AddAssertionChecks()
        {
            return "Add Debug.Assert statements, null checks, range validations, and other assertion checks to catch bugs early in development builds.";
        }

        [McpPluginPrompt(Name = "create-automated-tests", Role = Role.User)]
        [Description("Setup automated testing pipeline with continuous integration support.")]
        public string CreateAutomatedTests()
        {
            return "Setup automated testing pipeline using Unity Test Framework, configure test categories, create test reports, and integrate with CI/CD systems.";
        }
    }
}