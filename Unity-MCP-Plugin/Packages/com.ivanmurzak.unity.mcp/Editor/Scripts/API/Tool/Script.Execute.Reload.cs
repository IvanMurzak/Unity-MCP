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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.CodeExec;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// A domain reload while a snippet runs (it imported a script, or one compiled meanwhile) would disconnect the plugin
    /// and drop the snippet's reply. The reload therefore waits while a snippet runs. While it waits Unity does not start
    /// the compile either and keeps <c>EditorApplication.isCompiling</c> true, so a snippet that waits for the compile would
    /// wait for its whole budget: once a compile is pending, a running snippet gets <see cref="ReloadGraceMs"/> more, then
    /// its wait ends and it answers with <see cref="ReloadStopMessage"/>; the compile and reload follow.
    /// </summary>
    public static partial class Tool_Script
    {
        internal const int ReloadGraceMs = 10_000;

        /// <summary>How long the reload stays held after the snippet answered: the reply still has to leave over the connection.</summary>
        internal const int ReplyGraceMs = 1_000;

        internal const string ReloadStopMessage =
            "Scripts must compile and reload, which waits while a snippet runs, so the snippet was stopped to deliver this " +
            "reply (EditorApplication.isCompiling stays true until then: do not wait for it in a snippet). The compile starts " +
            "now; use the new types in a later call.";

        // The running snippets' stop sources, and those already timed to stop; touched on the main thread only.
        static readonly HashSet<CancellationTokenSource> Running = new HashSet<CancellationTokenSource>();
        static readonly HashSet<CancellationTokenSource> Stopping = new HashSet<CancellationTokenSource>();

        /// <returns>The run's result, and whether it was stopped because a reload is waiting.</returns>
        static async Task<(ExecuteResult Result, bool StoppedForReload)> RunHoldingReload(PreparedSnippet prepared)
        {
            var stop = new CancellationTokenSource();
            if (Running.Count == 0) EditorApplication.update += StopForPendingReload;
            Running.Add(stop);
            EditorApplication.LockReloadAssemblies();
            try
            {
                var result = await Runner.Value.RunAsync(prepared, stop.Token);
                return (result, stop.IsCancellationRequested && !result.Success);
            }
            finally
            {
                Running.Remove(stop);
                Stopping.Remove(stop);
                if (Running.Count == 0) EditorApplication.update -= StopForPendingReload;
                stop.Dispose();
                UnlockReloadSoon();
            }
        }

        static void StopForPendingReload()
        {
            if (!EditorApplication.isCompiling) return;
            foreach (var stop in Running)
                if (Stopping.Add(stop)) stop.CancelAfter(ReloadGraceMs); // once: a second CancelAfter would restart the timer
        }

        static async void UnlockReloadSoon()
        {
            try { await Task.Delay(ReplyGraceMs); }
            finally { MainThread.Instance.Run(EditorApplication.UnlockReloadAssemblies); }
        }
    }
}
