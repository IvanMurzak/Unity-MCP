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
using System.Collections.Generic;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        static Startup()
        {
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
            Server.DownloadServerBinaryIfNeeded();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of Unity-MCP. Please consider the move the project to a folder without spaces.");

            Application.unloading += OnBeforeReload;
            Application.quitting += OnBeforeReload;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;

            // Handle Play mode state changes to ensure reconnection after exiting Play mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Initialize sub-systems
            API.Tool_TestRunner.Init();
        }
        static async void OnBeforeReload()
        {
            Debug.Log("[Unity-MCP] OnBeforeReload triggered - disconnecting");
            var instance = McpPlugin.Instance;
            if (instance == null)
            {
                await McpPlugin.StaticDisposeAsync();
                return; // ignore
            }

            await (instance.RpcRouter?.Disconnect() ?? Task.CompletedTask);
            //await instance.Disconnect();
            //await McpPlugin.StaticDisposeAsync();
            // await Task.WhenAll(
            //     instance.Disconnect(),
            //     McpPlugin.StaticDisposeAsync()
            // );
        }
        static void OnAfterReload()
        {
            Debug.Log($"[Unity-MCP] OnAfterReload triggered - BuildAndStart with openConnection: {!IsCi()}");
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Log Play mode state changes for debugging
            Debug.Log($"[Unity-MCP] Play mode state changed: {state}");
            
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Unity is about to exit Play mode - connection may be lost
                    // The OnBeforeReload will handle disconnection if domain reload occurs
                    Debug.Log("[Unity-MCP] Exiting Play mode - connection may be affected by domain reload");
                    break;
                
                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    Debug.Log($"[Unity-MCP] Entered Edit mode - KeepConnected: {McpPluginUnity.KeepConnected}, IsCi: {IsCi()}");
                    if (McpPluginUnity.KeepConnected && !IsCi())
                    {
                        Debug.Log("[Unity-MCP] Scheduling reconnection after Play mode exit");
                        // Small delay to ensure Unity is fully settled in Edit mode
                        EditorApplication.delayCall += () =>
                        {
                            Debug.Log("[Unity-MCP] Initiating reconnection after Play mode exit");
                            McpPluginUnity.BuildAndStart(openConnection: true);
                        };
                    }
                    break;
                
                case PlayModeStateChange.ExitingEditMode:
                    Debug.Log("[Unity-MCP] Exiting Edit mode to enter Play mode");
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    Debug.Log("[Unity-MCP] Entered Play mode");
                    break;
            }
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// </summary>
        public static bool IsCi()
        {
            var commandLineArgs = ArgsUtils.ParseCommandLineArguments();

            var ci = commandLineArgs.GetValueOrDefault("CI") ?? Environment.GetEnvironmentVariable("CI");
            var gha = commandLineArgs.GetValueOrDefault("GITHUB_ACTIONS") ?? Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var az = commandLineArgs.GetValueOrDefault("TF_BUILD") ?? Environment.GetEnvironmentVariable("TF_BUILD"); // Azure Pipelines

            return string.Equals(ci?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gha?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(az?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
