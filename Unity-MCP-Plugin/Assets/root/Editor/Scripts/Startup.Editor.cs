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
        /// This prevents issues when cancelling in-progress connection attempts during assembly reload.</param>
        static void TryDisconnectAndCleanup(string callerName, bool onlyIfConnected = false)
        {
            if (!UnityMcpPlugin.HasInstance)
            {
                _logger.LogDebug("{class} {method} triggered: No UnityMcpPlugin instance to disconnect",
                    nameof(Startup), callerName);
                return;
            }

            _logger.LogInformation("{method} triggered", callerName);

            if (UnityMcpPlugin.Instance.HasMcpPluginInstance)
            {
                var connectionState = UnityMcpPlugin.ConnectionState.CurrentValue;

                // When onlyIfConnected is true, skip disconnect unless we have an established connection.
                // This prevents hanging when cancelling in-progress connection attempts (Connecting/Reconnecting states).
                if (onlyIfConnected && connectionState != HubConnectionState.Connected)
                {
                    _logger.LogTrace("Skipping {method} - not connected (state: {state})",
                        nameof(UnityMcpPlugin.Instance.DisconnectImmediate), connectionState);
                }
                else
                {
                    try
                    {
                        UnityMcpPlugin.Instance.DisconnectImmediate();
                    }
                    catch (System.Exception e)
                    {
                        _logger.LogWarning("{class} {method}: Exception during disconnect (non-blocking): {message}",
                            nameof(Startup), callerName, e.Message);
                    }
                }
            }

            UnityMcpPlugin.Instance.DisposeLogCollector();
        }
        static void OnAfterAssemblyReload()
        {
            var connectionAllowed = EnvironmentUtils.IsCi() == false;

            _logger.LogInformation("{method} triggered - BuildAndStart with connectionAllowed: {connectionAllowed}",
                nameof(OnAfterAssemblyReload), connectionAllowed);

            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            if (connectionAllowed)
                UnityMcpPlugin.ConnectIfNeeded();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!UnityMcpPlugin.HasInstance)
            {
                _logger.LogWarning("{class} {method} triggered: No UnityMcpPlugin instance available. State: {state}",
                    nameof(Startup), nameof(OnPlayModeStateChanged), state);
                return;
            }

            // Log Play mode state changes for debugging
            _logger.LogInformation("Play mode state changed: {state}", state);

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Unity is about to exit Play mode - connection may be lost
                    // The OnBeforeReload will handle disconnection if domain reload occurs
                    _logger.LogTrace("Exiting Play mode - connection may be affected by domain reload");
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    _logger.LogTrace("Entered Edit mode - KeepConnected: {keepConnected}, IsCi: {isCi}",
                        UnityMcpPlugin.KeepConnected, EnvironmentUtils.IsCi());

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

                        UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                        UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                        UnityMcpPlugin.ConnectIfNeeded();
                    };

                    // No delay, immediate reconnection for the case if Unity Editor in background
                    // (has no focus)
                    _logger.LogTrace("Initiating reconnection after Play mode exit");

                    UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                    UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                    UnityMcpPlugin.ConnectIfNeeded();
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
