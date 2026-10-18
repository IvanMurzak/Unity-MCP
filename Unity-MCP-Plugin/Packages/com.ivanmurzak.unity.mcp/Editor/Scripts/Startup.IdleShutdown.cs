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
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Quits an editor started by <c>unity-mcp-cli exec</c> after <c>UNITY_MCP_IDLE_TIMEOUT</c> seconds without tool
    /// calls, so a warm editor does not live forever. Off unless the variable (or the same-named flag) is set.
    /// </summary>
    public static partial class Startup
    {
        static IdleShutdownTimer? _idleTimer;

        static void StartIdleShutdownIfRequested()
        {
            // Asset import workers inherit the editor's environment; only the main editor may quit itself.
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            var seconds = EnvironmentUtils.ReadIdleTimeoutSeconds(ArgsUtils.ParseCommandLineArguments(), Environment.GetEnvironmentVariable);
            if (seconds == null)
                return;

            // A domain reload re-runs this: the timer starts over, which is fine — a reload means the editor was busy.
            _idleTimer = new IdleShutdownTimer(TimeSpan.FromSeconds(seconds.Value), MonotonicNow());
            EditorApplication.update += CheckIdleShutdown;
            _logger.LogInformation("Idle shutdown armed: the editor quits after {seconds}s without tool calls", seconds.Value);
        }

        static void CheckIdleShutdown()
        {
            if (_idleTimer == null)
                return;

            var calls = UnityMcpPluginEditor.HasInstance
                ? UnityMcpPluginEditor.Instance.McpPluginInstance?.ToolCallsCount ?? 0
                : 0;
            // The call counter only moves when a call starts; a running test pass is the long call we can see.
            var busy = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
                || API.Tool_Tests.IsTestRunActive(out _);
            if (!_idleTimer.ShouldQuit(MonotonicNow(), calls, busy))
                return;

            EditorApplication.update -= CheckIdleShutdown;
            _idleTimer = null;
            _logger.LogInformation("Idle shutdown: no tool calls for a while, saving and quitting");
            SaveForIdleShutdown();
            EditorApplication.Exit(0);
        }

        /// <summary>Editor uptime as a timestamp: unlike the wall clock, a system clock change cannot fire or stall the timer.</summary>
        static DateTime MonotonicNow() => DateTime.MinValue.AddSeconds(EditorApplication.timeSinceStartup);

        /// <summary>Saves changed assets and scenes that already have a file; an untitled scene has nowhere to go.</summary>
        static void SaveForIdleShutdown()
        {
            try
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty && !string.IsNullOrEmpty(scene.path))
                        EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Idle shutdown: saving failed, quitting anyway");
            }
        }
    }

    /// <summary>
    /// Idle bookkeeping without Unity APIs: any change of the tool-call counter (a new call, or a reset after a reload)
    /// counts as activity, and so does a busy editor (compiling, importing, play mode, a test run) and a stalled update
    /// loop (the main thread was blocked, typically by a long tool call that only just returned).
    /// </summary>
    public sealed class IdleShutdownTimer
    {
        /// <summary>A gap between two checks longer than this means the main thread was busy.</summary>
        public static readonly TimeSpan StallThreshold = TimeSpan.FromSeconds(1);

        readonly TimeSpan _timeout;
        DateTime _lastActivity;
        DateTime _lastCheck;
        ulong _lastCalls;

        public IdleShutdownTimer(TimeSpan timeout, DateTime now)
        {
            _timeout = timeout;
            _lastActivity = now;
            _lastCheck = now;
        }

        public bool ShouldQuit(DateTime now, ulong toolCalls, bool busy)
        {
            var stalled = now - _lastCheck > StallThreshold;
            _lastCheck = now;
            if (busy || stalled || toolCalls != _lastCalls)
            {
                _lastCalls = toolCalls;
                _lastActivity = now;
                return false;
            }
            return now - _lastActivity >= _timeout;
        }
    }
}
