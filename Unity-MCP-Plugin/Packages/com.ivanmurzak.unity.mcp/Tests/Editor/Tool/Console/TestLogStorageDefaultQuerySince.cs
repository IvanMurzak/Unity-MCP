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
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// <see cref="ILogStorage"/> is public, and <c>QuerySince</c>/<c>QuerySinceAsync</c> were added after its first
    /// release. <see cref="LegacyLogStorage"/> implements ONLY the members that existed before them, so this file
    /// compiling at all is the backward-compatibility check; the tests pin what the default implementations return.
    /// </summary>
    public class TestLogStorageDefaultQuerySince : BaseTest
    {
        /// <summary>
        /// An implementer written before the cursor existed: in memory, newest-page <see cref="Query"/>, and it
        /// stores entries as given (it never assigns <see cref="LogEntry.Sequence"/>).
        /// Do not add QuerySince/QuerySinceAsync here - the point is that it does not have them.
        /// </summary>
        class LegacyLogStorage : ILogStorage
        {
            readonly object _lock = new();
            readonly List<LogEntry> _entries = new();

            public int QueryCalls;

            public Task AppendAsync(params LogEntry[] entries)
            {
                Append(entries);
                return Task.CompletedTask;
            }

            public void Append(params LogEntry[] entries)
            {
                lock (_lock)
                    _entries.AddRange(entries);
            }

            public Task FlushAsync() => Task.CompletedTask;
            public void Flush() { }

            public Task<LogEntry[]> QueryAsync(
                int maxEntries = 100,
                LogType? logTypeFilter = null,
                bool includeStackTrace = false,
                int lastMinutes = 0)
                => Task.FromResult(Query(maxEntries, logTypeFilter, includeStackTrace, lastMinutes));

            public LogEntry[] Query(
                int maxEntries = 100,
                LogType? logTypeFilter = null,
                bool includeStackTrace = false,
                int lastMinutes = 0)
            {
                lock (_lock)
                {
                    QueryCalls++;
                    var cutoff = lastMinutes > 0 ? DateTime.Now.AddMinutes(-lastMinutes) : DateTime.MinValue;
                    return _entries
                        .Where(entry => entry.Timestamp >= cutoff)
                        .Where(entry => !logTypeFilter.HasValue || entry.LogType == logTypeFilter.Value)
                        .Reverse()
                        .Take(maxEntries)
                        .Reverse()
                        .Select(entry => entry.Clone(includeStackTrace))
                        .ToArray();
                }
            }

            public void Clear()
            {
                lock (_lock)
                    _entries.Clear();
            }

            public void Dispose() { }
        }

        static LogEntry Entry(string message, LogType type, long sequence = 0)
            => new LogEntry(type, message, DateTime.Now, "trace " + message) { Sequence = sequence };

        /// <summary>
        /// <paramref name="count"/> entries, every third a Warning. When <paramref name="sequenced"/> they carry
        /// sequences 1..count, as if stored through a sequencing writer; otherwise all are 0.
        /// </summary>
        static LegacyLogStorage Fill(int count, bool sequenced)
        {
            var storage = new LegacyLogStorage();
            for (var i = 1; i <= count; i++)
                storage.Append(Entry($"entry {i}", i % 3 == 0 ? LogType.Warning : LogType.Log, sequence: sequenced ? i : 0));
            return storage;
        }

        static LegacyLogStorage Unsequenced(int count) => Fill(count, sequenced: false);
        static LegacyLogStorage Sequenced(int count) => Fill(count, sequenced: true);

        static string[] Messages(IEnumerable<LogEntry> entries) => entries.Select(e => e.Message).ToArray();

        [TestCase(typeof(FileLogStorage))]
        [TestCase(typeof(BufferedFileLogStorage))]
        public void BuiltInStorages_OverrideTheDefaults(Type storageType)
        {
            var map = storageType.GetInterfaceMap(typeof(ILogStorage));
            var checkedMembers = 0;
            for (var i = 0; i < map.InterfaceMethods.Length; i++)
            {
                var name = map.InterfaceMethods[i].Name;
                if (name != nameof(ILogStorage.QuerySince) && name != nameof(ILogStorage.QuerySinceAsync))
                    continue;
                checkedMembers++;
                // BufferedFileLogStorage inherits FileLogStorage's public virtual implementation.
                Assert.AreEqual(typeof(FileLogStorage), map.TargetMethods[i].DeclaringType,
                    $"{storageType.Name}.{name} must dispatch to the storage's own implementation, not the interface default.");
            }
            Assert.AreEqual(2, checkedMembers, "Both cursor members were found in the interface map.");
        }

        // ── Storage that assigns no sequences: the cursor cannot be honoured ────────────────────────

        [Test]
        public void Unsequenced_QuerySince_ReturnsWhatQueryReturns()
        {
            ILogStorage storage = Unsequenced(12);

            var expected = storage.Query(maxEntries: 4, logTypeFilter: LogType.Log);
            var actual = storage.QuerySince(5, maxEntries: 4, logTypeFilter: LogType.Log);

            Assert.AreEqual(4, expected.Length, "Fixture: more matching entries than the limit.");
            CollectionAssert.AreEqual(Messages(expected), Messages(actual), "The newest page, as sinceSequence=0 would return.");
            Assert.IsTrue(actual.All(e => e.Sequence == 0));
            Assert.IsTrue(actual.All(e => e.StackTrace == null), "includeStackTrace is passed through.");
        }

        [Test]
        public void Unsequenced_QuerySinceAsync_ReturnsWhatQueryReturns()
        {
            ILogStorage storage = Unsequenced(12);

            // Unity Test Framework 1.1 has no async [Test]; LegacyLogStorage completes synchronously.
            var expected = storage.QueryAsync(maxEntries: 4, includeStackTrace: true).GetAwaiter().GetResult();
            var actual = storage.QuerySinceAsync(5, maxEntries: 4, includeStackTrace: true).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(Messages(expected), Messages(actual));
            Assert.IsTrue(actual.All(e => e.StackTrace != null), "includeStackTrace is passed through.");
        }

        [Test]
        public void Unsequenced_QuerySince_Unfiltered_ReadsTheStorageOnce()
        {
            var storage = Unsequenced(12);
            ILogStorage asInterface = storage;

            var expected = asInterface.Query(maxEntries: 4);
            storage.QueryCalls = 0;
            var actual = asInterface.QuerySince(5, maxEntries: 4);
            var syncCalls = storage.QueryCalls;
            storage.QueryCalls = 0;
            var actualAsync = asInterface.QuerySinceAsync(5, maxEntries: 4).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(Messages(expected), Messages(actual));
            CollectionAssert.AreEqual(Messages(expected), Messages(actualAsync));
            Assert.AreEqual(1, syncCalls, "The first unfiltered read already is Query's answer; QuerySince must not re-read.");
            Assert.AreEqual(1, storage.QueryCalls, "The first unfiltered read already is QueryAsync's answer; QuerySinceAsync must not re-read.");
        }

        [Test]
        public void Empty_QuerySince_ReturnsEmpty()
        {
            ILogStorage storage = new LegacyLogStorage();
            Assert.IsEmpty(storage.QuerySince(1));
            Assert.IsEmpty(storage.QuerySince(0));
        }

        // ── Storage whose entries carry sequences: the default honours the cursor contract ─────────

        [Test]
        public void Sequenced_QuerySince_ReturnsTheOldestPageAboveTheCursor()
        {
            ILogStorage storage = Sequenced(40);

            // 40 entries, cursor 1, limit 2: the default has to widen its window past 2, 4, 8, 16, 32 to reach the
            // cursor, and must then return the OLDEST matches, not the newest.
            var page = storage.QuerySince(1, maxEntries: 2, logTypeFilter: LogType.Warning);

            CollectionAssert.AreEqual(new long[] { 3, 6 }, page.Select(e => e.Sequence));
        }

        [Test]
        public void Sequenced_QuerySince_PagesJoinWithoutGapsOrDuplicates()
        {
            ILogStorage storage = Sequenced(40);
            var expected = Enumerable.Range(1, 40).Where(i => i % 3 == 0).Select(i => (long)i).ToArray();

            var seen = new List<long>();
            long cursor = 0;
            for (var guard = 0; guard < 100; guard++)
            {
                var page = storage.QuerySince(cursor, maxEntries: 4, logTypeFilter: LogType.Warning);
                if (page.Length == 0)
                    break;
                Assert.LessOrEqual(page.Length, 4, "A page never exceeds maxEntries.");
                CollectionAssert.IsOrdered(page.Select(e => e.Sequence), "Oldest first.");
                seen.AddRange(page.Select(e => e.Sequence));
                cursor = page.Max(e => e.Sequence);
            }

            CollectionAssert.AreEqual(expected, seen);
        }

        [Test]
        public void Sequenced_QuerySince_CursorAtHighest_ReturnsNothing()
        {
            ILogStorage storage = Sequenced(10);
            Assert.IsEmpty(storage.QuerySince(10));
        }

        [Test]
        public void Sequenced_QuerySince_CursorAboveEverything_ReadsFromTheBeginning()
        {
            ILogStorage storage = Sequenced(10);
            var page = storage.QuerySince(1_000, maxEntries: 3);
            CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, page.Select(e => e.Sequence));
        }

        [Test]
        public void Sequenced_QuerySinceAsync_MatchesQuerySince()
        {
            ILogStorage storage = Sequenced(40);

            var viaSync = storage.QuerySince(7, maxEntries: 5);
            var viaAsync = storage.QuerySinceAsync(7, maxEntries: 5).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new long[] { 8, 9, 10, 11, 12 }, viaSync.Select(e => e.Sequence));
            CollectionAssert.AreEqual(viaSync.Select(e => e.Sequence), viaAsync.Select(e => e.Sequence));
        }

        // ── The tool, end to end, against a legacy storage ──────────────────────────────────────────

        [Test]
        public void Tool_SinceSequence_WorksAgainstALegacyStorage()
        {
            var plugin = UnityMcpPluginEditor.Instance;
            var hadCollector = plugin.LogCollector != null;
            var marker = Guid.NewGuid().ToString("N");

            plugin.DisposeLogCollector();
            try
            {
                var storage = new LegacyLogStorage();
                plugin.AddUnityLogCollector(storage);

                Debug.Log($"legacy-storage {marker}");

                LogEntry[] result = Array.Empty<LogEntry>();
                Assert.DoesNotThrow(() => result = new Tool_Console().GetLogs(maxEntries: 1000, sinceSequence: 5));

                Assert.IsTrue(result.Any(e => e.Message.Contains($"legacy-storage {marker}")),
                    "Unsequenced storage: the newest page is returned.");
                Assert.IsTrue(result.All(e => e.Sequence == 0));
                Assert.Greater(storage.QueryCalls, 0, "The tool reached the legacy storage through the interface default.");
            }
            finally
            {
                plugin.DisposeLogCollector();
                if (hadCollector)
                    plugin.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
            }
        }
    }
}
