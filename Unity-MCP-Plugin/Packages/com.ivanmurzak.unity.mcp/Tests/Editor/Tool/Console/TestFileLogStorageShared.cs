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
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression cover for #855 — Console/ClearLogs failing with
    /// "The process cannot access the file ... because it is being used by another process".
    ///
    /// Unity launches Asset Import Workers as headless Editor processes sharing the main Editor's
    /// <c>-projectPath</c>, so each resolves the SAME <c>Temp/mcp-server/ai-editor-logs.txt</c> and
    /// keeps a write handle on it. <see cref="UnityMcpPlugin.AddUnityLogCollectorIfNeeded"/> now skips
    /// workers, but any second holder must still not be able to wedge <see cref="FileLogStorage.Clear"/> —
    /// that is what these tests pin.
    /// </summary>
    public class TestFileLogStorageShared : BaseTest
    {
        const string FileName = "test-file-log-storage-shared.txt";

        FileLogStorage? _primary;
        FileLogStorage? _foreign;

        [TearDown]
        public void TestTearDown()
        {
            _foreign?.Dispose();
            _primary?.Dispose();
            _foreign = null;
            _primary = null;
        }

        [Test]
        public void Clear_SucceedsWhileASecondStorageHoldsTheSameFile()
        {
            // Two storages over one path stand in for two Unity processes on one project. Before the
            // FileShare.Delete fix this threw IOException: the primary disposed only ITS OWN stream,
            // and Windows refuses DeleteFile while any surviving handle omits FILE_SHARE_DELETE.
            _primary = new FileLogStorage(requestedFileName: FileName);
            _foreign = new FileLogStorage(requestedFileName: FileName);

            _primary.Append(new LogEntry(LogType.Log, "from primary"));
            _foreign.Append(new LogEntry(LogType.Log, "from foreign"));

            Assert.DoesNotThrow(() => _primary!.Clear());
        }

        [Test]
        public void Clear_RemovesTheCachedEntriesWhileASecondStorageHoldsTheSameFile()
        {
            // Not-throwing is not enough: the point of Clear is that the cache is actually emptied,
            // so a later console-get-logs cannot resurrect pre-Clear entries.
            _primary = new FileLogStorage(requestedFileName: FileName);
            _foreign = new FileLogStorage(requestedFileName: FileName);

            _primary.Append(new LogEntry(LogType.Log, "before clear"));
            Assert.IsNotEmpty(_primary.Query(), "Precondition: the appended entry must be queryable.");

            _primary.Clear();

            Assert.IsEmpty(_primary.Query(), "Clear must leave no queryable entries behind.");
        }

        [Test]
        public void Clear_LeavesTheStorageWritableAfterwards()
        {
            // Clear disposes the write stream and nulls it; AppendInternal is expected to lazily
            // recreate it. A regression here would silently stop log collection after the first Clear.
            _primary = new FileLogStorage(requestedFileName: FileName);
            _foreign = new FileLogStorage(requestedFileName: FileName);

            _primary.Clear();
            _primary.Append(new LogEntry(LogType.Warning, "after clear"));

            var entries = _primary.Query();
            Assert.AreEqual(1, entries.Length, "Exactly the post-Clear entry should be present.");
            Assert.AreEqual("after clear", entries[0].Message);
        }

        [Test]
        public void SecondStorage_SharesTheSamePathRatherThanForkingANewFile()
        {
            // Pins the assumption the tests above rest on: the two storages really do collide on one
            // file. CreateWriteStream carries a uniquifying retry loop ("-2", "-3", ...) that only runs
            // when the open THROWS, and the share mode lets a second writer straight in — so the loop
            // never fires and no sibling appears. Should that ever change, the collision these tests
            // model would vanish and they would start passing vacuously.
            _primary = new FileLogStorage(requestedFileName: FileName);
            _foreign = new FileLogStorage(requestedFileName: FileName);

            _primary.Append(new LogEntry(LogType.Log, "primary"));
            _foreign.Append(new LogEntry(LogType.Log, "foreign"));

            var forks = FindForkedFiles();
            Assert.IsEmpty(forks,
                "Both storages must land on one file for this fixture to be meaningful. " +
                $"Unexpected uniquified sibling(s): {string.Join(", ", forks)}");
        }

        static string[] FindForkedFiles()
        {
            var directory = Path.GetFullPath(
                $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server");
            if (!Directory.Exists(directory))
                return System.Array.Empty<string>();

            var baseName = Path.GetFileNameWithoutExtension(FileName);
            var extension = Path.GetExtension(FileName);
            return Directory.GetFiles(directory, $"{baseName}-*{extension}");
        }
    }
}
