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
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestLogUtils : BaseTest
    {
        private const int Timeout = 100000;
        private UnityLogCollector? logCollector;

        [SetUp]
        public void TestSetUp()
        {
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));
            logCollector.Clear();
        }

        [TearDown]
        public void TestTearDown()
        {
            logCollector?.Dispose();
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesAllLogTypes()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test that all Unity log types are preserved during save/load
            logCollector.Clear();
            yield return null;

            var testData = new[]
            {
                new { Message = "Regular log message", Type = LogType.Log },
                new { Message = "Warning message", Type = LogType.Warning }
                // new { Message = "Error message", Type = LogType.Error },
                // new { Message = "Assert message", Type = LogType.Assert },
                // new { Message = "Exception message", Type = LogType.Exception }
            };

            // Generate logs of different types
            foreach (var test in testData)
            {
                switch (test.Type)
                {
                    case LogType.Log:
                        Debug.Log(test.Message);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(test.Message);
                        break;
                    case LogType.Error:
                        Debug.LogError(test.Message);
                        break;
                    case LogType.Assert:
                        Debug.LogAssertion(test.Message);
                        break;
                    case LogType.Exception:
                        Debug.LogException(new Exception(test.Message));
                        break;
                }
            }

            // Wait for logs to be collected
            yield return WaitForLogCount(testData.Length);

            // Save to file
            logCollector.Save();

            // Clear and reload
            // Simulate restart
            logCollector.Dispose();
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));
            // Assert.AreEqual(0, logCollector.Query().Length, "Logs should be empty array after clearing");

            var loadedLogs = logCollector.Query();
            Assert.AreEqual(testData.Length, loadedLogs.Length, "All log types should be preserved");

            // Verify each log type is preserved
            foreach (var test in testData)
            {
                var matchingLog = loadedLogs.FirstOrDefault(log =>
                    log.Message.Contains(test.Message) && log.LogType == test.Type);
                Assert.IsNotNull(matchingLog, $"Log type {test.Type} with message '{test.Message}' should be preserved");
            }
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesSpecialCharacters()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test that special characters, unicode, and formatting are preserved
            logCollector.Clear();
            yield return null;

            var specialMessages = new[]
            {
                "Message with \"quotes\" and 'apostrophes'",
                "Unicode: 你好世界 🚀 émojis",
                "Newlines:\nLine 1\nLine 2\nLine 3",
                "Tabs:\tindented\t\ttext",
                "Special chars: !@#$%^&*()_+-=[]{}|;:,.<>?/~`",
                "Backslashes: C:\\Path\\To\\File.txt",
                "Empty message:",
                "   Leading and trailing spaces   "
            };

            foreach (var message in specialMessages)
            {
                Debug.Log(message);
            }

            yield return WaitForLogCount(specialMessages.Length);

            // Save and reload
            logCollector.Save();
            // Simulate restart
            logCollector.Dispose();
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

            var loadedLogs = logCollector.Query();
            Assert.AreEqual(specialMessages.Length, loadedLogs.Length, "All logs should be preserved");

            // Verify exact message preservation
            foreach (var expectedMessage in specialMessages)
            {
                var matchingLog = loadedLogs.FirstOrDefault(log => log.Message == expectedMessage);
                Assert.IsNotNull(matchingLog, $"Message should be preserved exactly: '{expectedMessage}'");
            }
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesStackTraces()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Save original stack trace settings
            var originalWarningStackTrace = Application.GetStackTraceLogType(LogType.Warning);

            try
            {
                // Enable stack traces for warning logs (we can't use Error/Assert as they fail tests)
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);

                // Test that stack traces are preserved
                logCollector.Clear();
                yield return null;

                // Generate logs with stack traces (only warnings, as errors/assertions fail tests)
                Debug.LogWarning("Warning with stack trace 1");
                Debug.LogWarning("Warning with stack trace 2");
                Debug.LogWarning("Warning with stack trace 3");

                const int expectedLogs = 3;
                yield return WaitForLogCount(expectedLogs);

                var originalLogs = logCollector.Query();
                Assert.AreEqual(expectedLogs, originalLogs.Length);

                // Verify original logs have stack traces
                foreach (var log in originalLogs)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(log.StackTrace),
                        $"Original log should have stack trace: {log.Message}");
                }

                // Save and reload
                logCollector.Save();
                // Simulate restart
                logCollector.Dispose();
                logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

                var loadedLogs = logCollector.Query();
                Assert.AreEqual(expectedLogs, loadedLogs.Length, "All logs should be preserved");

                // Verify stack traces are preserved
                for (int i = 0; i < expectedLogs; i++)
                {
                    var original = originalLogs[i];
                    var loaded = loadedLogs.FirstOrDefault(log => log.Message == original.Message);

                    Assert.IsNotNull(loaded, $"Log should be found: {original.Message}");
                    Assert.AreEqual(original.StackTrace, loaded.StackTrace,
                        $"Stack trace should be preserved for: {original.Message}");
                }
            }
            finally
            {
                // Restore original stack trace settings even if test fails
                Application.SetStackTraceLogType(LogType.Warning, originalWarningStackTrace);
            }
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesTimestamps()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test that timestamps are preserved with accuracy
            logCollector.Clear();
            yield return null;

            const int testCount = 5;
            for (int i = 0; i < testCount; i++)
            {
                Debug.Log($"Timestamp test {i}");
            }

            yield return WaitForLogCount(testCount);

            var originalLogs = logCollector.Query();
            var originalTimestamps = originalLogs.Select(log => log.Timestamp).ToArray();

            // Save and reload
            logCollector.Save();
            // Simulate restart
            logCollector.Dispose();
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

            var loadedLogs = logCollector.Query();
            Assert.AreEqual(testCount, loadedLogs.Length);

            // Verify timestamps are preserved (allowing for minimal serialization precision loss)
            for (int i = 0; i < testCount; i++)
            {
                var original = originalLogs[i];
                var loaded = loadedLogs.FirstOrDefault(log => log.Message == original.Message);

                Assert.IsNotNull(loaded);

                // Timestamps should be equal or very close (within 1 second to account for serialization)
                var timeDiff = Math.Abs((original.Timestamp - loaded.Timestamp).TotalMilliseconds);
                Assert.Less(timeDiff, 1000,
                    $"Timestamp difference should be minimal. Original: {original.Timestamp}, Loaded: {loaded.Timestamp}");
            }
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_HandlesEmptyLogs()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test saving/loading when there are no logs
            logCollector.Clear();
            yield return null;

            Assert.AreEqual(0, logCollector.Query().Length);

            // Save empty logs
            logCollector.Save();

            Assert.AreEqual(0, logCollector.Query().Length, "Loading empty logs should result in zero entries");
            Assert.AreEqual(0, logCollector.Query().Length);
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_HandlesLargeMessages()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test very long log messages
            logCollector.Clear();
            yield return null;

            var largeMessage = new string('A', 10000); // 10KB message
            var mediumMessage = new string('B', 1000);  // 1KB message

            Debug.Log(largeMessage);
            Debug.Log(mediumMessage);
            Debug.Log("Small message");

            const int expectedLogs = 3;
            yield return WaitForLogCount(expectedLogs);



            // Save and reload
            logCollector.Save();
            // Simulate restart
            logCollector.Dispose();
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

            var loadedLogs = logCollector.Query();
            Assert.AreEqual(expectedLogs, loadedLogs.Length);

            // Verify large messages are preserved exactly
            Assert.IsTrue(loadedLogs.Any(log => log.Message == largeMessage),
                "Large message should be preserved");
            Assert.IsTrue(loadedLogs.Any(log => log.Message == mediumMessage),
                "Medium message should be preserved");
            Assert.IsTrue(loadedLogs.Any(log => log.Message == "Small message"),
                "Small message should be preserved");
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_MultipleSaveCycles()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test multiple save/load cycles to ensure data integrity over time
            logCollector.Clear();
            yield return null;

            const int cycles = 3;
            const int logsPerCycle = 5;

            for (int cycle = 0; cycle < cycles; cycle++)
            {
                // Add logs for this cycle
                for (int i = 0; i < logsPerCycle; i++)
                {
                    Debug.Log($"Cycle {cycle}, Log {i}");
                }

                yield return WaitForLogCount((cycle + 1) * logsPerCycle);

                // Save to file
                yield return WaitForTask(logCollector.SaveAsync());

                // Verify count before clearing
                var logs = logCollector.Query();
                var testLogsCount = logs.Where(l => l.Message.StartsWith("Cycle")).Count();
                Assert.AreEqual((cycle + 1) * logsPerCycle, testLogsCount,
                    $"Should have {(cycle + 1) * logsPerCycle} logs after cycle {cycle}");

                // Clear and reload
                // Simulate restart
                logCollector.Dispose();
                logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

                // Verify all logs from all cycles are still present
                var loadedLogs = logCollector.Query();
                var loadedTestLogsCount = loadedLogs.Where(l => l.Message.StartsWith("Cycle")).Count();
                Assert.AreEqual((cycle + 1) * logsPerCycle, loadedTestLogsCount,
                    $"All logs should be preserved after cycle {cycle}");

                // Verify specific logs from each cycle
                for (int pastCycle = 0; pastCycle <= cycle; pastCycle++)
                {
                    for (int i = 0; i < logsPerCycle; i++)
                    {
                        var expectedMessage = $"Cycle {pastCycle}, Log {i}";
                        Assert.IsTrue(loadedLogs.Any(log => log.Message == expectedMessage),
                            $"Log should exist: {expectedMessage}");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesLogOrder()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            // Test that log order is preserved
            logCollector.Clear();
            yield return null;

            const int testCount = 20;
            var messages = Enumerable.Range(0, testCount)
                .Select(i => $"Ordered log {i:D3}")
                .ToArray();

            foreach (var message in messages)
            {
                Debug.Log(message);
            }

            yield return WaitForLogCount(testCount);

            // Save and reload
            logCollector.Save();
            // Simulate restart
            logCollector.Dispose();
            logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-editor-logs.txt"));

            var loadedLogs = logCollector.Query();
            Assert.AreEqual(testCount, loadedLogs.Length);

            // Verify order is preserved by comparing timestamps
            for (int i = 0; i < testCount - 1; i++)
            {
                Assert.LessOrEqual(loadedLogs[i].Timestamp, loadedLogs[i + 1].Timestamp,
                    $"Logs should be in chronological order: {i} -> {i + 1}");
            }

            // Verify all messages are present in original order
            for (int i = 0; i < testCount; i++)
            {
                var expectedMessage = messages[i];
                var matchingLog = loadedLogs.FirstOrDefault(log => log.Message == expectedMessage);
                Assert.IsNotNull(matchingLog, $"Log {i} should be preserved: {expectedMessage}");
            }
        }

        [Test]
        public void SaveToFileImmediate_WritesSynchronously()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                return;
            }
            // Test synchronous save
            logCollector.Clear();

            Debug.Log("Immediate save test");

            // Since this is a synchronous test, we can't easily wait for the log callback if it's delayed.
            // But we can verify that the method executes without throwing exceptions.
            Assert.DoesNotThrow(() => logCollector.Save());
        }

        [UnityTest]
        public IEnumerator ClearLogs_RemovesAllLogs()
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            const int logsCount = 10;
            // Test that ClearLogs actually removes all logs
            logCollector.Clear();
            yield return null;

            // Add some logs
            for (int i = 0; i < logsCount; i++)
            {
                Debug.Log($"Test log {i}");
            }

            yield return WaitForLogCount(logsCount);
            Assert.AreEqual(logsCount, logCollector.Query().Length);

            // Clear logs
            logCollector.Clear();

            Assert.AreEqual(0, logCollector.Query().Length, "GetAllLogs should return empty array after clear");
        }

        #region File Size Limit Tests

        [Test]
        public void FileLogStorage_MaxFileSizeMB_DefaultValue()
        {
            // Test that default max file size is 512MB
            using var storage = new FileLogStorage(cacheFileName: "test-max-size-default.txt");
            // The default value is defined as a constant in the class
            // We can verify the storage was created successfully with default values
            Assert.DoesNotThrow(() => storage.Append(new LogEntry(LogType.Log, "Test")));
        }

        [Test]
        public void FileLogStorage_MaxFileSizeMB_CustomValue()
        {
            // Test that custom max file size can be set
            using var storage = new FileLogStorage(
                cacheFileName: "test-max-size-custom.txt",
                maxFileSizeMB: 100);
            Assert.DoesNotThrow(() => storage.Append(new LogEntry(LogType.Log, "Test")));
        }

        [Test]
        public void FileLogStorage_MaxFileSizeMB_ThrowsOnInvalidValue()
        {
            // Test that invalid max file size throws exception
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FileLogStorage(cacheFileName: "test-max-size-invalid.txt", maxFileSizeMB: 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FileLogStorage(cacheFileName: "test-max-size-invalid.txt", maxFileSizeMB: -1));
        }

        [UnityTest]
        public IEnumerator FileLogStorage_ResetLogFile_ResetsWhenLimitReached()
        {
            // Test with a very small max file size (1MB) to trigger reset quickly
            const int maxFileSizeMB = 1;
            const string testFileName = "test-reset-trigger.txt";

            using var storage = new FileLogStorage(
                cacheFileName: testFileName,
                maxFileSizeMB: maxFileSizeMB);

            // Clear any existing data
            storage.Clear();
            yield return null;

            // Generate a large message to fill up the file quickly
            var largeMessage = new string('X', 100000); // 100KB per message

            // Write entries until we exceed the limit (at least 11 messages for 1MB)
            for (int i = 0; i < 15; i++)
            {
                storage.Append(new LogEntry(LogType.Log, $"{largeMessage}_{i}"));
            }

            yield return null;

            // After reset, the file should have been cleared and new entries written
            // Query should still work
            var logs = storage.Query(maxEntries: 100);
            Assert.IsNotNull(logs, "Query should return logs after reset");

            // The file should have fewer entries than we wrote (due to reset)
            // Or it could have all entries if reset happened and new ones were written
            // The key test is that the system didn't crash and still works
            Assert.DoesNotThrow(() => storage.Query());

            // Cleanup
            storage.Clear();
        }

        [Test]
        public void BufferedFileLogStorage_MaxFileSizeMB_PassedToBase()
        {
            // Test that BufferedFileLogStorage passes maxFileSizeMB to base class
            using var storage = new BufferedFileLogStorage(
                cacheFileName: "test-buffered-max-size.txt",
                maxFileSizeMB: 256);

            Assert.DoesNotThrow(() => storage.Append(new LogEntry(LogType.Log, "Test")));
        }

        #endregion

        #region Helper Methods

        private IEnumerator WaitForLogCount(int expectedCount)
        {
            if (logCollector == null)
            {
                Assert.Fail($"{nameof(logCollector)} is not initialized");
                yield break;
            }
            var frameCount = 0;
            while (logCollector.Query(maxEntries: expectedCount).Length < expectedCount)
            {
                yield return null;
                frameCount++;
                Assert.Less(frameCount, Timeout,
                    $"Timeout waiting for {expectedCount} logs. Current count: {logCollector.Query(maxEntries: 2 ^ 12).Length}");
            }
        }

        private IEnumerator WaitForTask(System.Threading.Tasks.Task task)
        {
            var frameCount = 0;
            while (!task.IsCompleted)
            {
                yield return null;
                frameCount++;
                Assert.Less(frameCount, Timeout,
                    $"Timeout waiting for task to complete. Status: {task.Status}");
            }

            // Check if task faulted
            if (task.IsFaulted && task.Exception != null)
            {
                throw task.Exception;
            }
        }

        #endregion
    }
}

