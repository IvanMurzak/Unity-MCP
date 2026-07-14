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
using com.IvanMurzak.Unity.MCP.Editor.Services;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPluginEditor
    {
        /// <summary>
        /// Editor-only test accessor for the underlying <see cref="UnityConnectionConfig"/>.
        /// The field is <c>protected</c> on <see cref="UnityMcpPlugin"/>; this accessor exposes
        /// it to the Editor test assembly (via <c>InternalsVisibleTo</c>) so tests can verify
        /// state that is written through the plugin's build path.
        /// </summary>
        internal UnityConnectionConfig ConnectionConfigForTests => unityConnectionConfig;

        public UnityMcpPluginEditor BuildMcpPluginIfNeeded()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var loggerProvider = BuildLoggerProvider();

            // Feed the host project root into the connection config BEFORE the plugin is built.
            // McpPlugin's ctor calls GenerateSkillFilesIfNeeded() internally; it needs an anchor
            // for the relative SkillsPath. Without this, the McpPlugin ctor logs an
            // InvalidOperationException every Editor open / domain reload — see
            // https://github.com/IvanMurzak/Unity-MCP/issues/766 (host-side cascade of upstream
            // https://github.com/IvanMurzak/MCP-Plugin-dotnet/issues/107 /
            // https://github.com/IvanMurzak/MCP-Plugin-dotnet/pull/108).
            unityConnectionConfig.ProjectRootPath = ProjectRootPath;
            _logger.LogTrace("Seeded ConnectionConfig.ProjectRootPath={path}", ProjectRootPath);

            // Attach the instance-metadata handshake payload (mcp-authorize design 04/06, PR 3) so the hub
            // connection identifies THIS editor session to the server's account+instance router. The payload
            // is non-secret (engine/project/machine names + per-session instance id + project-path hash); the
            // token always travels separately in the Authorization header. Inert on a none-mode/local server
            // that ignores it. Re-applied on every (re)build; the session instance id is stable across reloads.
            ProjectInstanceService.AttachInstanceMetadata(unityConnectionConfig);

            var built = _plugin.BuildOnce(() => BuildMcpPlugin(
                version: BuildVersion(),
                reflector: CreateDefaultReflector(),
                loggerProvider: loggerProvider,
                configure: builder =>
                {
                    builder.WithSkillFileGenerator(new UnitySkillFileGenerator(loggerProvider?.CreateLogger(nameof(UnitySkillFileGenerator))));
                }
            ));
            stopwatch.Stop();

            if (built == null)
                return this; // already built, nothing to wire up

            _logger.LogDebug("MCP Plugin built in {elapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            SetCurrentPlugin(built);

            // Zero-button auto-adopt (mcp-authorize design 06 / D12): point the Cloud credential provider at
            // the shared machine store and attach the on-401 refresh→reconnect coordinator. Both are inert
            // when the machine store holds no credential, so this does not change the anonymous/local path.
            AccountCredentialService.AttachTo(built);

            ApplyConfigToMcpPlugin(built);

            return this;
        }

        public void DisposeMcpPluginInstance()
        {
            var oldInstance = _plugin.TakeInstance();
            if (oldInstance == null)
                return;

            SetCurrentPlugin(null);

            // Dispose on a background thread to avoid blocking Unity's main thread.
            // The dispose path calls ConnectionManager.DisconnectImmediate() which
            // internally blocks on a semaphore (_gate) held by a pending Connect()
            // task whose continuation is queued on the main thread SynchronizationContext —
            // a deadlock if we block the main thread here.
            _ = System.Threading.Tasks.Task.Run(() => oldInstance.Dispose());
        }
    }
}
