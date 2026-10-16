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
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MainThreadInstaller
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
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
        }

        static void Pump()
        {
            // Drain only what was queued before this tick: work posted while draining runs on the
            // next tick, as it did when each call subscribed its own update callback.
            for (var count = _pending.Count; count > 0 && _pending.TryDequeue(out var action); count--)
                action();
        }

        static Task<T> Dispatch<T>(Func<T> body)
        {
            var tcs = new TaskCompletionSource<T>();
            _pending.Enqueue(() =>
            {
                try { tcs.SetResult(body()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }
    }
}
#endif
