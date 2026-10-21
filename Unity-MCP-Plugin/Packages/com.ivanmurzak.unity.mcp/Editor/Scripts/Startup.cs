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
            // Install the Unity main-thread implementation BEFORE building the plugin. Building reflects over
            // every [AiSkill] member, and Skill_Rql.Markdown runs its renderer through MainThread.Instance.Run.
            // On Unity 7000.0.0a7 (CoreCLR) this static constructor runs before MainThreadInstaller.Init's
            // [InitializeOnLoadMethod], so MainThread.Instance was still the default implementation; Run()
            // then dispatched the work to the main thread, which is blocked right here in the constructor,
            // and the editor hung forever in "Running managed callbacks... Initializing InitializeOnLoad
            // Types". With the Unity implementation installed, Run() on the main thread executes inline.
            // Idempotent: InstallPump re-subscribes safely and Init runs again from its own attribute.
            com.IvanMurzak.Unity.MCP.Runtime.Utils.MainThreadInstaller.Init();

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
            StartCliSessionIfRequested();
        }
    }
}
