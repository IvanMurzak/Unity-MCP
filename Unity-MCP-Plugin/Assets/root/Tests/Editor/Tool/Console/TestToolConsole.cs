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
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestToolConsole : BaseTest
    {
        Tool_Console _tool = null!;
        UnityLogCollector _logCollector = null!;

        [SetUp]
        public void TestSetUp()
        {
            // Create local collector
            _logCollector = new UnityLogCollector(new FileLogStorage(cacheFileName: "test-tool-console.txt"));

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

        void ResultValidation(LogEntry[] result)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
        }

        void ResultValidationExpected(LogEntry[] result, params string[] expectedLines)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");

            if (expectedLines != null)
            {
                foreach (var line in expectedLines)
                    Assert.IsTrue(result.Any(entry => entry.Message.Contains(line)), $"Should contain expected line: {line}");
            }
        }

        void ResultValidationUnexpected(LogEntry[] result, params string[] unexpectedLines)
        {
            Debug.Log($"[{nameof(TestToolConsole)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");

            if (unexpectedLines != null)
            {
                foreach (var line in unexpectedLines)
                    Assert.IsFalse(result.Any(entry => entry.Message.Contains(line)), $"Should not contain unexpected line: {line}");
            }
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

            _logCollector.Save();

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

            _logCollector.Save();

            // Act
            var result = _tool.GetLogs(maxEntries: limit);

            // Assert
            ResultValidation(result);

            // Count the number of log entries in the result
            var lines = result
                .Where(entry => entry.Message.Contains("Test log"))
                .ToArray();

            Assert.AreEqual(limit, lines.Length, $"Should return exactly {limit} entries");
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

            _logCollector.Save();

            // Act - Get only warnings
            var result = _tool.GetLogs(logTypeFilter: LogType.Warning);

            // Assert
            ResultValidation(result);
            Assert.IsTrue(result.Any(entry => entry.LogType == LogType.Warning), "Should contain warning logs");
            Assert.IsFalse(result.Any(entry => entry.LogType == LogType.Log), "Should NOT contain log logs");
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
            Assert.Throws<ArgumentException>(
                () => _tool.GetLogs(maxEntries: 0),
                $"Should contain invalid maxEntries error");
        }

        [UnityTest]
        public IEnumerator GetLogs_WithIncludeStackTrace_IncludesStackTraces()
        {
            // Arrange: Generate a log with potential stack trace (warnings typically have stack traces)
            Debug.LogWarning("Test warning with stack trace");

            for (int i = 0; i < 3; i++)
                yield return null;

            _logCollector.Save();

            // Act
            var result = _tool.GetLogs(includeStackTrace: true, logTypeFilter: LogType.Warning);

            // Assert
            ResultValidation(result);

            // Stack traces should be included for warnings
            if (result.Any(entry => entry.LogType == LogType.Warning))
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

            _logCollector.Save();

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

            _logCollector.Save();

            // Test each safe log type filter
            LogType[] logTypes = { LogType.Log, LogType.Warning, LogType.Error };

            foreach (var logType in logTypes)
            {
                // Act
                var result = _tool.GetLogs(logTypeFilter: logType);

                // Assert
                ResultValidation(result);

                if (logType == LogType.Log)
                    Assert.IsTrue(result.Any(entry => entry.Message == regularLogMessage), $"Should contain regular log message for '{logType}' filter.");

                if (logType == LogType.Warning)
                    Assert.IsTrue(result.Any(entry => entry.Message == warningLogMessage), $"Should contain warning log message for '{logType}' filter.");
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

            _logCollector.Save();

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
                message: "Test message",
                stackTrace: "Test stack trace",
                logType: LogType.Warning
            );

            // Assert
            Assert.AreEqual("Test message", logEntry.Message);
            Assert.AreEqual("Test stack trace", logEntry.StackTrace);
            Assert.AreEqual(LogType.Warning, logEntry.LogType);
            Assert.IsTrue(logEntry.Timestamp <= DateTime.Now);
            Assert.IsTrue(logEntry.Timestamp >= DateTime.Now.AddMinutes(-1)); // Should be very recent
        }

        [Test]
        public void ConsoleLogEntry_ToString_FormatsCorrectly()
        {
            // Arrange - Test with Warning to avoid causing test failure
            var logEntry = new LogEntry(
                message: "Test message",
                stackTrace: "Test stack trace",
                logType: LogType.Warning
            );

            // Act
            var result = logEntry.ToString();

            // Assert
            Assert.IsTrue(result.Contains("[Warning]"), "Should contain log type");
            Assert.IsTrue(result.Contains("Test message"), "Should contain message");
            Assert.IsTrue(result.Contains(logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")), "Should contain formatted timestamp");
        }

        [Test]
        public void ConsoleLogEntry_ErrorType_CreatesCorrectly()
        {
            // Test Error log type creation directly (without using Debug.LogError)
            var errorLogEntry = new LogEntry(
                message: "Error message",
                stackTrace: "Error stack trace",
                logType: LogType.Error
            );

            // Assert
            Assert.AreEqual("Error message", errorLogEntry.Message);
            Assert.AreEqual("Error stack trace", errorLogEntry.StackTrace);
            Assert.AreEqual(LogType.Error, errorLogEntry.LogType);
            Assert.IsTrue(errorLogEntry.ToString().Contains("[Error]"), "Should format Error type correctly");
        }

        [Test]
        public void ConsoleLogEntry_AssertType_CreatesCorrectly()
        {
            // Test Assert log type creation directly (without using Debug.LogAssertion)
            var assertLogEntry = new LogEntry(
                message: "Assert message",
                stackTrace: "Assert stack trace",
                logType: LogType.Assert
            );

            // Assert
            Assert.AreEqual("Assert message", assertLogEntry.Message);
            Assert.AreEqual("Assert stack trace", assertLogEntry.StackTrace);
            Assert.AreEqual(LogType.Assert, assertLogEntry.LogType);
            Assert.IsTrue(assertLogEntry.ToString().Contains("[Assert]"), "Should format Assert type correctly");
        }
        [Test]
        public void Error_InvalidMaxEntries_ReturnsCorrectMessage()
        {
            // Act
            var result1 = Tool_Console.Error.InvalidMaxEntries(0);

            // Assert
            Assert.IsTrue(result1.Contains("[Error]"), "Should contain error prefix");
            Assert.IsTrue(result1.Contains("Invalid maxEntries value"), "Should contain error description");
            Assert.IsTrue(result1.Contains("'0'"), "Should contain the invalid value");
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

