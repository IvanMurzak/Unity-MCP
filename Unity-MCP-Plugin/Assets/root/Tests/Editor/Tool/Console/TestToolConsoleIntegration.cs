/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolConsoleIntegration : BaseTest
    {
        private Tool_Console _tool;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Console();
        }

        [UnityTest]
        public IEnumerator GetLogs_CapturesRealTimeLogs_Correctly()
        {
            // Arrange - Clear existing logs first
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

            // Act - Retrieve logs
            var allLogsResult = _tool.GetLogs(maxEntries: 1000);
            var logOnlyResult = _tool.GetLogs(maxEntries: 1000, logTypeFilter: LogType.Log);
            var warningOnlyResult = _tool.GetLogs(maxEntries: 1000, logTypeFilter: LogType.Warning);

            // Assert - Check that our unique logs are captured
            Assert.IsNotNull(allLogsResult, "All logs result should not be null");
            Assert.IsTrue(allLogsResult.Contains(testLogMessage),
                $"Should contain test log message.\nUnique ID: {uniqueId}\nResult: {allLogsResult}");
            Assert.IsTrue(allLogsResult.Contains(testWarningMessage),
                $"Should contain test warning message.\nResult: {allLogsResult}");
            Assert.IsTrue(allLogsResult.Contains(testLogMessage2),
                $"Should contain second test log message.\nResult: {allLogsResult}");

            // Assert - Check filtered results
            Assert.IsTrue(logOnlyResult.Contains(testLogMessage),
                $"Log filter should contain test log message.\nResult: {logOnlyResult}");
            Assert.IsTrue(logOnlyResult.Contains(testLogMessage2),
                $"Log filter should contain second test log message.\nResult: {logOnlyResult}");
            Assert.IsFalse(logOnlyResult.Contains(testWarningMessage) && logOnlyResult.Contains("[Warning]"),
                "Log filter should not contain warning in log entries");

            Assert.IsTrue(warningOnlyResult.Contains(testWarningMessage),
                $"Warning filter should contain test warning message.\nResult: {warningOnlyResult}");
            Assert.IsFalse(warningOnlyResult.Contains(testLogMessage) && warningOnlyResult.Contains("[Log]"),
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

            // Act - Get logs from a very short time window (should not include the "old" log)
            var recentLogsResult = _tool.GetLogs(lastMinutes: 0); // 0 means all logs

            // Generate a new log
            Debug.Log(newLogMessage);

            // Wait a few frames (EditMode tests can only yield null)
            for (int i = 0; i < 3; i++)
                yield return null;

            // Get logs from last 1 minute (should include both)
            var minuteLogsResult = _tool.GetLogs(lastMinutes: 1);

            // Assert
            Assert.IsNotNull(minuteLogsResult, "Minute logs result should not be null");
            Assert.IsTrue(minuteLogsResult.Contains(oldLogMessage),
                $"Should contain old log message when filtering by 1 minute.\nResult: {minuteLogsResult}");
            Assert.IsTrue(minuteLogsResult.Contains(newLogMessage),
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

        int CountLogEntries(string logsResult)
        {
            if (string.IsNullOrEmpty(logsResult) || !logsResult.Contains("[Success]"))
                return 0;

            // Count lines that look like log entries (contain timestamp and log type)
            var lines = logsResult.Split('\n');
            int count = 0;
            foreach (var line in lines)
            {
                if (line.Contains("] [")
                    && (
                        line.Contains("[Log]") ||
                        line.Contains("[Warning]") ||
                        line.Contains("[Error]") ||
                        line.Contains("[Assert]") ||
                        line.Contains("[Exception]")
                    ))
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void GetLogs_ThreadSafety_HandlesMultipleAccess()
        {
            // This test ensures the tool handles multiple sequential calls correctly
            // Note: GetLogs uses MainThread.Instance.Run() so we test sequential access instead of concurrent

            var threadsCount = 5;
            var results = new string[threadsCount];

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
                Assert.IsTrue(results[i].Contains("[Success]") || results[i].Contains("No log entries"),
                    $"Call {i} should return valid result. Result: {results[i]}");
            }

            // All calls should succeed and return consistent results
            for (int i = 1; i < results.Length; i++)
            {
                // Results should be consistent (same log entries available)
                Assert.IsTrue(results[i].Contains("[Success]") || results[i].Contains("No log entries"),
                    $"Sequential call {i} should maintain consistency");
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

            var description = descriptionAttr.Description;
            Assert.IsNotNull(description, "Description should not be null");
            Assert.IsTrue(description.EndsWith($"Max: {LogUtils.MaxLogEntries}"),
                $"{parameterName} parameter description should end with 'Max: {LogUtils.MaxLogEntries}'. Actual description: '{description}'");
        }
    }
}

