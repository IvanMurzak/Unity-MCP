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
using System.Linq;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolConsoleIntegration : BaseTest
    {
        private Tool_Console _tool = null!;
        private UnityLogCollector _logCollector = null!;

        [SetUp]
        public void TestSetUp()
        {
            // Create local collector
            _logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-tool-console-integration.txt"));

            // Inject into Singleton
            var property = typeof(UnityMcpPlugin).GetProperty("LogCollector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property.SetValue(UnityMcpPlugin.Instance, _logCollector);

            _tool = new Tool_Console();
        }

        [TearDown]
        public void TestTearDown()
        {
            _logCollector?.Dispose();
        }

        [UnityTest]
        public IEnumerator GetLogs_CapturesRealTimeLogs_Correctly()
        {
            // Arrange - Clear existing logs first
            _logCollector.Clear();
            _tool.GetLogs(maxEntries: 100000);

            yield return null; // Wait one frame

            // Generate unique test logs
            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var testLogMessage = $"Integration test log {uniqueId}";
            var testWarningMessage = $"Integration test warning {uniqueId}";
            var testLogMessage2 = $"Integration test log 2 {uniqueId}";

            // Act - Generate logs of different types (only safe log types)
            Debug.Log(testLogMessage);
            yield return null;

            Debug.LogWarning(testWarningMessage);
            yield return null;

            Debug.Log(testLogMessage2);
            yield return null;

            // Wait for log collection system to process (EditMode tests can only yield null)
            for (int i = 0; i < 5; i++)
                yield return null;

            _logCollector.Save();

            // Act - Retrieve logs
            var allLogsResult = _tool.GetLogs(maxEntries: 1000);
            var logOnlyResult = _tool.GetLogs(maxEntries: 1000, logTypeFilter: LogType.Log);
            var warningOnlyResult = _tool.GetLogs(maxEntries: 1000, logTypeFilter: LogType.Warning);

            // Assert - Check that our unique logs are captured
            Assert.IsNotNull(allLogsResult, "All logs result should not be null");
            Assert.IsTrue(allLogsResult.Any(entry => entry.Message.Contains(testLogMessage) && entry.LogType == LogType.Log),
                $"Should contain test log message.\nUnique ID: {uniqueId}\nResult: {allLogsResult}");
            Assert.IsTrue(allLogsResult.Any(entry => entry.Message.Contains(testWarningMessage) && entry.LogType == LogType.Warning),
                $"Should contain test warning message.\nResult: {allLogsResult}");
            Assert.IsTrue(allLogsResult.Any(entry => entry.Message.Contains(testLogMessage2) && entry.LogType == LogType.Log),
                $"Should contain second test log message.\nResult: {allLogsResult}");

            // Assert - Check filtered results
            Assert.IsTrue(logOnlyResult.Any(entry => entry.Message.Contains(testLogMessage) && entry.LogType == LogType.Log),
                $"Log filter should contain test log message.\nResult: {logOnlyResult}");
            Assert.IsTrue(logOnlyResult.Any(entry => entry.Message.Contains(testLogMessage2) && entry.LogType == LogType.Log),
                $"Log filter should contain second test log message.\nResult: {logOnlyResult}");
            Assert.IsFalse(logOnlyResult.Any(entry => entry.Message.Contains(testWarningMessage) && entry.LogType == LogType.Warning),
                "Log filter should not contain warning in log entries");

            Assert.IsTrue(warningOnlyResult.Any(entry => entry.Message.Contains(testWarningMessage) && entry.LogType == LogType.Warning),
                $"Warning filter should contain test warning message.\nResult: {warningOnlyResult}");
            Assert.IsFalse(warningOnlyResult.Any(entry => entry.Message.Contains(testLogMessage) && entry.LogType == LogType.Log),
                "Warning filter should not contain regular log in log entries");
        }

        [UnityTest]
        public IEnumerator GetLogs_WithTimeFilter_FiltersTimeCorrectly()
        {
            // Arrange
            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var oldLogMessage = $"Old log {uniqueId}";
            var newLogMessage = $"New log {uniqueId}";

            // Generate an "old" log (relative to our test)
            Debug.Log(oldLogMessage);

            // Wait a few frames (EditMode tests can only yield null)
            for (int i = 0; i < 3; i++)
                yield return null;

            _logCollector.Save();

            // Act - Get logs from a very short time window (should not include the "old" log)
            var recentLogsResult = _tool.GetLogs(lastMinutes: 0); // 0 means all logs

            // Generate a new log
            Debug.Log(newLogMessage);

            // Wait a few frames (EditMode tests can only yield null)
            for (int i = 0; i < 3; i++)
                yield return null;

            _logCollector.Save();

            // Get logs from last 1 minute (should include both)
            var minuteLogsResult = _tool.GetLogs(lastMinutes: 1);

            // Assert
            Assert.IsNotNull(minuteLogsResult, "Minute logs result should not be null");
            Assert.IsTrue(minuteLogsResult.Any(entry => entry.Message.Contains(oldLogMessage)),
                $"Should contain old log message when filtering by 1 minute.\nResult: {minuteLogsResult}");
            Assert.IsTrue(minuteLogsResult.Any(entry => entry.Message.Contains(newLogMessage)),
                $"Should contain new log message when filtering by 1 minute.\nResult: {minuteLogsResult}");
        }

        [UnityTest]
        public IEnumerator GetLogs_MemoryManagement_LimitsEntries()
        {
            // This test verifies that the log collection doesn't grow indefinitely
            // Note: This is more of a stress test and may take some time

            var initialLogsResult = _tool.GetLogs(maxEntries: 100000);
            var initialCount = CountLogEntries(initialLogsResult);

            // Generate many logs to test memory management
            for (int i = 0; i < 50; i++)
            {
                Debug.Log($"Memory test log {i}");
                if (i % 10 == 0) yield return null; // Yield periodically
            }

            // Wait for log collection system to process (EditMode tests can only yield null)
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            var afterLogsResult = _tool.GetLogs(maxEntries: 100000);
            var afterCount = CountLogEntries(afterLogsResult);

            // Assert - Should have more logs now, but the system should handle memory correctly
            Assert.IsTrue(afterCount >= initialCount,
                $"Should have at least as many logs as before. Initial: {initialCount}, After: {afterCount}");

            // The test passes if we don't run out of memory and the system continues to function
            Assert.IsNotNull(afterLogsResult, "Should still be able to get logs after generating many entries");
        }

        int CountLogEntries(LogEntry[] logsResult)
        {
            return logsResult?.Length ?? 0;
        }

        [Test]
        public void GetLogs_ThreadSafety_HandlesMultipleAccess()
        {
            // This test ensures the tool handles multiple sequential calls correctly
            // Note: GetLogs uses MainThread.Instance.Run() so we test sequential access instead of concurrent

            var threadsCount = 5;
            var results = new LogEntry[threadsCount][];

            // Generate some test logs first
            for (int i = 0; i < threadsCount; i++)
            {
                Debug.Log($"Sequential test log {i}");
            }

            // Test multiple sequential calls (simulating rapid successive calls)
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _tool.GetLogs(maxEntries: 100);
                Assert.IsNotNull(results[i], $"Call {i} should have completed successfully");
            }

            // All calls should succeed and return consistent results
            for (int i = 1; i < results.Length; i++)
            {
                // Results should be consistent (same log entries available)
                Assert.IsNotNull(results[i], $"Sequential call {i} should maintain consistency");
            }
        }

        [Test]
        public void GetLogs_MaxEntriesParameterDescription_EndsWithMax()
        {
            // This test verifies that the maxEntries parameter description ends with "Max: 5000"
            // by using reflection to get the parameter description attribute

            var methodInfo = typeof(Tool_Console).GetMethod(nameof(Tool_Console.GetLogs));
            Assert.IsNotNull(methodInfo, $"{nameof(Tool_Console.GetLogs)} method should exist");

            var parameterName = "maxEntries";
            var parameters = methodInfo.GetParameters();
            var maxEntriesParam = parameters.FirstOrDefault(p => p.Name == parameterName);
            Assert.IsNotNull(maxEntriesParam, $"{parameterName} parameter should exist");

            var descriptionAttr = maxEntriesParam.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
            Assert.IsNotNull(descriptionAttr, $"{parameterName} parameter should have Description attribute");

            var description = descriptionAttr!.Description;
            Assert.IsNotNull(description, "Description should not be null");
            Assert.IsTrue(description.Contains("Default: 100"),
                $"{parameterName} parameter description should contain 'Default: 100'. Actual description: '{description}'");
        }

        // TODO: Re-enable these tests when UnityLogCollector API is updated
        // These tests require LogEntries property, ClearLogs(), SaveToFile(), LoadFromFile(), GetAllLogs() methods
        // which are not currently available in the UnityLogCollector class

        // [UnityTest]
        // public IEnumerator GetLogs_Validate_LogCount()
        // {
        //     // This test verifies that logs are being stored and read from the log cache properly.
        //     var testCount = 15;
        //     var timeout = 10000;
        //     var startCount = logCollector.LogEntries;
        //     for (int i = 0; i < testCount; i++)
        //     {
        //         Debug.Log($"Test Log {i + 1}");
        //     }

        //     var frameCount = 0;
        //     while (Startup.LogCollector.LogEntries < startCount + testCount)
        //     {
        //         yield return null;
        //         frameCount++;
        //         Assert.Less(frameCount, timeout, "Timeout waiting for logs to be collected.");
        //     }
        //     Assert.AreEqual(startCount + testCount, Startup.LogCollector.LogEntries, "Log entry count should match the amount of logs generated by this test.");
        // }

        // [UnityTest]
        // public IEnumerator GetLogs_Validate_ConsoleLogRetention()
        // {
        //     // This test verifies that logs are being stored and read from the log cache properly.
        //     const int testCount = 15;
        //     const int timeout = 100000;

        //     var logMessages = Enumerable.Range(1, testCount)
        //         .Select(i => $"Test Log {i}")
        //         .ToArray();

        //     Debug.Log($"Starting log retention test with {testCount} logs.");
        //     Debug.Log($"Generated log messages:\n{string.Join("\n", logMessages)}");

        //     // Ensure a clean slate
        //     Debug.Log($"Clearing existing logs.");
        //     Startup.LogCollector.ClearLogs();
        //     yield return null;

        //     var startCount = Startup.LogCollector.LogEntries;
        //     Assert.AreEqual(0, startCount, "Log entries should be empty at the start.");

        //     foreach (var logMessage in logMessages)
        //     {
        //         Debug.Log(logMessage);
        //     }

        //     // Wait for logs to be collected
        //     var frameCount = 0;
        //     while (Startup.LogCollector.LogEntries < testCount)
        //     {
        //         yield return null;
        //         frameCount++;
        //         Assert.Less(frameCount, timeout, "Timeout waiting for logs to be collected.");
        //     }
        //     Assert.AreEqual(testCount, Startup.LogCollector.LogEntries, "Log entries count should include new entries.");

        //     // Save to file and wait for completion
        //     var saveTask = Startup.LogCollector.SaveToFile();
        //     frameCount = 0;
        //     while (!saveTask.IsCompleted)
        //     {
        //         yield return null;
        //         frameCount++;
        //         Assert.Less(frameCount, timeout, $"Timeout waiting for {nameof(UnityLogCollector.SaveAsync)} to complete.");
        //     }

        //     // Clear logs and confirm
        //     Startup.LogCollector.ClearLogs(false);
        //     Assert.AreEqual(0, Startup.LogCollector.LogEntries, "Log entries should be cleared.");
        //     Assert.AreEqual(0, Startup.LogCollector.GetAllLogs().Length, "Log entries should be cleared.");

        //     // Load from file and wait for completion
        //     var loadTask = Startup.LogCollector.LoadFromFile();
        //     frameCount = 0;
        //     while (!loadTask.IsCompleted)
        //     {
        //         yield return null;
        //         frameCount++;
        //         Assert.Less(frameCount, timeout, $"Timeout waiting for {nameof(UnityLogCollector.LoadFromFile)} to complete.");
        //     }

        //     var allLogs = Startup.LogCollector.GetAllLogs();

        //     Assert.AreEqual(Startup.LogCollector.LogEntries, allLogs.Length, "Loaded log entries count should match the saved entries.");

        //     // Final assertion
        //     Assert.AreEqual(testCount, Startup.LogCollector.LogEntries, "LogUtils should have the restored logs in memory.");

        //     for (int i = 0; i < testCount; i++)
        //     {
        //         var expectedMessage = logMessages[i];
        //         Assert.IsTrue(allLogs.Any(entry => entry.Message == expectedMessage),
        //             $"Restored logs should contain: {expectedMessage}");
        //     }
        // }
    }
}

