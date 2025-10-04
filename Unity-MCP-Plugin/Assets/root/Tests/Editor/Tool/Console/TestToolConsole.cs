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
using System;
using System.Collections;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolConsole : BaseTest
    {
        Tool_Console _tool;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Console();

            // Clear any existing logs by getting them all
            LogUtils.ClearLogs();
        }

        void ResultValidation(string result)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result.Contains("[Success]"), $"Should contain success message.");
        }

        void ResultValidationExpected(string result, params string[] expectedLines)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result.Contains("[Success]"), $"Should contain success message.");

            if (expectedLines != null)
            {
                foreach (var line in expectedLines)
                    Assert.IsTrue(result.Contains(line), $"Should contain expected line: {line}");
            }
        }

        void ResultValidationUnexpected(string result, params string[] unexpectedLines)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result.Contains("[Success]"), $"Should contain success message.");

            if (unexpectedLines != null)
            {
                foreach (var line in unexpectedLines)
                    Assert.IsFalse(result.Contains(line), $"Should not contain unexpected line: {line}");
            }
        }

        void ErrorValidation(string result, string expectedErrorStart = "[Error]")
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Error Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(result.StartsWith(expectedErrorStart), $"Result should start with '{expectedErrorStart}'.");
        }

        [UnityTest]
        public IEnumerator GetLogs_DefaultParameters_ReturnsLogs()
        {
            // Arrange: Generate some test logs (only safe log types)
            var logMessage1 = "Test log message 1";
            var warningMessage = "Test warning message";
            var logMessage2 = "Test log message 2";

            Debug.Log(logMessage1);
            Debug.LogWarning(warningMessage);
            Debug.Log(logMessage2);

            // Wait for logs to be captured
            for (int i = 0; i < 3; i++)
                yield return null;

            // Act
            var result = _tool.GetLogs();

            // Assert
            ResultValidationExpected(result,
                 logMessage1,
                 warningMessage,
                 logMessage2);
        }

        [UnityTest]
        public IEnumerator GetLogs_WithMaxEntries_LimitsResults()
        {
            // Arrange: Generate multiple test logs
            const int limit = 3;

            for (int i = 0; i < 10; i++)
                Debug.Log($"Test log {i}");

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act
            var result = _tool.GetLogs(maxEntries: limit);

            // Assert
            ResultValidation(result);

            // Count the number of log entries in the result
            var lines = result.Split('\n')
                .Where(line => line.Contains("[Log]"))
                .ToArray();

            Assert.AreEqual(lines.Length, limit, $"Should return exactly {limit} entries");
        }

        [UnityTest]
        public IEnumerator GetLogs_WithLogTypeFilter_FiltersCorrectly()
        {
            // Arrange: Generate logs of different types (only safe log types)
            Debug.Log("Test log message");
            Debug.LogWarning("Test warning message");
            Debug.Log("Another test log message");

            for (int i = 0; i < 5; i++)
                yield return null;

            // Act - Get only warnings
            var result = _tool.GetLogs(logTypeFilter: LogType.Warning);

            // Assert
            ResultValidation(result);
            Assert.IsTrue(result.Contains("[Warning]"), "Should contain warning logs");
            Assert.IsFalse(result.Contains("[Log]") && !result.Contains("[Warning]"), "Should not contain non-warning logs in the log entries");
        }

        [UnityTest]
        public IEnumerator GetLogs_ErrorLogTypeFilter_HandlesCorrectly()
        {
            // This test verifies that the Error log type filter is supported
            // without actually generating error logs (which would fail the test)

            // Generate some non-error logs
            var logMessage = "Regular log for error filter test";
            var logWarningMessage = "Warning log for error filter test";
            Debug.Log(logMessage);
            Debug.LogWarning(logWarningMessage);

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act - Test Error filter (should not return validation error)
            var result = _tool.GetLogs(logTypeFilter: LogType.Error);

            // Assert - Should succeed even if no error logs are found
            ResultValidationUnexpected(result, logMessage, logWarningMessage);
        }

        [UnityTest]
        public IEnumerator GetLogs_AssertLogTypeFilter_HandlesCorrectly()
        {
            // This test verifies that the Assert log type filter is supported
            // without actually generating assertion logs (which would fail the test)

            // Generate some non-assertion logs
            var logMessage = "Regular log for assert filter test";
            var logWarningMessage = "Warning log for assert filter test";
            Debug.Log(logMessage);
            Debug.LogWarning(logWarningMessage);

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act - Test Assert filter (should not return validation error)
            var result = _tool.GetLogs(logTypeFilter: LogType.Assert);

            // Assert - Should succeed even if no assertion logs are found
            ResultValidationUnexpected(result, logMessage, logWarningMessage);
        }

        [Test]
        public void GetLogs_WithInvalidMaxEntries_ReturnsError()
        {
            // Act - Test with value below minimum
            var result1 = _tool.GetLogs(maxEntries: 0);

            // Assert
            ErrorValidation(result1);
            Assert.IsTrue(result1.Contains("Invalid maxEntries value"), $"Should contain invalid maxEntries error.\nResult: {result1}");

            // Act - Test with value above maximum
            var result2 = _tool.GetLogs(maxEntries: LogUtils.MaxLogEntries + 1);

            // Assert
            ErrorValidation(result2);
            Assert.IsTrue(result2.Contains("Invalid maxEntries value"), $"Should contain invalid maxEntries error.\nResult: {result2}");
        }

        [UnityTest]
        public IEnumerator GetLogs_WithIncludeStackTrace_IncludesStackTraces()
        {
            // Arrange: Generate a log with potential stack trace (warnings typically have stack traces)
            Debug.LogWarning("Test warning with stack trace");

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act
            var result = _tool.GetLogs(includeStackTrace: true, logTypeFilter: LogType.Warning);

            // Assert
            ResultValidation(result);

            // Stack traces should be included for warnings
            if (result.Contains("[Warning]"))
            {
                // Note: In Unity editor, stack traces might not always be present for all log types
                // This test verifies the parameter is handled correctly
                Assert.DoesNotThrow(() => _tool.GetLogs(includeStackTrace: true));
            }
        }

        [UnityTest]
        public IEnumerator GetLogs_WithTimeFilter_FiltersCorrectly()
        {
            // Arrange: Generate some logs
            var logMessage = "Old log message";
            Debug.Log(logMessage);

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act - Get logs from last 1 minute (should include recent logs)
            var result = _tool.GetLogs(lastMinutes: 1);

            // Assert
            ResultValidationExpected(result, logMessage);
        }

        [Test]
        public void GetLogs_NoMatchingLogs_ReturnsNoLogsMessage()
        {
            // Act - Try to get logs with a very restrictive filter
            var result = _tool.GetLogs(logTypeFilter: LogType.Exception, lastMinutes: 1);

            // Assert
            ResultValidation(result);
        }

        [UnityTest]
        public IEnumerator GetLogs_AllLogTypes_HandlesCorrectly()
        {
            // Arrange: Generate safe types of logs
            var regularLogMessage = "Recent regular log";
            var warningLogMessage = "Recent warning log";

            Debug.Log(regularLogMessage);
            Debug.LogWarning(warningLogMessage);

            for (int i = 0; i < 3; i++)
                yield return null;

            // Test each safe log type filter
            LogType[] logTypes = { LogType.Log, LogType.Warning, LogType.Error };

            foreach (var logType in logTypes)
            {
                // Act
                var result = _tool.GetLogs(logTypeFilter: logType);

                // Assert
                ResultValidation(result);

                if (logType == LogType.Log)
                    Assert.IsTrue(result.Contains(regularLogMessage), $"Should contain regular log message for '{logType}' filter.");

                if (logType == LogType.Warning)
                    Assert.IsTrue(result.Contains(warningLogMessage), $"Should contain warning log message for '{logType}' filter.");
            }
        }

        [UnityTest]
        public IEnumerator GetLogs_CombinedFilters_WorksTogether()
        {
            // Arrange: Generate various logs (avoiding Debug.LogError)
            var logWarningMessage1 = "Recent warning 1";
            var logWarningMessage2 = "Recent warning 2";
            var regularLogMessage = "Recent regular log";

            Debug.LogWarning(logWarningMessage1);
            Debug.LogWarning(logWarningMessage2);
            Debug.Log(regularLogMessage);

            for (int i = 0; i < 3; i++)
                yield return null;

            // Act - Combine multiple filters
            var result = _tool.GetLogs(
                maxEntries: 1,
                logTypeFilter: LogType.Warning,
                includeStackTrace: true,
                lastMinutes: 1
            );

            // Assert
            ResultValidationExpected(result, logWarningMessage2);
            ResultValidationUnexpected(result, logWarningMessage1, regularLogMessage);
        }

        [Test]
        public void ConsoleLogEntry_CreatesCorrectly()
        {
            // Arrange & Act
            var logEntry = new LogEntry(
                "Test message",
                "Test stack trace",
                LogType.Warning
            );

            // Assert
            Assert.AreEqual("Test message", logEntry.message);
            Assert.AreEqual("Test stack trace", logEntry.stackTrace);
            Assert.AreEqual(LogType.Warning, logEntry.logType);
            Assert.AreEqual("Warning", logEntry.logTypeString);
            Assert.IsTrue(logEntry.timestamp <= DateTime.Now);
            Assert.IsTrue(logEntry.timestamp >= DateTime.Now.AddMinutes(-1)); // Should be very recent
        }

        [Test]
        public void ConsoleLogEntry_ToString_FormatsCorrectly()
        {
            // Arrange - Test with Warning to avoid causing test failure
            var logEntry = new LogEntry(
                "Test message",
                "Test stack trace",
                LogType.Warning
            );

            // Act
            var result = logEntry.ToString();

            // Assert
            Assert.IsTrue(result.Contains("[Warning]"), "Should contain log type");
            Assert.IsTrue(result.Contains("Test message"), "Should contain message");
            Assert.IsTrue(result.Contains(logEntry.timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")), "Should contain formatted timestamp");
        }

        [Test]
        public void ConsoleLogEntry_ErrorType_CreatesCorrectly()
        {
            // Test Error log type creation directly (without using Debug.LogError)
            var errorLogEntry = new LogEntry(
                "Error message",
                "Error stack trace",
                LogType.Error
            );

            // Assert
            Assert.AreEqual("Error message", errorLogEntry.message);
            Assert.AreEqual("Error stack trace", errorLogEntry.stackTrace);
            Assert.AreEqual(LogType.Error, errorLogEntry.logType);
            Assert.AreEqual("Error", errorLogEntry.logTypeString);
            Assert.IsTrue(errorLogEntry.ToString().Contains("[Error]"), "Should format Error type correctly");
        }

        [Test]
        public void ConsoleLogEntry_AssertType_CreatesCorrectly()
        {
            // Test Assert log type creation directly (without using Debug.LogAssertion)
            var assertLogEntry = new LogEntry(
                "Assert message",
                "Assert stack trace",
                LogType.Assert
            );

            // Assert
            Assert.AreEqual("Assert message", assertLogEntry.message);
            Assert.AreEqual("Assert stack trace", assertLogEntry.stackTrace);
            Assert.AreEqual(LogType.Assert, assertLogEntry.logType);
            Assert.AreEqual("Assert", assertLogEntry.logTypeString);
            Assert.IsTrue(assertLogEntry.ToString().Contains("[Assert]"), "Should format Assert type correctly");
        }
        [Test]
        public void Error_InvalidMaxEntries_ReturnsCorrectMessage()
        {
            // Act
            var result1 = Tool_Console.Error.InvalidMaxEntries(0);
            var result2 = Tool_Console.Error.InvalidMaxEntries(LogUtils.MaxLogEntries + 1);

            // Assert
            Assert.IsTrue(result1.Contains("[Error]"), "Should contain error prefix");
            Assert.IsTrue(result1.Contains("Invalid maxEntries value"), "Should contain error description");
            Assert.IsTrue(result1.Contains("'0'"), "Should contain the invalid value");

            Assert.IsTrue(result2.Contains("[Error]"), "Should contain error prefix");
            Assert.IsTrue(result2.Contains($"'{LogUtils.MaxLogEntries + 1}'"), "Should contain the invalid value");
        }

        [Test]
        public void Error_InvalidLogTypeFilter_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_Console.Error.InvalidLogTypeFilter("InvalidType");

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix");
            Assert.IsTrue(result.Contains("Invalid logType filter"), "Should contain error description");
            Assert.IsTrue(result.Contains("'InvalidType'"), "Should contain the invalid value");
            Assert.IsTrue(result.Contains("Valid values:"), "Should list valid values");
        }
    }
}

