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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// An editor a CLI session started (<c>AIGD_SESSION_OWNER=cli</c>, e.g. by <c>unity-mcp-cli exec</c>): it quits after
    /// <c>AIGD_SESSION_IDLE_SECONDS</c> (or the older <c>UNITY_MCP_IDLE_TIMEOUT</c>) seconds without tool calls, so a warm
    /// editor does not live forever, and removes the CLI's session lease when it exits. Off unless a variable is set.
    /// </summary>
    public static partial class Startup
    {
        static IdleShutdownTimer? _idleTimer;

        /// <summary>This editor's CLI session lease nonce; null when the user (not a CLI session) started it.</summary>
        internal static string? CliSessionNonce { get; private set; }

        static void StartCliSessionIfRequested()
        {
            // Asset import workers inherit the editor's environment; only the main editor may quit itself.
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            var args = ArgsUtils.ParseCommandLineArguments();
            CliSessionNonce = EnvironmentUtils.ReadSessionNonce(args, Environment.GetEnvironmentVariable);
            if (CliSessionNonce != null)
                Application.quitting += DeleteCliSessionLease;

            var seconds = EnvironmentUtils.ReadIdleTimeoutSeconds(args, Environment.GetEnvironmentVariable);
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

            _logger.LogInformation("Idle shutdown: no tool calls for a while, saving and quitting");
            SaveAndQuit();
        }

        /// <summary>
        /// Saves changed scenes and assets, removes the CLI session lease and exits. Main thread only. Used by the idle
        /// shutdown and by the CLI's close request (<see cref="API.Tool_EditorSession"/>).
        /// </summary>
        internal static void SaveAndQuit()
        {
            EditorApplication.update -= CheckIdleShutdown;
            _idleTimer = null;
            SaveForQuit();
            // EditorApplication.Exit does not promise the quitting event, so remove the lease here too.
            DeleteCliSessionLease();
            EditorApplication.Exit(0);
        }

        /// <summary>Removes the CLI's session lease if it still names this editor (its nonce), so it never outlives us.</summary>
        static void DeleteCliSessionLease()
        {
            if (CliSessionNonce == null)
                return;
            try
            {
                if (CliSessionLease.TryDeleteOwn(CliSessionLease.PathFor(UnityMcpPluginEditor.ProjectRootPath), CliSessionNonce))
                    _logger.LogInformation("Removed the CLI session lease");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not remove the CLI session lease");
            }
        }

        /// <summary>Editor uptime as a timestamp: unlike the wall clock, a system clock change cannot fire or stall the timer.</summary>
        static DateTime MonotonicNow() => DateTime.MinValue.AddSeconds(EditorApplication.timeSinceStartup);

        /// <summary>Saves changed assets and scenes that already have a file; an untitled scene has nowhere to go.</summary>
        static void SaveForQuit()
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
                _logger.LogError(e, "Saving before quit failed, quitting anyway");
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
