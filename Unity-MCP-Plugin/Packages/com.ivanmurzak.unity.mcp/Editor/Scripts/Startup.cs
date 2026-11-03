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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
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
            // Asset import workers load these editor assemblies too; they must not build, connect or quit: a no-auth
            // server keeps one plugin connection, so a worker's replaced the editor's (#1050). Not isBatchMode:
            // headless CLI/CI editors still need MCP.
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            // Install the Unity main-thread implementation BEFORE building the plugin. Building reflects over
            // every [AiSkill] member, and Skill_Rql.Markdown runs its renderer through MainThread.Instance.Run.
            // [InitializeOnLoad] static constructors always run before [InitializeOnLoadMethod], so here
            // MainThread.Instance is still ReflectorNet's default, which treats only ManagedThreadId == 1 as the
            // main thread. That holds on Mono, but on the Unity 7 CoreCLR editor on Windows the main thread's id
            // is 2: Run() then dispatched the work to the main thread, which is blocked right here in the
            // constructor, and the editor hung forever in "Initializing InitializeOnLoad Types". With the Unity
            // implementation installed, Run() on the main thread executes inline.
            // Idempotent: InstallPump re-subscribes safely and Init runs again from its own attribute.
            MainThreadInstaller.Init();

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
