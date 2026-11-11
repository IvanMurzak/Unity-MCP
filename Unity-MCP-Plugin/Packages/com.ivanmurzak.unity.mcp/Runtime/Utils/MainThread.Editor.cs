/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MainThreadInstaller
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            if (com.IvanMurzak.Unity.MCP.Runtime.Utils.McpDisable.IsDisabled)
                return;

            // Init runs on the main thread (Startup's static constructor, then [InitializeOnLoadMethod]). Seed the
            // dispatcher's id here: Startup installs UnityMainThread before MainThreadDispatcher.EditorInitialize
            // runs, so the id must not depend on which thread first touches MainThreadDispatcher.
            MainThreadDispatcher.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            UnityMainThread.InstallPump();
            MainThread.Instance = new UnityMainThread();
        }
    }
    public class UnityMainThread : MainThread
    {
        public override bool IsMainThread => MainThreadDispatcher.IsMainThread;

        public override Task RunAsync(Task task)
            => MainThreadDispatcher.IsMainThread ? task : Dispatch(() => { task.Wait(); return true; });

        public override Task<T> RunAsync<T>(Task<T> task)
            => MainThreadDispatcher.IsMainThread ? task : Dispatch(() => task.Result);

        public override Task<T> RunAsync<T>(Func<T> func)
            => MainThreadDispatcher.IsMainThread ? Task.FromResult(func()) : Dispatch(func);

        public override Task RunAsync(Action action)
        {
            if (MainThreadDispatcher.IsMainThread)
            {
                action();
                return Task.CompletedTask;
            }
            return Dispatch(() => { action(); return true; });
        }

        // Work posted from background threads, drained on the main thread by Pump.
        //
        // Do NOT subscribe a per-call callback to EditorApplication.update from the calling
        // thread instead: `update` is a plain public static delegate FIELD (not an event), so
        // `update += x` is a non-atomic read-combine-write. A background `+=` racing with the
        // main thread's `-=` of the previous hop (or with another background `+=`) loses one
        // write: either the new callback is dropped (the caller then blocks forever in
        // MainThread.Run) or a finished callback is resurrected and runs twice.
        static readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();

        /// <summary>
        /// Subscribes the queue pump to <see cref="EditorApplication.update"/>. Must be called on
        /// the main thread (it is, from <see cref="MainThreadInstaller.Init"/>), so the delegate
        /// field is only ever mutated by the thread that owns it.
        /// </summary>
        internal static void InstallPump()
        {
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;

            _mainNativeThreadId = EditorModalWindow.CurrentNativeThreadId();
            // The watchdog's timer must not outlive this domain's code.
            AssemblyReloadEvents.beforeAssemblyReload -= StopWatchdog;
            AssemblyReloadEvents.beforeAssemblyReload += StopWatchdog;
        }

        static void Pump()
        {
            // Drain only what was queued before this tick: work posted while draining runs on the
            // next tick, as it did when each call subscribed its own update callback.
            for (var count = _pending.Count; count > 0 && _pending.TryDequeue(out var action); count--)
                action();
        }

        /// <summary>
        /// Runs <paramref name="action"/> on the main thread on a later tick, without waiting for it. Unlike
        /// <see cref="MainThread.Run(Action)"/> it is never failed by the modal-window watchdog, so work that must
        /// happen (state cleanup) still runs once a modal window is answered.
        /// </summary>
        public static void Post(Action action) => _pending.Enqueue(action);

        static Task<T> Dispatch<T>(Func<T> body)
        {
            var item = new WorkItem<T>(body);
            _waiting.TryAdd(item, 0);
            _pending.Enqueue(item.Execute);
            EnsureWatchdog();
            return item.Task;
        }

        #region Modal-window watchdog

        // A modal window (a Win32 dialog) runs its own message loop on the main thread, so EditorApplication.update,
        // and with it Pump, stops until a person answers it: every MCP call then waited silently until the client
        // timed out (minutes). The watchdog fails the work the main thread has not finished after GraceSeconds
        // while such a window is open, with an error that names the window, so the agent learns why at once.
        // Only that case: work held by a busy (not blocked) main thread keeps waiting, as before.

        /// <summary>How long queued work waits before the watchdog may fail it.</summary>
        public const double GraceSeconds = 2.0;
        const int WatchdogPeriodMs = 1000;

        static readonly ConcurrentDictionary<IWorkItem, byte> _waiting = new ConcurrentDictionary<IWorkItem, byte>();
        static readonly object _watchdogLock = new object();
        static System.Threading.Timer? _watchdog;
        static uint _mainNativeThreadId;

        /// <summary>The modal window that blocks the main thread right now, if any (Windows; null elsewhere).</summary>
        public static Func<EditorModalWindow?> FindBlockingWindow { get; set; } = () => EditorModalWindow.Find(_mainNativeThreadId);

        static void EnsureWatchdog()
        {
            if (Volatile.Read(ref _watchdog) != null)
                return;
            lock (_watchdogLock)
                _watchdog ??= new System.Threading.Timer(_ => CheckWaiting(), null, WatchdogPeriodMs, WatchdogPeriodMs);
        }

        static void StopWatchdog()
        {
            lock (_watchdogLock)
            {
                _watchdog?.Dispose();
                _watchdog = null;
            }
        }

        /// <summary>Fails the waiting work older than <see cref="GraceSeconds"/> when a modal window blocks the main thread.</summary>
        internal static void CheckWaiting()
        {
            try
            {
                var stale = _waiting.Keys.Where(item => item.Age.TotalSeconds >= GraceSeconds).ToList();
                if (stale.Count == 0)
                    return;
                var window = FindBlockingWindow();
                if (window == null)
                    return;
                foreach (var item in stale)
                    item.FailBlocked(window);
            }
            catch (Exception)
            {
                // A watchdog failure must never take the timer thread down; the work keeps waiting as before.
            }
        }

        interface IWorkItem
        {
            TimeSpan Age { get; }
            void FailBlocked(EditorModalWindow window);
        }

        sealed class WorkItem<T> : IWorkItem
        {
            const int Queued = 0, Running = 1, Abandoned = 2;

            readonly Func<T> _body;
            readonly TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>();
            readonly Stopwatch _age = Stopwatch.StartNew();
            int _state = Queued;

            public WorkItem(Func<T> body) => _body = body;

            public Task<T> Task => _tcs.Task;
            public TimeSpan Age => _age.Elapsed;

            public void Execute()
            {
                // Failed before it started: the caller was told it did not run, so it must not run now.
                if (Interlocked.CompareExchange(ref _state, Running, Queued) != Queued)
                    return;
                try { _tcs.TrySetResult(_body()); }
                catch (Exception ex) { _tcs.TrySetException(ex); }
                finally { _waiting.TryRemove(this, out _); }
            }

            public void FailBlocked(EditorModalWindow window)
            {
                var notStarted = Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued;
                _waiting.TryRemove(this, out _);
                _tcs.TrySetException(new ToolUnavailableException(window.BlockedCallMessage(started: !notStarted)));
            }
        }

        #endregion
    }
}
#endif
