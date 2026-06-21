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
using com.IvanMurzak.Unity.MCP.Editor.DevControl;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static partial class Startup
    {
        /// <summary>
        /// The DEV-ONLY inject/control bridge instance, started gated on <c>UNITY_MCP_DEV_CONTROL=1</c>
        /// (process env &gt; project-root <c>.env</c> &gt; default). Null when the flag is off (a shipped
        /// plugin never listens) or when a previous instance was torn down for domain reload / quit.
        /// </summary>
        static DevControlServer? _devControlServer;

        /// <summary>
        /// Start the DEV-ONLY inject/control bridge when the enable flag resolves to <c>"1"</c>. The port
        /// comes from <c>UNITY_MCP_DEV_CONTROL_PORT</c> (default <see cref="DevControlServer.DefaultPort"/>)
        /// — an unset or unparseable value falls back to the default. No-op (and never listens) when the
        /// flag is not exactly <c>"1"</c>. Defensively wrapped so a bridge failure cannot take down editor
        /// boot. Idempotent: a second call while a server is live is a no-op.
        /// </summary>
        static void StartDevControlIfEnabled()
        {
            if (_devControlServer != null)
                return;

            var projectRoot = UnityMcpPluginEditor.ProjectRootPath;
            if (!DevControlEnv.IsEnabled(projectRoot))
                return;

            var port = DevControlEnv.ResolvePort(projectRoot, DevControlServer.DefaultPort);
            try
            {
                _devControlServer = new DevControlServer(port);
                _devControlServer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[dev-control] failed to start: {message}", ex.Message);
                _devControlServer = null;
            }
        }

        /// <summary>
        /// Stop + dispose the dev-control bridge so a domain reload / editor quit does not leak the bound
        /// port (the HttpListener accept thread holds the socket otherwise). Safe to call when no server
        /// is running. Wired into <see cref="TryDisconnectAndCleanup"/> (beforeAssemblyReload + quit).
        /// </summary>
        static void StopDevControl()
        {
            if (_devControlServer == null)
                return;
            try { _devControlServer.Dispose(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[dev-control] exception during dispose (non-blocking): {message}", ex.Message);
            }
            finally { _devControlServer = null; }
        }
    }
}
