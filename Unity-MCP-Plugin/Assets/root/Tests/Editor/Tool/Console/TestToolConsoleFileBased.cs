/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Collections;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolConsoleFileBased : BaseTest
    {
        private Tool_Console _tool;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Console();
        }

        [UnityTest]
        public IEnumerator GetLogs_WithFileBased_MaintainsExistingFunctionality()
        {
            // Arrange - Clear existing logs first
            LogUtils.ClearLogs();
            yield return null;

            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var testLogMessage = $"File-based test log {uniqueId}";
            var testWarningMessage = $"File-based test warning {uniqueId}";

            // Act - Generate logs
            Debug.Log(testLogMessage);
            yield return null;
            Debug.LogWarning(testWarningMessage);
            yield return new WaitForSeconds(0.1f); // Allow file operations

            // Test basic functionality
            var allLogsResult = _tool.GetLogs(maxEntries: 100);
            Assert.IsTrue(allLogsResult.Contains("[Success]"), "Should return success message");
            Assert.IsTrue(allLogsResult.Contains(testLogMessage), "Should contain test log message");
            Assert.IsTrue(allLogsResult.Contains(testWarningMessage), "Should contain test warning message");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLogs_LogTypeFiltering_WorksWithFileBased()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;

            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var logMessage = $"FilterTest Log {uniqueId}";
            var warningMessage = $"FilterTest Warning {uniqueId}";
            var errorMessage = $"FilterTest Error {uniqueId}";

            // Act - Generate different log types
            Debug.Log(logMessage);
            yield return new WaitForFixedUpdate();
            Debug.LogWarning(warningMessage);
            yield return new WaitForFixedUpdate();
            Debug.LogError(errorMessage);
            yield return new WaitForSeconds(0.2f); // Allow file operations

            // Test filtering by Warning
            var warningResult = _tool.GetLogs(maxEntries: 100, logTypeFilter: "Warning");
            Assert.IsTrue(warningResult.Contains(warningMessage), "Should contain warning message when filtered");
            Assert.IsFalse(warningResult.Contains(logMessage), "Should not contain log message when filtered by Warning");

            // Test filtering by Error
            var errorResult = _tool.GetLogs(maxEntries: 100, logTypeFilter: "Error");
            Assert.IsTrue(errorResult.Contains(errorMessage), "Should contain error message when filtered");
            Assert.IsFalse(errorResult.Contains(warningMessage), "Should not contain warning message when filtered by Error");

            // Test All filter
            var allResult = _tool.GetLogs(maxEntries: 100, logTypeFilter: "All");
            Assert.IsTrue(allResult.Contains(logMessage), "All filter should contain log message");
            Assert.IsTrue(allResult.Contains(warningMessage), "All filter should contain warning message");
            Assert.IsTrue(allResult.Contains(errorMessage), "All filter should contain error message");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLogs_MaxEntriesLimit_WorksWithFileBased()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;

            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];

            // Generate more logs than we'll request
            for (int i = 0; i < 15; i++)
            {
                Debug.Log($"MaxEntries test {i} {uniqueId}");
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(0.2f); // Allow file operations

            // Act - Request limited number of logs
            var limitedResult = _tool.GetLogs(maxEntries: 5);

            // Assert
            Assert.IsTrue(limitedResult.Contains("[Success]"), "Should return success");
            
            // Count the actual log entries in the result (excluding the summary line)
            var lines = limitedResult.Split('\n');
            var logLines = lines.Where(line => line.Contains($"{uniqueId}")).ToArray();
            
            Assert.IsTrue(logLines.Length <= 5, $"Should have at most 5 log entries, but got {logLines.Length}");
            
            // The returned logs should be the most recent ones
            if (logLines.Length > 0)
            {
                var lastLogLine = logLines[logLines.Length - 1];
                Assert.IsTrue(lastLogLine.Contains("14"), "Should contain the most recent log (index 14)");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLogs_StackTraceInclusion_WorksWithFileBased()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;

            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var testMessage = $"StackTrace test {uniqueId}";

            // Act - Generate a log (this will have a stack trace)
            Debug.Log(testMessage);
            yield return new WaitForSeconds(0.1f); // Allow file operations

            // Test without stack trace
            var withoutStackTrace = _tool.GetLogs(maxEntries: 10, includeStackTrace: false);
            Assert.IsTrue(withoutStackTrace.Contains(testMessage), "Should contain the log message");
            Assert.IsFalse(withoutStackTrace.Contains("Stack Trace:"), "Should not contain stack trace header when disabled");

            // Test with stack trace
            var withStackTrace = _tool.GetLogs(maxEntries: 10, includeStackTrace: true);
            Assert.IsTrue(withStackTrace.Contains(testMessage), "Should contain the log message with stack trace enabled");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLogs_TimeFiltering_WorksWithFileBased()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;

            var uniqueId = System.Guid.NewGuid().ToString("N")[..8];
            var oldMessage = $"Old log {uniqueId}";
            var recentMessage = $"Recent log {uniqueId}";

            // Generate an "old" log
            Debug.Log(oldMessage);
            yield return new WaitForSeconds(0.1f);

            // Wait to create time separation
            yield return new WaitForSeconds(1.0f);

            // Generate a recent log
            Debug.Log(recentMessage);
            yield return new WaitForSeconds(0.1f);

            // Act - Get logs from last minute (should include both)
            var lastMinuteResult = _tool.GetLogs(maxEntries: 100, lastMinutes: 1);
            Assert.IsTrue(lastMinuteResult.Contains(oldMessage), "Should contain old message within 1 minute");
            Assert.IsTrue(lastMinuteResult.Contains(recentMessage), "Should contain recent message within 1 minute");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLogs_EmptyResult_HandledCorrectly()
        {
            // Arrange - Clear all logs
            LogUtils.ClearLogs();
            yield return new WaitForSeconds(0.1f); // Allow file operations

            // Act - Request logs when none exist
            var result = _tool.GetLogs(maxEntries: 100);

            // Assert
            Assert.IsTrue(result.Contains("[Success]") || result.Contains("No log entries"), 
                "Should handle empty result gracefully");

            yield return null;
        }

        [Test]
        public void GetLogs_InvalidParameters_HandledCorrectly()
        {
            // Test invalid maxEntries
            var invalidMaxResult = _tool.GetLogs(maxEntries: -1);
            Assert.IsTrue(invalidMaxResult.Contains("[Error]") || invalidMaxResult.Contains("Invalid"), 
                "Should handle invalid maxEntries parameter");

            var tooLargeMaxResult = _tool.GetLogs(maxEntries: 10000);
            Assert.IsTrue(tooLargeMaxResult.Contains("[Error]") || tooLargeMaxResult.Contains("Invalid"), 
                "Should handle too large maxEntries parameter");

            // Test invalid log type filter
            var invalidTypeResult = _tool.GetLogs(maxEntries: 100, logTypeFilter: "InvalidType");
            Assert.IsTrue(invalidTypeResult.Contains("[Error]") || invalidTypeResult.Contains("Invalid"), 
                "Should handle invalid logTypeFilter parameter");
        }

        int CountLogEntries(string logsResult)
        {
            if (string.IsNullOrEmpty(logsResult)) return 0;
            
            var lines = logsResult.Split('\n');
            // Count lines that look like log entries (contain timestamp pattern)
            return lines.Count(line => line.Contains("] [") && (line.Contains("] Log") || line.Contains("] Warning") || line.Contains("] Error")));
        }
    }
}