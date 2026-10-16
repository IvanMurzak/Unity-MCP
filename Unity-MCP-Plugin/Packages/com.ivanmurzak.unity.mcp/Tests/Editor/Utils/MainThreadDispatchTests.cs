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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>
    /// Regression for the flaky <c>ToolThreadSafetyTests.ToolList_BothThreads</c> /
    /// <c>ToolSetEnabledState_BothThreads</c> timeouts. A background thread issuing
    /// back-to-back <see cref="MainThread.Run{T}(System.Func{T})"/> calls (a tool body
    /// hop followed by the RQL result-formatting hop) used to subscribe to
    /// <c>EditorApplication.update</c> — a plain static delegate field, so <c>+=</c> is a
    /// non-atomic read-modify-write — while the main thread was unsubscribing the
    /// previous hop (or another background thread was subscribing). The lost write dropped
    /// a hop and its caller blocked forever, or resurrected a finished hop so it ran twice.
    /// Against the old dispatcher this test stalls (or logs InvalidOperationException) on
    /// every run.
    /// </summary>
    public class MainThreadDispatchTests
    {
        const int Workers = 8;
        const int IterationsPerWorker = 500;
        const int Total = Workers * IterationsPerWorker;
        const float StallSeconds = 15f;

        static void Noop() { }

        [UnityTest]
        public IEnumerator Run_BackToBackFromBackgroundThread_NeverLosesADispatch()
        {
            var completed = 0;
            // Several workers hop back-to-back, as concurrent tool calls from the MCP server do.
            var workers = new Task[Workers];
            for (var w = 0; w < Workers; w++)
            {
                workers[w] = Task.Run(() =>
                {
                    Assert.IsFalse(MainThread.Instance.IsMainThread);
                    for (var i = 0; i < IterationsPerWorker; i++)
                    {
                        var expected = i;
                        var actual = MainThread.Instance.Run(() =>
                        {
                            Assert.IsTrue(MainThread.Instance.IsMainThread, "Dispatched work must run on the main thread.");
                            return expected;
                        });
                        Assert.AreEqual(expected, actual);
                        Interlocked.Increment(ref completed);
                    }
                });
            }
            var task = Task.WhenAll(workers);

            var lastProgress = Volatile.Read(ref completed);
            var lastProgressTime = Time.realtimeSinceStartup;
            while (!task.IsCompleted)
            {
                // Other main-thread subscribers churning EditorApplication.update must not
                // interfere with dispatches posted from the background thread either.
                EditorApplication.update += Noop;
                EditorApplication.update -= Noop;

                var now = Volatile.Read(ref completed);
                if (now != lastProgress)
                {
                    lastProgress = now;
                    lastProgressTime = Time.realtimeSinceStartup;
                }
                else if (Time.realtimeSinceStartup - lastProgressTime > StallSeconds)
                {
                    Assert.Fail($"Main-thread dispatch stalled: {now}/{Total} background-thread " +
                        $"MainThread.Run calls completed, then no progress for {StallSeconds} s.");
                }
                yield return null;
            }

            if (task.IsFaulted)
                throw task.Exception!;
            Assert.AreEqual(Total, completed);
        }
    }
}
