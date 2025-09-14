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
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static partial class Startup
    {
        static async void OnBeforeReload()
        {
            if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                Debug.Log($"{DebugName} OnBeforeReload triggered - disconnecting");
            var instance = McpPlugin.Instance;
            if (instance == null)
            {
                await McpPlugin.StaticDisposeAsync();
                return; // ignore
            }

            await (instance.RpcRouter?.Disconnect() ?? Task.CompletedTask);
        }
        static void OnAfterReload()
        {
            if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                Debug.Log($"{DebugName} OnAfterReload triggered - BuildAndStart with openConnection: {!IsCi()}");
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Log Play mode state changes for debugging
            if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                Debug.Log($"{DebugName} Play mode state changed: {state}");

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Unity is about to exit Play mode - connection may be lost
                    // The OnBeforeReload will handle disconnection if domain reload occurs
                    if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                        Debug.Log($"{DebugName} Exiting Play mode - connection may be affected by domain reload");
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                        Debug.Log($"{DebugName} Entered Edit mode - KeepConnected: {McpPluginUnity.KeepConnected}, IsCi: {IsCi()}");
                    if (McpPluginUnity.KeepConnected && !IsCi())
                    {
                        if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                            Debug.Log($"{DebugName} Scheduling reconnection after Play mode exit");
                        // Small delay to ensure Unity is fully settled in Edit mode
                        EditorApplication.delayCall += () =>
                        {
                            if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                                Debug.Log($"{DebugName} Initiating reconnection after Play mode exit");
                            McpPluginUnity.BuildAndStart(openConnection: true);
                        };
                    }
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                        Debug.Log($"{DebugName} Exiting Edit mode to enter Play mode");
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    if (McpPluginUnity.IsLogActive(MCP.Utils.LogLevel.Trace))
                        Debug.Log($"{DebugName} Entered Play mode");
                    break;
            }
        }
    }
}
