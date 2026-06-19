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
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;
using UnityEngine;
using AiAgentConfiguratorRegistry = com.IvanMurzak.McpPlugin.AgentConfig.AiAgentConfiguratorRegistry;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(Startup));

        static Startup()
        {
            // Skip auto-start in headless batchmode (e.g. CI / `-batchmode -runTests`). Otherwise the
            // plugin connects to the shared local MCP server — forcibly disconnecting an interactive
            // Editor running on the same machine — and, with no token in a clean CI checkout, logs an
            // authorization error from a background connection callback that Unity's Test Framework
            // converts into a test failure (it cannot be suppressed via LogAssert, being off the test's
            // main-thread scope). Opt in for intentional headless use by setting UNITY_MCP_BATCHMODE=1.
            if (Application.isBatchMode && System.Environment.GetEnvironmentVariable("UNITY_MCP_BATCHMODE") != "1")
                return;

            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of AI Game Developer. Please consider the move the project to a folder without spaces.");

            SubscribeOnEditorEvents();

            // Initialize sub-systems
            API.Tool_Tests.Init();
            UpdateChecker.Init();
            PackageUtils.Init();

            // Auto-generate skill files for the selected agent if enabled
            var savedAgentId = MainWindowEditor.selectedAiAgentId.Value;
            var agent = AiAgentConfiguratorRegistry.GetByAgentId(savedAgentId);
            if (agent?.SupportsSkills == true && UnityMcpPluginEditor.IsAutoGenerateSkills(agent.AgentId))
            {
                UnityMcpPluginEditor.SkillsPath = agent.SkillsPath!;
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            }

            // DEV-ONLY: start the 127.0.0.1 inject/control bridge, gated on UNITY_MCP_DEV_CONTROL=1
            // (process env > project-root .env > default). No-op / never listens in a shipped plugin.
            StartDevControlIfEnabled();
        }
    }
}
