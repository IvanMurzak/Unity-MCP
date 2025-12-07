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
        private LogUtils _logUtils;

        [SetUp]
        public void TestSetUp()
        {
            _logUtils = new LogUtils("test-editor-logs.txt");
            _logUtils.ClearCacheFile();
        }

        [TearDown]
        public void TestTearDown()
        {
            _logUtils.Dispose();
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_PreservesAllLogTypes()
        {
            // Test that all Unity log types are preserved during save/load
            _logUtils.ClearLogs();
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
            yield return WaitForTask(_logUtils.SaveToFile());

            // Clear and reload
            _logUtils.ClearLogs(false);
            Assert.AreEqual(0, _logUtils.LogEntries);

            yield return WaitForTask(_logUtils.LoadFromFile());

            var loadedLogs = _logUtils.GetAllLogs();
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
            // Test that special characters, unicode, and formatting are preserved
            _logUtils.ClearLogs();
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
            yield return WaitForTask(_logUtils.SaveToFile());
            _logUtils.ClearLogs(false);
            yield return WaitForTask(_logUtils.LoadFromFile());

            var loadedLogs = _logUtils.GetAllLogs();
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
            // Save original stack trace settings
            var originalWarningStackTrace = Application.GetStackTraceLogType(LogType.Warning);

            try
            {
                // Enable stack traces for warning logs (we can't use Error/Assert as they fail tests)
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);

                // Test that stack traces are preserved
                _logUtils.ClearLogs();
                yield return null;

                // Generate logs with stack traces (only warnings, as errors/assertions fail tests)
                Debug.LogWarning("Warning with stack trace 1");
                Debug.LogWarning("Warning with stack trace 2");
                Debug.LogWarning("Warning with stack trace 3");

                const int expectedLogs = 3;
                yield return WaitForLogCount(expectedLogs);

                var originalLogs = _logUtils.GetAllLogs();
                Assert.AreEqual(expectedLogs, originalLogs.Length);

                // Verify original logs have stack traces
                foreach (var log in originalLogs)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(log.StackTrace),
                        $"Original log should have stack trace: {log.Message}");
                }

                // Save and reload
                yield return WaitForTask(_logUtils.SaveToFile());
                _logUtils.ClearLogs(false);
                yield return WaitForTask(_logUtils.LoadFromFile());

                var loadedLogs = _logUtils.GetAllLogs();
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
            // Test that timestamps are preserved with accuracy
            _logUtils.ClearLogs();
            yield return null;

            const int testCount = 5;
            for (int i = 0; i < testCount; i++)
            {
                Debug.Log($"Timestamp test {i}");
            }

            yield return WaitForLogCount(testCount);

            var originalLogs = _logUtils.GetAllLogs();
            var originalTimestamps = originalLogs.Select(log => log.Timestamp).ToArray();

            // Save and reload
            yield return WaitForTask(_logUtils.SaveToFile());
            _logUtils.ClearLogs(false);
            yield return WaitForTask(_logUtils.LoadFromFile());

            var loadedLogs = _logUtils.GetAllLogs();
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
            // Test saving/loading when there are no logs
            _logUtils.ClearLogs();
            yield return null;

            Assert.AreEqual(0, _logUtils.LogEntries);

            // Save empty logs
            yield return WaitForTask(_logUtils.SaveToFile());

            // Try to load (should result in empty logs)
            yield return WaitForTask(_logUtils.LoadFromFile());

            Assert.AreEqual(0, _logUtils.LogEntries, "Loading empty logs should result in zero entries");
            Assert.AreEqual(0, _logUtils.GetAllLogs().Length);
        }

        [UnityTest]
        public IEnumerator SaveToFile_LoadFromFile_HandlesLargeMessages()
        {
            // Test very long log messages
            _logUtils.ClearLogs();
            yield return null;

            var largeMessage = new string('A', 10000); // 10KB message
            var mediumMessage = new string('B', 1000);  // 1KB message

            Debug.Log(largeMessage);
            Debug.Log(mediumMessage);
            Debug.Log("Small message");

            const int expectedLogs = 3;
            yield return WaitForLogCount(expectedLogs);



            // Save and reload
            yield return WaitForTask(_logUtils.SaveToFile());
            _logUtils.ClearLogs(false);
            yield return WaitForTask(_logUtils.LoadFromFile());

            var loadedLogs = _logUtils.GetAllLogs();
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
            // Test multiple save/load cycles to ensure data integrity over time
            _logUtils.ClearLogs();
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
                yield return WaitForTask(_logUtils.SaveToFile());

                // Verify count before clearing
                Assert.AreEqual((cycle + 1) * logsPerCycle, _logUtils.LogEntries,
                    $"Should have {(cycle + 1) * logsPerCycle} logs after cycle {cycle}");

                // Clear and reload
                _logUtils.ClearLogs(false);
                yield return WaitForTask(_logUtils.LoadFromFile());

                // Verify all logs from all cycles are still present
                var loadedLogs = _logUtils.GetAllLogs();
                Assert.AreEqual((cycle + 1) * logsPerCycle, loadedLogs.Length,
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
            // Test that log order is preserved
            _logUtils.ClearLogs();
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
            yield return WaitForTask(_logUtils.SaveToFile());
            _logUtils.ClearLogs(false);
            yield return WaitForTask(_logUtils.LoadFromFile());

            var loadedLogs = _logUtils.GetAllLogs();
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
            // Test synchronous save
            _logUtils.ClearLogs();

            Debug.Log("Immediate save test");

            // Since this is a synchronous test, we can't easily wait for the log callback if it's delayed.
            // But we can verify that the method executes without throwing exceptions.
            Assert.DoesNotThrow(() => _logUtils.SaveToFileImmediate());
        }

        [UnityTest]
        public IEnumerator ClearLogs_RemovesAllLogs()
        {
            const int logsCount = 10;
            // Test that ClearLogs actually removes all logs
            _logUtils.ClearLogs();
            yield return null;

            // Add some logs
            for (int i = 0; i < logsCount; i++)
            {
                Debug.Log($"Test log {i}");
            }

            yield return WaitForLogCount(logsCount);
            Assert.AreEqual(logsCount, _logUtils.LogEntries);

            // Clear logs
            _logUtils.ClearLogs();

            Assert.AreEqual(0, _logUtils.LogEntries, "LogEntries should be zero after clear");
            Assert.AreEqual(0, _logUtils.GetAllLogs().Length, "GetAllLogs should return empty array after clear");
        }

        #region Helper Methods

        private IEnumerator WaitForLogCount(int expectedCount)
        {
            var frameCount = 0;
            while (_logUtils.LogEntries < expectedCount)
            {
                yield return null;
                frameCount++;
                Assert.Less(frameCount, Timeout,
                    $"Timeout waiting for {expectedCount} logs. Current count: {_logUtils.LogEntries}");
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

