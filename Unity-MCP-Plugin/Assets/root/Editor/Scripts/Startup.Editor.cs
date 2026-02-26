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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static partial class Startup
    {
        static void SubscribeOnEditorEvents()
        {
            Application.unloading += OnApplicationUnloading;
            Application.quitting += OnApplicationQuitting;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            // Handle Play mode state changes to ensure reconnection after exiting Play mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        static void OnApplicationUnloading() => TryDisconnectAndCleanup(nameof(OnApplicationUnloading));
        static void OnApplicationQuitting() => TryDisconnectAndCleanup(nameof(OnApplicationQuitting));
        static void OnBeforeAssemblyReload() => TryDisconnectAndCleanup(nameof(OnBeforeAssemblyReload), onlyIfConnected: true);

        /// <summary>
        /// Safely disconnects and cleans up the MCP plugin instance.
        /// Catches exceptions to prevent blocking Unity's shutdown/reload process.
        /// </summary>
        /// <param name="callerName">Name of the calling method for logging.</param>
        /// <param name="onlyIfConnected">If true, only disconnects when in Connected state.
        /// This prevents issues when cancelling in-progress connection attempts during assembly reload.
        /// Note: Log collector disposal always occurs regardless of this flag.</param>
        static void TryDisconnectAndCleanup(string callerName, bool onlyIfConnected = false)
        {
            if (!UnityMcpPluginEditor.HasInstance)
            {
                _logger.LogDebug("{class} {method} triggered: No UnityMcpPluginEditor instance to disconnect",
                    nameof(Startup), callerName);
                return;
            }

            _logger.LogInformation("{method} triggered", callerName);

            var plugin = UnityMcpPluginEditor.Instance;
            if (plugin.HasMcpPluginInstance)
            {
                var connectionState = UnityMcpPluginEditor.ConnectionState.CurrentValue;

                // When onlyIfConnected is true, skip disconnect unless we have an established connection.
                // This prevents hanging when cancelling in-progress connection attempts (Connecting/Reconnecting states).
                if (onlyIfConnected && connectionState != HubConnectionState.Connected)
                {
                    _logger.LogTrace("Skipping {method} - not connected (state: {state})",
                        nameof(plugin.DisconnectImmediate), connectionState);
                }
                else
                {
                    try
                    {
                        plugin.DisconnectImmediate();
                    }
                    catch (System.Exception e)
                    {
                        _logger.LogWarning(e, "{class} {method}: Exception during disconnect (non-blocking): {message}",
                            nameof(Startup), callerName, e.Message);
                    }
                }
            }

            try
            {
                plugin.DisposeLogCollector();
            }
            catch (System.Exception e)
            {
                _logger.LogWarning(e, "{class} {method}: Exception during log collector disposal (non-blocking): {message}",
                    nameof(Startup), callerName, e.Message);
            }

            if (UnityMcpPluginRuntime.HasInstance)
            {
                try
                {
                    UnityMcpPluginRuntime.DisposeInstance();
                }
                catch (System.Exception e)
                {
                    _logger.LogWarning(e, "{class} {method}: Exception during runtime MCP plugin disposal (non-blocking): {message}",
                        nameof(Startup), callerName, e.Message);
                }
            }
        }
        static void OnAfterAssemblyReload()
        {
            var connectionAllowed = EnvironmentUtils.IsCi() == false;

            _logger.LogInformation("{method} triggered - BuildAndStart with connectionAllowed: {connectionAllowed}",
                nameof(OnAfterAssemblyReload), connectionAllowed);

            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            if (connectionAllowed)
                UnityMcpPluginEditor.ConnectIfNeeded();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!UnityMcpPluginEditor.HasInstance)
            {
                _logger.LogDebug("{class} {method} triggered: No UnityMcpPluginEditor instance available. State: {state}",
                    nameof(Startup), nameof(OnPlayModeStateChanged), state);
                return;
            }

            // Log Play mode state changes for debugging
            _logger.LogInformation("Play mode state changed: {state}", state);

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Dispose the runtime plugin instance created by Initialize().Build().
                    // This handles the no-domain-reload case; TryDisconnectAndCleanup covers the domain-reload case.
                    _logger.LogTrace("Exiting Play mode - disposing runtime MCP plugin instance if present");
                    if (UnityMcpPluginRuntime.HasInstance)
                        UnityMcpPluginRuntime.DisposeInstance();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    _logger.LogTrace("Entered Edit mode - KeepConnected: {keepConnected}, IsCi: {isCi}",
                        UnityMcpPluginEditor.KeepConnected, EnvironmentUtils.IsCi());

                    if (EnvironmentUtils.IsCi())
                    {
                        _logger.LogTrace("Skipping reconnection in CI environment");
                        break;
                    }

                    _logger.LogTrace("Scheduling reconnection after Play mode exit");

                    // Small delay to ensure Unity is fully settled in Edit mode
                    EditorApplication.delayCall += () =>
                    {
                        _logger.LogTrace("Initiating delayed reconnection after Play mode exit");

                        UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
                        UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                        UnityMcpPluginEditor.ConnectIfNeeded();
                    };

                    // No delay, immediate reconnection for the case if Unity Editor in background
                    // (has no focus)
                    _logger.LogTrace("Initiating reconnection after Play mode exit");

                    UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
                    UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                    UnityMcpPluginEditor.ConnectIfNeeded();
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    _logger.LogTrace("Exiting Edit mode to enter Play mode");
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    _logger.LogTrace("Entered Play mode");
                    break;
            }
        }
    }
}
