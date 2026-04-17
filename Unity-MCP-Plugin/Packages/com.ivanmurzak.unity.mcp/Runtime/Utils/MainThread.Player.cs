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
using System;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MainThreadInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (MainThread.Instance is not UnityMainThread)
                MainThread.Instance = new UnityMainThread();
        }
    }
    public class UnityMainThread : MainThread
    {
        public override bool IsMainThread => MainThreadDispatcher.IsMainThread;

        public override Task RunAsync(Task task)
        {
            if (MainThreadDispatcher.IsMainThread)
                return task;

            var tcs = new TaskCompletionSource<bool>();

            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    task.Wait();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public override Task<T> RunAsync<T>(Task<T> task)
        {
            if (MainThreadDispatcher.IsMainThread)
                return task;

            var tcs = new TaskCompletionSource<T>();

            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    T result = task.Result;
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public override Task<T> RunAsync<T>(Func<T> func)
        {
            if (MainThreadDispatcher.IsMainThread)
                return Task.FromResult(func());

            var tcs = new TaskCompletionSource<T>();

            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public override Task RunAsync(Action action)
        {
            if (MainThreadDispatcher.IsMainThread)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();

            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
#endif
