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
using com.IvanMurzak.Unity.MCP.Utils;
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
        static void OnApplicationUnloading()
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Debug))
                Debug.Log($"{DebugName} OnApplicationUnloading triggered");
            Disconnect();
        }
        static void OnApplicationQuitting()
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Debug))
                Debug.Log($"{DebugName} OnApplicationQuitting triggered");
            Disconnect();
        }
        static void OnBeforeAssemblyReload()
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Debug))
                Debug.Log($"{DebugName} OnBeforeAssemblyReload triggered");
            Disconnect();
        }
        static void OnAfterAssemblyReload()
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Debug))
                Debug.Log($"{DebugName} OnAfterReload triggered - BuildAndStart with openConnection: {!EnvironmentUtils.IsCi()}");
            McpPluginUnity.BuildAndStart(openConnectionIfNeeded: !EnvironmentUtils.IsCi());
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Log Play mode state changes for debugging
            if (McpPluginUnity.IsLogActive(LogLevel.Debug))
                Debug.Log($"{DebugName} Play mode state changed: {state}");

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Unity is about to exit Play mode - connection may be lost
                    // The OnBeforeReload will handle disconnection if domain reload occurs
                    if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                        Debug.Log($"{DebugName} Exiting Play mode - connection may be affected by domain reload");
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                        Debug.Log($"{DebugName} Entered Edit mode - KeepConnected: {McpPluginUnity.KeepConnected}, IsCi: {EnvironmentUtils.IsCi()}");

                    if (McpPluginUnity.KeepConnected && !EnvironmentUtils.IsCi())
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                            Debug.Log($"{DebugName} Scheduling reconnection after Play mode exit");

                        // Small delay to ensure Unity is fully settled in Edit mode
                        EditorApplication.delayCall += () =>
                        {
                            if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                                Debug.Log($"{DebugName} Initiating delayed reconnection after Play mode exit");
                            McpPluginUnity.BuildAndStart();
                        };
                        if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                            Debug.Log($"{DebugName} Initiating reconnection after Play mode exit");
                        McpPluginUnity.BuildAndStart();
                    }
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                        Debug.Log($"{DebugName} Exiting Edit mode to enter Play mode");
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    if (McpPluginUnity.IsLogActive(LogLevel.Trace))
                        Debug.Log($"{DebugName} Entered Play mode");
                    break;
            }
        }
    }
}
