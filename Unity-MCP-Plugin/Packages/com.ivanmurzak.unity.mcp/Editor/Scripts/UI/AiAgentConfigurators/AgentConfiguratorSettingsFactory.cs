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
using AgentConfig = com.IvanMurzak.McpPlugin.AgentConfig;
using UnityConnectionMode = com.IvanMurzak.Unity.MCP.ConnectionMode;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Bridges Unity's editor/connection state into the engine-agnostic
    /// <see cref="AgentConfig.AgentConfiguratorSettings"/> consumed by the shared
    /// <c>com.IvanMurzak.McpPlugin.AgentConfig</c> module. This is the single place that maps
    /// Unity's statics (<see cref="UnityMcpPluginEditor.Port"/>, <c>.Host</c>, <c>.Token</c>, …,
    /// and <see cref="McpServerManager.ExecutableFullPath"/>) onto the shared settings record.
    /// The shared library detects the host OS at runtime, so per-OS config-file paths work on
    /// Win/Mac/Linux without a compile-time branch here.
    /// </summary>
    internal static class AgentConfiguratorSettingsFactory
    {
        /// <summary>
        /// Builds an <see cref="AgentConfig.AgentConfiguratorSettings"/> snapshot from the current
        /// Unity editor connection state, auto-detecting the host OS.
        /// </summary>
        public static AgentConfig.AgentConfiguratorSettings Create()
        {
            return AgentConfig.AgentConfiguratorSettings.CreateForHost(
                projectRootPath: UnityMcpPluginEditor.ProjectRootPath,
                executableFullPath: McpServerManager.ExecutableFullPath,
                port: UnityMcpPluginEditor.Port,
                timeoutMs: UnityMcpPluginEditor.TimeoutMs,
                host: UnityMcpPluginEditor.Host,
                token: UnityMcpPluginEditor.Token,
                connectionMode: MapConnectionMode(UnityMcpPluginEditor.ConnectionMode),
                authOption: UnityMcpPluginEditor.AuthOption,
                // Pass Unity's authoritative server identity explicitly so the shared module's
                // Docker command (image tag = serverVersion) tracks McpServerManager's pin instead
                // of silently coinciding with the shared library's own defaults — which would drift
                // the moment ServerVersion is bumped here. The Docker image base mirrors the literal
                // McpServerManager.DockerSetupRunCommand() builds ("aigamedeveloper/mcp-server"),
                // which Unity has no constant for.
                serverExecutableName: McpServerManager.ExecutableName,
                serverVersion: McpServerManager.ServerVersion,
                dockerImage: "aigamedeveloper/mcp-server");
        }

        /// <summary>
        /// Maps Unity's <see cref="UnityConnectionMode"/> (<c>Custom</c> = local server / <c>Cloud</c>)
        /// onto the shared <see cref="AgentConfig.ConnectionMode"/> (<c>Local</c> / <c>Cloud</c>).
        /// Only <c>Cloud</c> changes auth behaviour (cloud always requires it); everything else is local.
        /// </summary>
        public static AgentConfig.ConnectionMode MapConnectionMode(UnityConnectionMode mode)
            => mode == UnityConnectionMode.Cloud
                ? AgentConfig.ConnectionMode.Cloud
                : AgentConfig.ConnectionMode.Local;
    }
}
