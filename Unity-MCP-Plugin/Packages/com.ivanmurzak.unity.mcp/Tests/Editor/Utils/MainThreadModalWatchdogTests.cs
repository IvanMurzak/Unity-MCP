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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    /// <summary>
    /// The modal-window watchdog of <see cref="UnityMainThread"/>: while a modal window holds the main thread, work
    /// posted from a background thread (an MCP tool call) fails within seconds with an error naming the window, instead
    /// of waiting until the client times out. The window probe is faked; the main thread is "blocked" by waiting on the
    /// background task, so the dispatch pump cannot run, as under a real modal window.
    /// </summary>
    public class MainThreadModalWatchdogTests
    {
        static readonly EditorModalWindow Window = new EditorModalWindow(
            "Missing signature", "The package has no signature.\nImport it anyway?", new[] { "Import Anyway", "Cancel" });

        Func<EditorModalWindow?> _originalProbe = null!;

        [SetUp]
        public void SetUp() => _originalProbe = UnityMainThread.FindBlockingWindow;

        [TearDown]
        public void TearDown() => UnityMainThread.FindBlockingWindow = _originalProbe;

        // Long enough for the grace period plus two watchdog ticks.
        static readonly TimeSpan BlockFor = TimeSpan.FromSeconds(UnityMainThread.GraceSeconds + 3);

        [UnityTest]
        public IEnumerator QueuedWork_WhileModalWindowBlocks_FailsFastAndNeverRuns()
        {
            UnityMainThread.FindBlockingWindow = () => Window;
            var ran = 0;
            var call = Task.Run(() => MainThread.Instance.Run(() => { Interlocked.Exchange(ref ran, 1); return 1; }));

            // The main thread is held here, so the pump cannot pick the work up.
            var finished = SpinWait.SpinUntil(() => call.IsCompleted, BlockFor);

            Assert.IsTrue(finished, "The call must fail while the main thread is still blocked.");
            Assert.IsTrue(call.IsFaulted);
            var error = call.Exception!.GetBaseException();
            Assert.IsInstanceOf<ToolUnavailableException>(error);
            StringAssert.Contains("\"Missing signature\"", error.Message);
            StringAssert.Contains("[Import Anyway | Cancel]", error.Message);
            StringAssert.Contains("The call did not run", error.Message);
            // The MCP server reads that phrase as a plugin-reload refusal and relabels the error.
            StringAssert.DoesNotContain("this call was not run", error.Message.ToLowerInvariant());

            // Once the main thread runs again, the abandoned work must not run: its caller was told it did not.
            for (var i = 0; i < 5; i++)
                yield return null;
            Assert.AreEqual(0, Volatile.Read(ref ran));
        }

        [UnityTest]
        public IEnumerator QueuedWork_BusyMainThreadWithoutModalWindow_KeepsWaitingAndRuns()
        {
            UnityMainThread.FindBlockingWindow = () => null;
            var call = Task.Run(() => MainThread.Instance.Run(() => 7));

            var finished = SpinWait.SpinUntil(() => call.IsCompleted, BlockFor);
            Assert.IsFalse(finished, "A busy (not blocked) main thread must not fail the call.");

            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!call.IsCompleted && DateTime.UtcNow < deadline)
                yield return null;
            Assert.IsTrue(call.IsCompleted && !call.IsFaulted, "The call must run once the main thread is free.");
            Assert.AreEqual(7, call.Result);
        }

        [UnityTest]
        public IEnumerator RunningWork_ThatWaitsOnAModalWindow_FailsTheCallerButFinishes()
        {
            UnityMainThread.FindBlockingWindow = () => Window;
            var finishedBody = 0;
            // The body runs on the main thread and holds it, as a tool whose work opened the modal window does.
            var call = Task.Run(() => MainThread.Instance.Run(() =>
            {
                Thread.Sleep(BlockFor);
                Interlocked.Exchange(ref finishedBody, 1);
                return 1;
            }));

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (!call.IsCompleted && DateTime.UtcNow < deadline)
                yield return null;

            Assert.IsTrue(call.IsFaulted);
            var error = call.Exception!.GetBaseException();
            Assert.IsInstanceOf<ToolUnavailableException>(error);
            StringAssert.Contains("already running", error.Message);
            Assert.AreEqual(1, Volatile.Read(ref finishedBody), "Work that had started runs to its end.");
        }

        [Test]
        public void Post_IsNotFailedByTheWatchdog()
        {
            UnityMainThread.FindBlockingWindow = () => Window;
            var ran = 0;
            UnityMainThread.Post(() => Interlocked.Exchange(ref ran, 1));
            Thread.Sleep(BlockFor); // blocked: the watchdog ticks, but posted work is not its to fail
            Assert.AreEqual(0, Volatile.Read(ref ran), "Posted work waits for the main thread.");
        }

        [Test]
        public void Describe_NamesTitleMessageAndButtons()
            => Assert.AreEqual(
                "\"Missing signature\": The package has no signature. Import it anyway? [Import Anyway | Cancel]",
                Window.Describe());

        [Test]
        public void Describe_UntitledLongMessage_IsTruncated()
        {
            var window = new EditorModalWindow(string.Empty, new string('x', 1000), new[] { "OK" });
            var text = window.Describe();
            Assert.IsTrue(text.StartsWith(new string('x', 300) + "... [OK]"), text);
        }

        [Test]
        public void BlockedCallMessage_TellsTheAgentNotToPressTheWindow()
        {
            var message = Window.BlockedCallMessage(started: false);
            StringAssert.Contains("ask the user to answer it", message);
            StringAssert.Contains("do not press its buttons", message);
        }
    }
}
