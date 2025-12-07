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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static partial class Startup
    {
        public static com.IvanMurzak.Unity.MCP.LogUtils LogUtils { get; private set; }

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
            if (UnityMcpPlugin.HasInstance)
            {
                UnityMcpPlugin.Instance.LogInfo("{method} triggered", typeof(Startup), nameof(OnApplicationUnloading));
                UnityMcpPlugin.Instance.DisconnectImmediate();
            }
            else
            {
                Debug.Log($"{nameof(Startup)} {nameof(OnApplicationUnloading)} triggered: No UnityMcpPlugin instance to disconnect.");
            }
            if (LogUtils != null)
            {
                LogUtils.SaveToFileImmediate();
                LogUtils.Dispose();
            }
        }
        static void OnApplicationQuitting()
        {
            if (UnityMcpPlugin.HasInstance)
            {
                UnityMcpPlugin.Instance.LogInfo("{method} triggered", typeof(Startup), nameof(OnApplicationQuitting));
                UnityMcpPlugin.Instance.DisconnectImmediate();
            }
            else
            {
                Debug.Log($"{nameof(Startup)} {nameof(OnApplicationQuitting)} triggered: No UnityMcpPlugin instance to disconnect.");
            }
            if (LogUtils != null)
            {
                _ = LogUtils.HandleQuit();
            }
        }
        static void OnBeforeAssemblyReload()
        {
            if (UnityMcpPlugin.HasInstance)
            {
                UnityMcpPlugin.Instance.LogInfo("{method} triggered", typeof(Startup), nameof(OnBeforeAssemblyReload));
                UnityMcpPlugin.Instance.DisconnectImmediate();
            }
            else
            {
                Debug.Log($"{nameof(Startup)} {nameof(OnBeforeAssemblyReload)} triggered: No UnityMcpPlugin instance to disconnect.");
            }
            if (LogUtils != null)
            {
                LogUtils.SaveToFileImmediate();
                LogUtils.Dispose();
            }
        }
        static void OnAfterAssemblyReload()
        {
            var connectionAllowed = EnvironmentUtils.IsCi() == false;

            UnityMcpPlugin.Instance.LogInfo($"{nameof(OnAfterAssemblyReload)} triggered - BuildAndStart with {nameof(connectionAllowed)}: {connectionAllowed}",
                typeof(Startup));

            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();

            if (connectionAllowed)
                UnityMcpPlugin.ConnectIfNeeded();

            if (LogUtils != null)
            {
                _ = LogUtils.LoadFromFile();
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!UnityMcpPlugin.HasInstance)
                Debug.LogWarning($"{nameof(Startup)} {nameof(OnPlayModeStateChanged)} triggered: No UnityMcpPlugin instance available. State: {state}.");

            // Log Play mode state changes for debugging
            UnityMcpPlugin.Instance.LogInfo($"Play mode state changed: {state}", typeof(Startup));

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Unity is about to exit Play mode - connection may be lost
                    // The OnBeforeReload will handle disconnection if domain reload occurs
                    UnityMcpPlugin.Instance.LogTrace($"Exiting Play mode - connection may be affected by domain reload", typeof(Startup));
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Unity has returned to Edit mode - ensure connection is re-established
                    // if the configuration expects it to be connected
                    UnityMcpPlugin.Instance.LogTrace($"Entered Edit mode - KeepConnected: {UnityMcpPlugin.KeepConnected}, IsCi: {EnvironmentUtils.IsCi()}.",
                        typeof(Startup));

                    if (EnvironmentUtils.IsCi())
                    {
                        UnityMcpPlugin.Instance.LogTrace($"Skipping reconnection in CI environment.", typeof(Startup));
                        break;
                    }

                    UnityMcpPlugin.Instance.LogTrace($"Scheduling reconnection after Play mode exit.", typeof(Startup));

                    // Small delay to ensure Unity is fully settled in Edit mode
                    EditorApplication.delayCall += () =>
                    {
                        UnityMcpPlugin.Instance.LogTrace($"Initiating delayed reconnection after Play mode exit.", typeof(Startup));

                        UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                        UnityMcpPlugin.ConnectIfNeeded();
                    };

                    // No delay, immediate reconnection for the case if Unity Editor in background
                    // (has no focus)
                    UnityMcpPlugin.Instance.LogTrace($"Initiating reconnection after Play mode exit.", typeof(Startup));

                    UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                    UnityMcpPlugin.ConnectIfNeeded();
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    UnityMcpPlugin.Instance.LogTrace($"Exiting Edit mode to enter Play mode.", typeof(Startup));
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    UnityMcpPlugin.Instance.LogTrace($"Entered Play mode.", typeof(Startup));
                    break;
            }
        }
    }
}
