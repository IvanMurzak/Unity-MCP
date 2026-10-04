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
using System.IO;
using System.Linq;
using System.Threading;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The `console-get-logs` contract: honour includeStackTrace, and hand out a monotonic `sequence`
    /// that agents use as a cursor (`sinceSequence`). Both storage implementations are exercised, because
    /// the plugin ships the buffered one while the plain one is the base of it.
    /// Entries are appended straight to the storage - never through Debug.LogError - so error entries with
    /// a stored trace can be used without failing the test run.
    /// </summary>
    public class TestLogSequenceCursor : BaseTest
    {
        const string LogFileName = "sequence-cursor-logs.txt";

        readonly List<string> _directories = new();
        readonly List<string> _files = new();
        readonly List<FileLogStorage> _storages = new();

        [TearDown]
        public void TestTearDown()
        {
            foreach (var storage in _storages)
                storage.Dispose();
            _storages.Clear();

            foreach (var file in _files)
            {
                try { File.Delete(file); }
                catch { /* best effort */ }
            }
            _files.Clear();

            foreach (var directory in _directories)
            {
                try { Directory.Delete(directory, recursive: true); }
                catch { /* best effort - it lives under the OS temp folder */ }
            }
            _directories.Clear();
        }

        string NewDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "unity-mcp-log-cursor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _directories.Add(directory);
            return directory;
        }

        FileLogStorage Open(string directory, bool buffered, int flushEntriesThreshold = 10)
        {
            FileLogStorage storage = buffered
                ? new BufferedFileLogStorage(
                    flushEntriesThreshold: flushEntriesThreshold,
                    cacheFilePath: directory,
                    cacheFileName: LogFileName)
                : new FileLogStorage(
                    directoryPath: directory,
                    requestedFileName: LogFileName);
            _storages.Add(storage);
            return storage;
        }

        /// <summary>Follows the cursor the way an agent does: next call starts at the highest sequence received.</summary>
        static List<LogEntry> ReadAllPages(FileLogStorage storage, int pageSize, LogType? filter, out int pages)
        {
            var all = new List<LogEntry>();
            long cursor = 0;
            pages = 0;
            for (var guard = 0; guard < 10_000; guard++)
            {
                var page = storage.QuerySince(cursor, maxEntries: pageSize, logTypeFilter: filter);
                if (page.Length == 0)
                    break;

                Assert.LessOrEqual(page.Length, pageSize, "A page must never exceed maxEntries.");
                pages++;
                all.AddRange(page);
                cursor = page.Max(entry => entry.Sequence);
            }
            return all;
        }

        // ------------------------------------------------------------------ 1. stack trace

        [TestCase(false)]
        [TestCase(true)]
        public void ErrorEntryWithStackTrace_IsReturnedOnlyWhenRequested(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered);
            const string trace = "at Game.Player.Update () [0x00001] in Player.cs:42";
            storage.Append(new LogEntry(LogType.Error, "boom", trace));

            // Asserted first: the fixture provably holds a trace, so the assertions below cannot pass by absence.
            var withTrace = storage.Query(includeStackTrace: true);
            Assert.AreEqual(1, withTrace.Length);
            Assert.AreEqual(trace, withTrace[0].StackTrace, "includeStackTrace=true must return the stored trace.");

            Assert.IsNull(storage.Query()[0].StackTrace, "includeStackTrace defaults to false.");
            Assert.IsNull(storage.Query(includeStackTrace: false)[0].StackTrace);
            Assert.IsNull(storage.QuerySince(0)[0].StackTrace, "The cursor query honours the flag too.");
            Assert.AreEqual(trace, storage.QuerySince(0, includeStackTrace: true)[0].StackTrace);

            // Same entry after it has reached the file (a buffered entry lives in a shared buffer slot until then).
            storage.Flush();
            Assert.AreEqual(trace, storage.Query(includeStackTrace: true)[0].StackTrace);
            Assert.IsNull(storage.Query()[0].StackTrace);
            Assert.IsNull(storage.QuerySince(0)[0].StackTrace);
            Assert.AreEqual(trace, storage.QuerySince(0, includeStackTrace: true)[0].StackTrace);
        }

        [Test]
        public void BufferedStorage_StrippingStackTraceDoesNotTouchTheBufferedEntry()
        {
            var storage = Open(NewDirectory(), buffered: true, flushEntriesThreshold: 100);
            const string trace = "at A.B () in C.cs:1";
            storage.Append(new LogEntry(LogType.Error, "still in the buffer", trace));

            // The entry has not been flushed, so a query reads the buffer's own instance. Stripping that
            // instance would lose the trace for every later query AND for the file write.
            Assert.IsNull(storage.Query()[0].StackTrace);
            Assert.IsNull(storage.QuerySince(0)[0].StackTrace);

            Assert.AreEqual(trace, storage.Query(includeStackTrace: true)[0].StackTrace);
            storage.Flush();
            Assert.AreEqual(trace, storage.Query(includeStackTrace: true)[0].StackTrace,
                "The trace must still reach the file after default queries ran.");
        }

        // ------------------------------------------------------------------ 3. paging

        [TestCase(false)]
        [TestCase(true)]
        public void Paging_UnionOfPagesEqualsTheMatchingSet_NoGapsNoDuplicates(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered);
            const int matching = 61;
            const int pageSize = 7;

            // Matching warnings interleaved with non-matching logs. 181 entries against a flush threshold of 10
            // leaves one entry in the buffer, so a page boundary can fall inside the buffered tail.
            for (var i = 0; i < matching - 1; i++)
            {
                storage.Append(new LogEntry(LogType.Log, $"noise-a {i}"));
                storage.Append(new LogEntry(LogType.Warning, $"match {i}"));
                storage.Append(new LogEntry(LogType.Log, $"noise-b {i}"));
            }
            storage.Append(new LogEntry(LogType.Warning, $"match {matching - 1}"));

            var firstPage = storage.QuerySince(0, maxEntries: pageSize, logTypeFilter: LogType.Warning);
            CollectionAssert.AreEqual(
                Enumerable.Range(0, pageSize).Select(i => $"match {i}"),
                firstPage.Select(e => e.Message),
                "On overflow the OLDEST page must come back, otherwise the next call leaves a gap.");

            var all = ReadAllPages(storage, pageSize, LogType.Warning, out var pages);

            CollectionAssert.AreEqual(
                Enumerable.Range(0, matching).Select(i => $"match {i}"),
                all.Select(e => e.Message),
                "Every matching entry exactly once, in order.");
            Assert.AreEqual((matching + pageSize - 1) / pageSize, pages, "Page count");
            Assert.AreEqual(matching, all.Select(e => e.Sequence).Distinct().Count(), "No duplicate sequences.");
            CollectionAssert.IsOrdered(all.Select(e => e.Sequence), "Pages are oldest first.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Query_WithoutCursor_StillReturnsTheNewestEntriesChronologically(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered);
            for (var i = 0; i < 25; i++)
                storage.Append(new LogEntry(LogType.Log, $"entry {i}"));

            var newest = storage.Query(maxEntries: 5);

            CollectionAssert.AreEqual(
                Enumerable.Range(20, 5).Select(i => $"entry {i}"),
                newest.Select(e => e.Message),
                "sinceSequence=0 keeps today's behaviour: the newest page, oldest first.");
            Assert.IsTrue(newest.All(e => e.Sequence > 0), "Every returned entry carries its sequence.");
            CollectionAssert.IsOrdered(newest.Select(e => e.Sequence));
        }

        [Test]
        public void Query_WithoutCursor_NewestPageSpanningBufferAndFile_IsChronological()
        {
            // Threshold 10: entries 0-19 reach the file, 20-24 stay in the buffer, so a page of 8 spans both.
            var storage = Open(NewDirectory(), buffered: true, flushEntriesThreshold: 10);
            for (var i = 0; i < 25; i++)
                storage.Append(new LogEntry(LogType.Log, $"entry {i}"));

            var newest = storage.Query(maxEntries: 8);

            CollectionAssert.AreEqual(
                Enumerable.Range(17, 8).Select(i => $"entry {i}"),
                newest.Select(e => e.Message),
                "The file part is older than the buffered part, so it must come first.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuerySince_HugeMaxEntries_ReturnsTheEntries(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered);
            for (var i = 0; i < 3; i++)
                storage.Append(new LogEntry(LogType.Log, $"entry {i}"));

            // maxEntries comes straight from the caller: it must only cap the result, never size an allocation.
            var all = storage.QuerySince(0, maxEntries: int.MaxValue);

            CollectionAssert.AreEqual(
                new[] { "entry 0", "entry 1", "entry 2" },
                all.Select(e => e.Message));
        }

        [Test]
        public void Cursor_AtTheHighestSequence_ReturnsNothing_AndNewEntriesAreSeenOnce()
        {
            var storage = Open(NewDirectory(), buffered: true);
            for (var i = 0; i < 5; i++)
                storage.Append(new LogEntry(LogType.Log, $"entry {i}"));
            var highest = storage.Query().Max(e => e.Sequence);

            Assert.IsEmpty(storage.QuerySince(highest), "Nothing newer than the highest sequence.");

            storage.Append(new LogEntry(LogType.Warning, "fresh"));
            var fresh = storage.QuerySince(highest);
            Assert.AreEqual(1, fresh.Length);
            Assert.AreEqual("fresh", fresh[0].Message);
            Assert.AreEqual(highest + 1, fresh[0].Sequence);
        }

        // ------------------------------------------------------------------ 4. reset

        [TestCase(false)]
        [TestCase(true)]
        public void Clear_ThenReopen_SequencesContinueAbovePreClearMaximum(bool buffered)
        {
            var directory = NewDirectory();
            var first = Open(directory, buffered);
            for (var i = 0; i < 10; i++)
                first.Append(new LogEntry(LogType.Log, $"before {i}"));
            var preClearMax = first.Query(maxEntries: 100).Max(e => e.Sequence);

            first.Clear();
            first.Dispose();

            var reopened = Open(directory, buffered);
            for (var i = 0; i < 3; i++)
                reopened.Append(new LogEntry(LogType.Log, $"after {i}"));

            var after = reopened.Query();
            Assert.AreEqual(3, after.Length, "Clear() removed the old entries.");
            Assert.IsTrue(after.All(e => e.Sequence > preClearMax),
                $"The counter must not restart at 1 after Clear + reopen (pre-clear max {preClearMax}, got {string.Join(",", after.Select(e => e.Sequence))}).");

            // An agent still holding a cursor from before the clear gets the new entries, not [].
            var stale = reopened.QuerySince(preClearMax);
            CollectionAssert.AreEqual(after.Select(e => e.Sequence), stale.Select(e => e.Sequence));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Clear_SameInstance_SequencesContinue_AndTheOldCursorSeesOnlyNewEntries(bool buffered)
        {
            // What `console-clear-logs` does: clear the live storage and keep logging into it.
            var storage = Open(NewDirectory(), buffered);
            for (var i = 0; i < 10; i++)
                storage.Append(new LogEntry(LogType.Log, $"before {i}"));
            var preClearMax = storage.Query(maxEntries: 100).Max(e => e.Sequence);

            storage.Clear();
            Assert.IsEmpty(storage.QuerySince(preClearMax), "Nothing new right after the clear.");

            for (var i = 0; i < 3; i++)
                storage.Append(new LogEntry(LogType.Log, $"after {i}"));

            var fresh = storage.QuerySince(preClearMax);
            CollectionAssert.AreEqual(new[] { "after 0", "after 1", "after 2" }, fresh.Select(e => e.Message));
            Assert.AreEqual(preClearMax + 1, fresh[0].Sequence, "Clear() must not restart the counter.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SidecarLost_ThenClearAndCrash_SequenceStaysAboveEverythingIssued(bool buffered)
        {
            var sidecarName = Path.GetFileNameWithoutExtension(LogFileName) + ".sequence";
            var directory = NewDirectory();
            var first = Open(directory, buffered);
            for (var i = 0; i < 5; i++)
                first.Append(new LogEntry(LogType.Log, $"entry {i}"));
            first.Flush();
            var issued = first.Query(maxEntries: 100).Max(e => e.Sequence);
            first.Dispose();

            // The sidecar is lost; only the log still knows which numbers were issued.
            File.Delete(Path.Combine(directory, sidecarName));

            var reopened = Open(directory, buffered);
            reopened.Clear(); // deletes the log: from here on only the sidecar remembers

            // Crash: no Dispose. The next instance starts from what is on disk.
            Assert.IsTrue(File.Exists(Path.Combine(directory, sidecarName)),
                "Deleting the log must not leave the counter with no record at all.");
            var restartedDirectory = NewDirectory();
            File.Copy(Path.Combine(directory, sidecarName), Path.Combine(restartedDirectory, sidecarName));

            var restarted = Open(restartedDirectory, buffered);
            restarted.Append(new LogEntry(LogType.Log, "after restart"));

            Assert.Greater(restarted.Query()[0].Sequence, issued, "A sequence must never be reissued.");
        }

        [Test]
        public void DefaultLocation_SequenceSidecarIsNotInTheServerBinaryFolder()
        {
            var name = $"sidecar-location-{Guid.NewGuid():N}.txt";
            var sidecar = Path.GetFileNameWithoutExtension(name) + ".sequence";
            var projectRoot = Path.GetDirectoryName(Application.dataPath)!;
            var expected = Path.Combine(projectRoot, "Library", "mcp-logs", sidecar);
            var insideServerFolder = Path.Combine(McpServerManager.ExecutableFolderRootPath, sidecar);
            _files.Add(expected);
            _files.Add(insideServerFolder);
            _files.Add(Path.Combine(projectRoot, "Temp", "mcp-server", name));

            var storage = new FileLogStorage(requestedFileName: name); // the Editor default: no directory given
            _storages.Add(storage);
            storage.Append(new LogEntry(LogType.Log, "entry"));
            storage.Dispose(); // writes the exact high-water mark

            // McpServerManager.DeleteBinaryFolderIfExists deletes that folder recursively on every server update.
            Assert.IsFalse(File.Exists(insideServerFolder), "The sidecar must not live in the server binary cache.");
            Assert.IsTrue(File.Exists(expected), "The sidecar lives under Library/, which survives the Temp/ wipe.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CursorAboveEverythingIssued_ReturnsTheOldestPage_NotEmpty(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered);
            for (var i = 0; i < 12; i++)
                storage.Append(new LogEntry(LogType.Log, $"entry {i}"));
            var highest = storage.Query(maxEntries: 100).Max(e => e.Sequence);

            var page = storage.QuerySince(highest + 1_000, maxEntries: 5);

            CollectionAssert.AreEqual(
                Enumerable.Range(0, 5).Select(i => $"entry {i}"),
                page.Select(e => e.Message),
                "A cursor above the highest sequence means the counter restarted: return the oldest page.");
            CollectionAssert.AreEqual(
                page.Select(e => e.Sequence),
                storage.QuerySince(highest + 1, maxEntries: 5).Select(e => e.Sequence),
                "The boundary: one above the highest is already a restarted counter.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EditorRestart_LogFileGoneAndNoOrderlyDispose_SequenceStaysAboveEverythingIssued(bool buffered)
        {
            // An Editor restart wipes Temp/ (where the log lives) while Library/ (the sidecar) survives, and a
            // crashed Editor never ran Dispose. The sidecar of a RUNNING storage holds a reserved ceiling, so
            // the next instance still starts above everything that was issued.
            var runningDirectory = NewDirectory();
            var running = Open(runningDirectory, buffered, flushEntriesThreshold: 1);
            for (var i = 0; i < 5; i++)
                running.Append(new LogEntry(LogType.Log, $"entry {i}"));
            var issued = running.Query(maxEntries: 100).Max(e => e.Sequence);

            // `running` is deliberately NOT disposed: copy only the sidecar into a directory with no log file.
            var sidecarName = Path.GetFileNameWithoutExtension(LogFileName) + ".sequence";
            var restartedDirectory = NewDirectory();
            File.Copy(Path.Combine(runningDirectory, sidecarName), Path.Combine(restartedDirectory, sidecarName));
            Assert.IsFalse(File.Exists(Path.Combine(restartedDirectory, LogFileName)), "The log did not survive the restart.");

            var restarted = Open(restartedDirectory, buffered);
            restarted.Append(new LogEntry(LogType.Log, "after restart"));

            var sequence = restarted.Query()[0].Sequence;
            Assert.Greater(sequence, issued, "A sequence must never be reissued, even when the log file is gone.");
            Assert.IsEmpty(restarted.QuerySince(sequence), "Nothing is newer than the entry just written.");
        }

        // ------------------------------------------------------------------ 5. concurrency

        [TestCase(false)]
        [TestCase(true)]
        public void ConcurrentLogging_PagesJoinWithNoGapsOrDuplicates(bool buffered)
        {
            var storage = Open(NewDirectory(), buffered, flushEntriesThreshold: 16);
            const int threads = 8;
            const int perThread = 151; // 1208 entries: not a multiple of 16, so the buffered tail is non-empty

            // Dedicated threads released together, so the appends really interleave (a thread pool may start
            // its workers one after another).
            using var start = new Barrier(threads);
            var workers = Enumerable.Range(0, threads).Select(t => new Thread(() =>
            {
                start.SignalAndWait();
                for (var i = 0; i < perThread; i++)
                    storage.Append(new LogEntry(LogType.Log, $"t{t}-{i}"));
            })).ToList();
            workers.ForEach(worker => worker.Start());
            workers.ForEach(worker => worker.Join());

            var all = ReadAllPages(storage, pageSize: 37, filter: null, out _);

            Assert.AreEqual(threads * perThread, all.Count, "Every entry exactly once.");
            CollectionAssert.AreEqual(
                Enumerable.Range(1, threads * perThread).Select(i => (long)i),
                all.Select(e => e.Sequence),
                "Sequences are a gap-free run from 1: assigned atomically with storing.");
            Assert.AreEqual(threads * perThread, all.Select(e => e.Message).Distinct().Count(), "No duplicated entry.");

            // Entries written by one thread keep their relative order.
            for (var t = 0; t < threads; t++)
            {
                var own = all.Where(e => e.Message.StartsWith($"t{t}-")).Select(e => int.Parse(e.Message.Substring($"t{t}-".Length)));
                CollectionAssert.IsOrdered(own, $"thread {t}");
            }
        }

        // ------------------------------------------------------------------ 6. domain reload

        [TestCase(false)]
        [TestCase(true)]
        public void Reopen_AfterFlush_SequencesContinue(bool buffered)
        {
            var directory = NewDirectory();
            var first = Open(directory, buffered);
            for (var i = 0; i < 5; i++)
                first.Append(new LogEntry(LogType.Log, $"before reload {i}"));
            first.Flush();
            var lastBefore = first.Query().Max(e => e.Sequence);
            first.Dispose();

            var reopened = Open(directory, buffered); // append mode over the same file
            for (var i = 0; i < 3; i++)
                reopened.Append(new LogEntry(LogType.Log, $"after reload {i}"));

            var fresh = reopened.QuerySince(lastBefore);
            CollectionAssert.AreEqual(
                new[] { "after reload 0", "after reload 1", "after reload 2" },
                fresh.Select(e => e.Message));
            Assert.AreEqual(lastBefore + 1, fresh[0].Sequence, "An orderly reopen continues right after the last number.");

            var everything = ReadAllPages(reopened, pageSize: 4, filter: null, out _);
            Assert.AreEqual(8, everything.Count, "Entries from before and after the reopen are one stream.");
            CollectionAssert.IsOrdered(everything.Select(e => e.Sequence));
        }

        // ------------------------------------------------------------------ serialization + the tool

        [Test]
        public void LogEntry_SerializesSequenceAsLowercaseField()
        {
            var entry = new LogEntry(LogType.Log, "m") { Sequence = 7 };

            var json = System.Text.Json.JsonSerializer.Serialize(entry);

            StringAssert.Contains("\"sequence\":7", json);
            StringAssert.DoesNotContain("\"Sequence\"", json);

            var roundTrip = System.Text.Json.JsonSerializer.Deserialize<LogEntry>(json)!;
            Assert.AreEqual(7, roundTrip.Sequence);
        }

        [Test]
        public void Tool_SinceSequence_ReturnsOnlyNewerEntries()
        {
            var tool = new Tool_Console();
            var marker = Guid.NewGuid().ToString("N");

            Debug.Log($"cursor-first {marker}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();
            var seen = tool.GetLogs(maxEntries: 1000).First(e => e.Message.Contains($"cursor-first {marker}"));
            Assert.Greater(seen.Sequence, 0, "Every returned entry carries a sequence.");

            Debug.Log($"cursor-second {marker}");
            UnityMcpPluginEditor.Instance.LogCollector!.Save();

            var newer = tool.GetLogs(maxEntries: 1000, sinceSequence: seen.Sequence);

            Assert.IsTrue(newer.All(e => e.Sequence > seen.Sequence));
            Assert.IsTrue(newer.Any(e => e.Message.Contains($"cursor-second {marker}")));
            Assert.IsFalse(newer.Any(e => e.Message.Contains($"cursor-first {marker}")));
            CollectionAssert.IsOrdered(newer.Select(e => e.Sequence), "Cursor results are oldest first.");
        }

        [Test]
        public void Tool_NegativeSinceSequence_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new Tool_Console().GetLogs(sinceSequence: -1));
        }
    }
}
