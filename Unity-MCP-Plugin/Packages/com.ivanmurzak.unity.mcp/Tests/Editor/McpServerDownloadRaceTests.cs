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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression tests for the first-install server-binary download RACE. On a fresh plugin install two triggers
    /// funnel into <see cref="McpServerManager"/>.<c>DownloadAndUnpackBinary</c> — the <c>[InitializeOnLoad]</c>
    /// static ctor and <c>ServerBinaryUpdateWatcher</c>'s <c>registeredPackages</c> handler (which UPM fires
    /// multiple times per install) — plus the manual <c>Tools ▸ Server ▸ Download Binaries</c> menu, which calls
    /// <c>DownloadAndUnpackBinary</c> DIRECTLY (bypassing <c>DownloadServerBinaryIfNeeded</c>'s status check). The
    /// old dedup read the <see cref="McpServerStatus.Downloading"/> status then set it only later (a TOCTOU), so
    /// two callers both entered, both downloaded to the SAME fixed temp zip path, and the second
    /// <c>WebClient</c> opening the file the first still held threw <c>IOException: Sharing violation</c> — the
    /// download then only succeeded on a manual retry (a single invocation → no race).
    ///
    /// <para>The fix has two deterministic, editor-free, network-free seams these tests pin:
    ///   • <see cref="McpServerManager.TryBeginDownload"/> / <see cref="McpServerManager.EndDownload"/> — the
    ///     atomic single-flight guard (<c>Interlocked.CompareExchange</c>). Exactly one concurrent caller may hold
    ///     the slot; every other caller no-ops. Reverting it to a non-atomic check-then-set makes the contention
    ///     test red.
    ///   • <see cref="McpServerManager.BuildArchiveFilePath"/> — the per-attempt UNIQUE temp zip path (trailing
    ///     Guid, mirroring the staging folder). Two calls yield different paths, so even a guard-bypassing caller
    ///     cannot collide on the zip. Reverting it to the old fixed name makes the uniqueness test red.</para>
    /// Nothing here touches a running Editor, the network, or the real project's Library folder.
    /// </summary>
    public class McpServerDownloadRaceTests
    {
        // The single-flight slot is shared static state; never let a test leak it (a leak would make every later
        // download no-op forever). Release it before and after each test.
        [SetUp]
        public void ReleaseSlotBefore() => McpServerManager.EndDownload();

        [TearDown]
        public void ReleaseSlotAfter() => McpServerManager.EndDownload();

        // --- Atomic single-flight guard (the core TOCTOU fix) ---

        [Test]
        public void TryBeginDownload_SecondCaller_IsRejectedUntilEndDownload()
        {
            Assert.IsFalse(McpServerManager.IsDownloadInProgress, "precondition: no download slot held");

            Assert.IsTrue(McpServerManager.TryBeginDownload(), "the first caller must acquire the download slot");
            Assert.IsTrue(McpServerManager.IsDownloadInProgress);

            // The SECOND concurrent caller must be rejected — this is the whole fix: on the old check-then-set both
            // callers passed the guard and raced on the same temp zip (Sharing violation).
            Assert.IsFalse(McpServerManager.TryBeginDownload(),
                "a second caller must NOT start a concurrent download while one is in flight");

            McpServerManager.EndDownload();

            // After release the slot is free again, so a later retry / real download can proceed.
            Assert.IsFalse(McpServerManager.IsDownloadInProgress);
            Assert.IsTrue(McpServerManager.TryBeginDownload(), "the slot must be re-acquirable after EndDownload");
            McpServerManager.EndDownload();
        }

        [Test]
        public void TryBeginDownload_UnderMaxContention_ExactlyOneWinner()
        {
            Assert.IsFalse(McpServerManager.IsDownloadInProgress, "precondition: no download slot held");

            const int threadCount = 32;
            var winners = 0;
            using (var barrier = new Barrier(threadCount))
            {
                var tasks = new Task[threadCount];
                for (var i = 0; i < threadCount; i++)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        barrier.SignalAndWait(); // release all threads at once to maximize the race window
                        if (McpServerManager.TryBeginDownload())
                            Interlocked.Increment(ref winners);
                    });
                }
                Assert.IsTrue(Task.WaitAll(tasks, 30_000), "all contending tasks should finish in time");
            }

            Assert.AreEqual(1, winners,
                "exactly one caller may hold the single-flight download slot even under maximal contention " +
                "(a non-atomic check-then-set would let more than one through — the original bug)");

            McpServerManager.EndDownload();
            Assert.IsFalse(McpServerManager.IsDownloadInProgress);
        }

        // --- Per-attempt UNIQUE temp archive path (defensive belt-and-suspenders) ---

        [Test]
        public void BuildArchiveFilePath_ProducesDistinctPathPerCall_NeverCollides()
        {
            const int calls = 64;
            var seen = new HashSet<string>();
            for (var i = 0; i < calls; i++)
                Assert.IsTrue(seen.Add(McpServerManager.BuildArchiveFilePath()),
                    "every download attempt must get a UNIQUE temp archive path (the old fixed name collided on " +
                    "concurrent first-install downloads → Sharing violation)");

            Assert.AreEqual(calls, seen.Count);
        }

        [Test]
        public void BuildArchiveFilePath_KeepsExpectedShape()
        {
            var path = McpServerManager.BuildArchiveFilePath();

            StringAssert.EndsWith(".zip", path);
            var fileName = Path.GetFileName(path);
            StringAssert.StartsWith(McpServerManager.ExecutableName.ToLowerInvariant() + "-", fileName);
            StringAssert.Contains(McpServerManager.PlatformName, fileName);
            StringAssert.Contains(McpServerManager.ServerVersion, fileName);
        }
    }
}
