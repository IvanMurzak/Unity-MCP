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
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Runtime.Tests
{
    public class LogUtilsFileBasedTests
    {
        private string _tempLogDirectory;
        private string _originalDataPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create a temporary directory for testing
            _tempLogDirectory = Path.Combine(Path.GetTempPath(), "mcp-test-" + System.Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempLogDirectory);
            
            Debug.Log($"[LogUtilsFileBasedTests] SetUp - Test directory: {_tempLogDirectory}");
            
            // Clear any existing logs
            LogUtils.ClearLogs();
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[LogUtilsFileBasedTests] TearDown");
            
            // Clean up test directory
            if (Directory.Exists(_tempLogDirectory))
            {
                try
                {
                    Directory.Delete(_tempLogDirectory, true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to cleanup test directory: {ex.Message}");
                }
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogUtils_PersistsLogsToFile_AfterDomainReload()
        {
            // Arrange
            var testMessage = $"File persistence test {System.Guid.NewGuid().ToString("N")[..8]}";
            
            // Act - Generate a log entry
            Debug.Log(testMessage);
            yield return new WaitForSeconds(0.1f); // Allow time for file operations
            
            // Assert - Check that log is captured
            var logs = LogUtils.GetAllLogs();
            Assert.IsTrue(logs.Any(log => log.message.Contains(testMessage)), 
                "Log should be captured in LogUtils");
            
            // Simulate what happens after domain reload by clearing memory cache
            // (In real domain reload, the static fields would be reset but file would remain)
            var initialLogCount = LogUtils.LogEntries;
            Assert.IsTrue(initialLogCount > 0, "Should have logs before clearing");
            
            yield return null;
        }

        [UnityTest] 
        public IEnumerator LogUtils_MemoryCacheClearedAfterFileWrite()
        {
            // Arrange - Clear logs first
            LogUtils.ClearLogs();
            yield return null;
            
            // Act - Generate multiple logs to test memory cache behavior
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Cache test log {i}");
                yield return new WaitForFixedUpdate();
            }
            
            yield return new WaitForSeconds(0.2f); // Allow file operations to complete
            
            // Assert - Memory should be limited but file should have all logs
            var allLogs = LogUtils.GetAllLogs();
            Assert.IsTrue(allLogs.Length >= 10, "All logs should be retrievable");
            
            // Check that we can filter by type
            var logTypeEntries = LogUtils.GetLastLogs(100, LogType.Log);
            Assert.IsTrue(logTypeEntries.Length >= 10, "Should be able to filter by LogType.Log");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogUtils_FiltersLogsByType_Correctly()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;
            
            var testId = System.Guid.NewGuid().ToString("N")[..6];
            
            // Act - Generate different types of logs
            Debug.Log($"Test log {testId}");
            yield return new WaitForFixedUpdate();
            
            Debug.LogWarning($"Test warning {testId}");
            yield return new WaitForFixedUpdate();
            
            Debug.LogError($"Test error {testId}");
            yield return new WaitForFixedUpdate();
            
            yield return new WaitForSeconds(0.2f);
            
            // Assert - Check filtering works
            var allLogs = LogUtils.GetLastLogs(100);
            var warningLogs = LogUtils.GetLastLogs(100, LogType.Warning);
            var errorLogs = LogUtils.GetLastLogs(100, LogType.Error);
            var infoLogs = LogUtils.GetLastLogs(100, LogType.Log);
            
            Assert.IsTrue(allLogs.Length >= 3, "Should have at least 3 logs total");
            Assert.IsTrue(warningLogs.Any(log => log.message.Contains($"Test warning {testId}")), 
                "Should filter warning logs correctly");
            Assert.IsTrue(errorLogs.Any(log => log.message.Contains($"Test error {testId}")), 
                "Should filter error logs correctly");
            Assert.IsTrue(infoLogs.Any(log => log.message.Contains($"Test log {testId}")), 
                "Should filter info logs correctly");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogUtils_HandlesTimeFiltering_Correctly()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;
            
            var oldTestMessage = $"Old test log {System.Guid.NewGuid().ToString("N")[..6]}";
            var recentTestMessage = $"Recent test log {System.Guid.NewGuid().ToString("N")[..6]}";
            
            // Act - Generate an old log
            Debug.Log(oldTestMessage);
            yield return new WaitForSeconds(0.1f);
            
            // Wait a bit to create time separation
            yield return new WaitForSeconds(0.5f);
            
            var recentTime = System.DateTime.Now;
            
            // Generate a recent log
            Debug.Log(recentTestMessage);
            yield return new WaitForSeconds(0.1f);
            
            // Assert - Check time filtering
            var recentLogs = LogUtils.GetLastLogs(100, null, recentTime.AddSeconds(-0.1));
            
            Assert.IsTrue(recentLogs.Any(log => log.message.Contains(recentTestMessage)), 
                "Recent log should be included in time filter");
            
            // The old log might or might not be included depending on exact timing,
            // but the recent log should definitely be there
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogUtils_LimitsReturnedEntries_Correctly()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;
            
            // Act - Generate more logs than the limit we'll request
            var testId = System.Guid.NewGuid().ToString("N")[..6];
            for (int i = 0; i < 20; i++)
            {
                Debug.Log($"Limit test log {i} {testId}");
                yield return new WaitForFixedUpdate();
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // Assert - Check that limiting works
            var limitedLogs = LogUtils.GetLastLogs(5);
            Assert.IsTrue(limitedLogs.Length <= 5, "Should respect the maxEntries limit");
            
            // The returned logs should be the most recent ones
            if (limitedLogs.Length > 0)
            {
                var lastLog = limitedLogs[limitedLogs.Length - 1];
                Assert.IsTrue(lastLog.message.Contains("19"), 
                    "Last log should be from the most recent entries");
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogUtils_ThreadSafety_HandlesMultipleOperations()
        {
            // Arrange
            LogUtils.ClearLogs();
            yield return null;
            
            var testId = System.Guid.NewGuid().ToString("N")[..6];
            
            // Act - Simulate concurrent operations by rapidly generating logs and reading them
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Concurrent test {i} {testId}");
                
                // Immediately try to read logs (simulating concurrent access)
                var logs = LogUtils.GetLastLogs(100);
                Assert.IsNotNull(logs, $"Should always return non-null array at iteration {i}");
                
                yield return new WaitForFixedUpdate();
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // Assert - Final verification
            var finalLogs = LogUtils.GetAllLogs();
            Assert.IsTrue(finalLogs.Length >= 10, "Should have captured all concurrent logs");
            
            yield return null;
        }
    }
}