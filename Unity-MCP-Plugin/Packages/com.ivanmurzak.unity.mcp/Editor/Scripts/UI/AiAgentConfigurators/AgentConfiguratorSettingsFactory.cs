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
        /// Unity editor connection state, auto-detecting the host OS. This is the DEFAULT
        /// (OAuth golden) path: the written config is credential-free, so
        /// <see cref="AgentConfig.AgentConfiguratorSettings.Token"/> is no longer required input
        /// (design 06 / mcp-authorize b6) — the value carried here is only consumed by the advanced
        /// access-token path (<see cref="AgentConfig.HttpCredentialMode.AccessToken"/>).
        /// </summary>
        public static AgentConfig.AgentConfiguratorSettings Create()
            => Build(UnityMcpPluginEditor.Token);

        /// <summary>
        /// Builds a settings snapshot for the ADVANCED "use access token" escape hatch (design 06
        /// Flow C): the same host/port/mode snapshot as <see cref="Create"/> but with
        /// <paramref name="accessToken"/> injected as the bearer token, so a subsequent
        /// <c>GetHttpConfig(settings, credentialMode: HttpCredentialMode.AccessToken)</c> writes the
        /// legacy <c>Authorization: Bearer</c> config for a client that cannot do MCP OAuth. This is
        /// the only place a user-entered PAT enters the snapshot — the golden path never needs one.
        /// </summary>
        public static AgentConfig.AgentConfiguratorSettings CreateWithAccessToken(string? accessToken)
            => Build(accessToken);

        private static AgentConfig.AgentConfiguratorSettings Build(string? token)
        {
            return AgentConfig.AgentConfiguratorSettings.CreateForHost(
                projectRootPath: UnityMcpPluginEditor.ProjectRootPath,
                executableFullPath: McpServerManager.ExecutableFullPath,
                port: UnityMcpPluginEditor.Port,
                timeoutMs: UnityMcpPluginEditor.TimeoutMs,
                host: UnityMcpPluginEditor.Host,
                token: token,
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
