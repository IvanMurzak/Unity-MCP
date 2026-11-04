/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.CodeExec;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// A domain reload while a snippet runs (it imported a script, or one compiled meanwhile) would disconnect the plugin
    /// and drop the snippet's reply. The reload therefore waits while a snippet runs. While it waits Unity does not start
    /// the compile either and keeps <c>EditorApplication.isCompiling</c> true, so a snippet that waits for the compile would
    /// wait for its whole budget: once a compile has been pending for <see cref="ReloadGraceMs"/>, a running snippet's wait
    /// ends and it answers with <see cref="ReloadStopMessage"/>; the compile and reload follow.
    /// </summary>
    public static partial class Tool_Script
    {
        internal const int ReloadGraceMs = 10_000;

        /// <summary>How long the reload stays held after the snippet answered: the reply still has to leave over the connection.</summary>
        internal const int ReplyGraceMs = 1_000;

        internal const string ReloadPendingMessage =
            "Scripts are compiling and reload next, so this snippet was not run (a running snippet holds the reload back). " +
            "Retry in a few seconds: rql-query `editor | select isCompiling` reads false once the reload is done.";

        /// <summary>
        /// The refusal of a call deferred for a pending reload: nothing of the call has run (only its code was compiled,
        /// off the main thread), so it is not-executed (HTTP 503) and a server with the call guard sends it again once
        /// the reloaded plugin is back. Never use it once the snippet started (see <see cref="ReloadStopMessage"/>).
        /// </summary>
        internal static ToolUnavailableException ReloadPending() => new ToolUnavailableException(ReloadPendingMessage, notExecuted: true);

        /// <summary>How long calls are deferred for one pending compile; after it they run again (another lock may hold it).</summary>
        internal static readonly TimeSpan DeferLimit = TimeSpan.FromSeconds(60);

        // When a compile was first seen waiting while this plugin held the reload (null: not seen). Main thread only.
        static DateTime? _heldCompileSince;

        /// <summary>
        /// Whether a new call must not run: a compile is waiting for a reload that our own lock held back. A snippet started
        /// now would lock the reload again, and a client that keeps calling (polling for its new type) would hold it forever,
        /// as each call locks until 1 s after its reply. Compiles we never held (another lock, Play Mode's
        /// recompile-after-playing) do not defer anything, and the deferral ends after <see cref="DeferLimit"/>.
        /// </summary>
        internal static bool DeferForReload(bool compiling, bool playing, bool reloadLocked, DateTime now)
        {
            if (playing || !compiling)
            {
                _heldCompileSince = null;
                return false;
            }
            if (reloadLocked && _heldCompileSince == null) _heldCompileSince = now;
            return _heldCompileSince is DateTime since && now - since < DeferLimit;
        }

        static bool DeferForReload() =>
            DeferForReload(EditorApplication.isCompiling, EditorApplication.isPlaying, _reloadLocks > 0, DateTime.UtcNow);

        internal const string ReloadStopMessage =
            "Scripts must compile and reload, which waits while a snippet runs, so the snippet was stopped to deliver this " +
            "reply (EditorApplication.isCompiling stays true until then: do not wait for it in a snippet). The compile starts " +
            "now; use the new types in a later call.";

        // Running snippets → when a pending compile stops them (null: none pending). Main thread only.
        static readonly Dictionary<CancellationTokenSource, DateTime?> Running = new Dictionary<CancellationTokenSource, DateTime?>();

        // Reload locks this domain holds (main thread only); released before any reload, so a lock never outlives its domain.
        static int _reloadLocks;

        /// <returns>The run's result, and whether it was stopped because a reload is waiting.</returns>
        static async Task<(ExecuteResult Result, bool StoppedForReload)> RunHoldingReload(PreparedSnippet prepared)
        {
            var stop = new CancellationTokenSource();
            if (Running.Count == 0) EditorApplication.update += StopForPendingReload;
            Running.Add(stop, null);
            LockReload();
            try
            {
                var result = await Runner.Value.RunAsync(prepared, stop.Token);
                // Code-Exec reports a canceled wait as Timeout; the snippet's own failure keeps its error.
                return (result, stop.IsCancellationRequested && result.ErrorKind == ErrorKind.Timeout);
            }
            finally
            {
                Running.Remove(stop);
                if (Running.Count == 0) EditorApplication.update -= StopForPendingReload;
                stop.Dispose();
                UnlockReloadSoon();
            }
        }

        static void StopForPendingReload()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in Running.ToList())
            {
                if (!EditorApplication.isCompiling) Running[entry.Key] = null; // the compile ended without a reload
                else if (entry.Value == null) Running[entry.Key] = now.AddMilliseconds(ReloadGraceMs);
                else if (now >= entry.Value) entry.Key.Cancel();
            }
        }

        static void LockReload()
        {
            if (_reloadLocks++ == 0) AssemblyReloadEvents.beforeAssemblyReload += ReleaseReloadLocks;
            EditorApplication.LockReloadAssemblies();
        }

        static async void UnlockReloadSoon()
        {
            try { await Task.Delay(ReplyGraceMs); }
            finally { MainThread.Instance.Run(UnlockReload); }
        }

        static void UnlockReload()
        {
            if (_reloadLocks == 0) return; // already released by ReleaseReloadLocks
            DeferForReload(); // a compile waiting now was held by this lock: later calls defer until it reloads
            if (--_reloadLocks == 0) AssemblyReloadEvents.beforeAssemblyReload -= ReleaseReloadLocks;
            EditorApplication.UnlockReloadAssemblies();
        }

        // A reload that happens anyway (e.g. entering Play Mode right after a snippet) drops the pending UnlockReloadSoon:
        // Unity's lock count outlives the domain, so without this every later script reload would stay blocked.
        static void ReleaseReloadLocks()
        {
            while (_reloadLocks > 0) UnlockReload();
        }
    }
}
