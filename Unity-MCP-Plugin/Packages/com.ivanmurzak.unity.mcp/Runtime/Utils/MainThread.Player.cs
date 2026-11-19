/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if !UNITY_EDITOR
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MainThreadInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (McpDisable.IsDisabled)
                return;
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

        /// <summary>
        /// The cancellation token of the MCP tool call running on the calling thread: McpPlugin's
        /// <see cref="ToolCallCancellation.Token"/>, which fires when the caller went away. Tests replace it.
        /// </summary>
        public static Func<CancellationToken> CallToken { get; set; } = () => ToolCallCancellation.Token;

        static Task<T> Dispatch<T>(Func<T> body)
        {
            // A cancelled call's work is not queued, and is skipped when the call is cancelled while it waits in the
            // queue. Work that has started is never interrupted.
            var cancel = CallToken();
            if (cancel.IsCancellationRequested)
                return Task.FromCanceled<T>(cancel);

            var tcs = new TaskCompletionSource<T>();
            MainThreadDispatcher.Enqueue(() =>
            {
                if (cancel.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancel);
                    return;
                }
                try { tcs.SetResult(body()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }
    }
}
#endif
