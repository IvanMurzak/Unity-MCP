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
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
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
            if (MppmUtils.IsMppmClone)
                ConfigureMppmClone();

            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            if (MppmUtils.IsMppmClone)
                DisableWriteToolsForClone();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of AI Game Developer. Please consider the move the project to a folder without spaces.");

            SubscribeOnEditorEvents();

            // Initialize sub-systems
            API.Tool_Tests.Init();
            UpdateChecker.Init();
            PackageUtils.Init();

            if (!MppmUtils.IsMppmClone)
            {
                // Auto-generate skill files for the selected agent if enabled
                var savedAgentId = MainWindowEditor.selectedAiAgentId.Value;
                var agent = AiAgentConfiguratorRegistry.GetByAgentId(savedAgentId);
                if (agent?.SupportsSkills == true && UnityMcpPluginEditor.IsAutoGenerateSkills(agent.AgentId))
                {
                    UnityMcpPluginEditor.SkillsPath = agent.SkillsPath!;
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
                }
            }

            // DEV-ONLY: start the 127.0.0.1 inject/control bridge, gated on UNITY_MCP_DEV_CONTROL=1
            // (process env > project-root .env > default). No-op / never listens in a shipped plugin.
            StartDevControlIfEnabled();
        }

        /// <summary>
        /// MPPM (Multiplayer Play Mode) virtual-player clones never run their own MCP server —
        /// they connect as hub clients to the MAIN editor's server. The clone lives at
        /// <c>&lt;project&gt;/Library/VP/&lt;cloneId&gt;/</c>, so the main project root is four levels up
        /// from the clone's <c>Application.dataPath</c>. Read the main project's saved config to get
        /// the host/port the main editor's server is actually listening on (falling back to the
        /// deterministic port derived from the main project directory) and force the clone into
        /// connect-only mode.
        /// </summary>
        static void ConfigureMppmClone()
        {
            UnityMcpPluginEditor.KeepConnected = true;
            UnityMcpPluginEditor.KeepServerRunning = false;

            var mainProjectDir = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", "..", "..", ".."));
            var mainConfigPath = System.IO.Path.Combine(
                mainProjectDir, "UserSettings", "AI-Game-Developer-Config.json");

            var mainHost = $"http://localhost:{UnityMcpPlugin.GeneratePortFromDirectory(mainProjectDir)}";
            string? mainToken = null;
            if (System.IO.File.Exists(mainConfigPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(mainConfigPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("host", out var hostProp))
                    {
                        var h = hostProp.GetString();
                        if (!string.IsNullOrEmpty(h))
                            mainHost = h;
                    }
                    if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                        mainToken = tokenProp.GetString();
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning("Failed to read main project config at {path}: {error}", mainConfigPath, ex.Message);
                }
            }

            // Custom mode so Host resolves to the local URL below (Cloud mode would redirect the
            // clone to the cloud endpoint instead of the main editor's local server).
            UnityMcpPluginEditor.ConnectionMode = ConnectionMode.Custom;
            UnityMcpPluginEditor.Host = mainHost;
            UnityMcpPluginEditor.LocalHost = mainHost;
            if (mainToken != null)
                UnityMcpPluginEditor.Token = mainToken;
            UnityMcpPluginEditor.AuthOption = AuthOption.none;
        }

        /// <summary>
        /// MPPM clones share the main project's Assets/ (read-only) — mutating tools would either
        /// fail or corrupt shared state, so they are disabled on the clone's hub connection.
        /// </summary>
        static void DisableWriteToolsForClone()
        {
            var tools = UnityMcpPluginEditor.Instance.Tools;
            if (tools == null) return;

            var writeTools = new HashSet<string>
            {
                "script-update-or-create", "script-delete", "script-execute",
                "assets-create-folder", "assets-delete", "assets-move", "assets-copy", "assets-modify",
                "assets-prefab-create", "assets-prefab-save", "assets-prefab-close", "assets-prefab-open",
                "assets-prefab-instantiate", "assets-material-create", "assets-refresh",
                "scene-create", "scene-save",
                "package-add", "package-remove",
                "gameobject-create", "gameobject-destroy", "gameobject-duplicate",
                "gameobject-modify", "gameobject-set-parent",
                "gameobject-component-add", "gameobject-component-destroy", "gameobject-component-modify",
                "object-modify",
            };

            foreach (var toolName in writeTools)
                tools.SetToolEnabled(toolName, false);
        }
    }
}
