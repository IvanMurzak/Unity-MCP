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
using com.IvanMurzak.McpPlugin.Common.Utils;
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
        public const string EnvIdleTimeout = "UNITY_MCP_IDLE_TIMEOUT";

        static IdleShutdownTimer? _idleTimer;

        static void StartIdleShutdownIfRequested()
        {
            var seconds = ReadIdleTimeoutSeconds(ArgsUtils.ParseCommandLineArguments(), Environment.GetEnvironmentVariable);
            if (seconds == null)
                return;

            // A domain reload re-runs this: the timer starts over, which is fine — a reload means the editor was busy.
            _idleTimer = new IdleShutdownTimer(TimeSpan.FromSeconds(seconds.Value), DateTime.UtcNow);
            EditorApplication.update += CheckIdleShutdown;
            _logger.LogInformation("Idle shutdown armed: the editor quits after {seconds}s without tool calls", seconds.Value);
        }

        /// <summary>Args beat env. Null when unset or not a positive whole number.</summary>
        public static int? ReadIdleTimeoutSeconds(IReadOnlyDictionary<string, string> args, Func<string, string?> env)
        {
            var raw = args.TryGetValue(EnvIdleTimeout, out var fromArgs) && !string.IsNullOrWhiteSpace(fromArgs)
                ? fromArgs
                : env(EnvIdleTimeout);
            return int.TryParse(raw?.Trim().Trim('"'), out var seconds) && seconds > 0 ? seconds : (int?)null;
        }

        static void CheckIdleShutdown()
        {
            if (_idleTimer == null)
                return;

            var calls = UnityMcpPluginEditor.HasInstance
                ? UnityMcpPluginEditor.Instance.McpPluginInstance?.ToolCallsCount ?? 0
                : 0;
            var busy = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
            if (!_idleTimer.ShouldQuit(DateTime.UtcNow, calls, busy))
                return;

            EditorApplication.update -= CheckIdleShutdown;
            _idleTimer = null;
            _logger.LogInformation("Idle shutdown: no tool calls for a while, saving and quitting");
            SaveForIdleShutdown();
            EditorApplication.Exit(0);
        }

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
    /// counts as activity, and so does a busy editor (compiling, importing, play mode).
    /// </summary>
    public sealed class IdleShutdownTimer
    {
        readonly TimeSpan _timeout;
        DateTime _lastActivity;
        ulong _lastCalls;

        public IdleShutdownTimer(TimeSpan timeout, DateTime now)
        {
            _timeout = timeout;
            _lastActivity = now;
        }

        public bool ShouldQuit(DateTime now, ulong toolCalls, bool busy)
        {
            if (busy || toolCalls != _lastCalls)
            {
                _lastCalls = toolCalls;
                _lastActivity = now;
                return false;
            }
            return now - _lastActivity >= _timeout;
        }
    }
}
