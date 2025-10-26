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
using System.Text;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using Extensions.Unity.PlayerPrefsEx;
using NUnit.Framework.Internal;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestResultCollector : ICallbacks
    {
        static int counter = 0;

        readonly object _logsMutex = new();
        readonly List<TestResultData> _results = new();
        readonly TestSummaryData _summary = new();
        readonly List<TestLogEntry> _logs = new();
        readonly TestMode _testMode;

        DateTime startTime;

        public List<TestResultData> GetResults() => _results;
        public TestSummaryData GetSummary() => _summary;
        public List<TestLogEntry> GetLogs()
        {
            lock (_logsMutex)
            {
                return _logs.ToList();
            }
        }
        public TestMode GetTestMode() => _testMode;

        public string TestModeAsString => _testMode switch
        {
            TestMode.EditMode => "EditMode",
            TestMode.PlayMode => "PlayMode",
            _ => "Unknown"
        };

        public static PlayerPrefsString TestCallRequestID = new PlayerPrefsString("Unity_MCP_TestRunner_TestCallRequestID");

        public static PlayerPrefsBool IncludeMessage = new PlayerPrefsBool("Unity_MCP_TestRunner_IncludeMessage", true);
        public static PlayerPrefsBool IncludeMessageStacktrace = new PlayerPrefsBool("Unity_MCP_TestRunner_IncludeStacktrace");

        public static PlayerPrefsBool IncludeLogs = new PlayerPrefsBool("Unity_MCP_TestRunner_IncludeLogs");
        public static PlayerPrefsInt IncludeLogsMinLevel = new PlayerPrefsInt("Unity_MCP_TestRunner_IncludeLogsMinLevel", (int)LogType.Warning);
        public static PlayerPrefsBool IncludeLogsStacktrace = new PlayerPrefsBool("Unity_MCP_TestRunner_IncludeLogsStacktrace");

        public TestResultCollector()
        {
            int newCount = System.Threading.Interlocked.Increment(ref counter);

            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Trace))
                Debug.Log($"[{nameof(TestResultCollector)}] Ctor.");

            if (newCount > 1)
                throw new InvalidOperationException($"Only one instance of {nameof(TestResultCollector)} is allowed. Current count: {newCount}");
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log($"[{nameof(TestResultCollector)}] RunStarted.");

            startTime = DateTime.Now;
            var testCount = CountTests(testsToRun);

            lock (_logsMutex)
            {
                _logs.Clear();
            }
            _results.Clear();
            _summary.Clear();
            _summary.TotalTests = testCount;

            // Subscribe on log messages
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log($"[{nameof(TestResultCollector)}] Run {TestModeAsString} started: {testCount} tests.");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log($"[{nameof(TestResultCollector)}] RunFinished.");

            // Unsubscribe from log messages
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            var duration = DateTime.Now - startTime;
            _summary.Duration = DateTime.Now - startTime;
            _summary.TotalTests = CountTests(result.Test);
            if (_summary.FailedTests > 0)
            {
                _summary.Status = TestRunStatus.Failed;
            }
            else if (_summary.PassedTests > 0)
            {
                _summary.Status = TestRunStatus.Passed;
            }
            else
            {
                _summary.Status = TestRunStatus.Unknown;
            }

            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
            {
                Debug.Log($"[{nameof(TestResultCollector)}] Run {TestModeAsString} finished with {_summary.TotalTests} test results. Result status: {result.TestStatus}");
                Debug.Log($"[{nameof(TestResultCollector)}] Final duration: {duration:mm\\:ss\\.fff}. Completed: {_results.Count}/{_summary.TotalTests}");
            }

            UnityMcpPlugin.BuildAndStart();

            var requestId = TestCallRequestID.Value;
            TestCallRequestID.Value = string.Empty;
            if (string.IsNullOrEmpty(requestId) == false)
            {
                var response = ResponseCallTool
                    .Success(FormatTestResults(
                        includeMessage: IncludeMessage.Value,
                        includeLogs: IncludeLogs.Value,
                        includeMessageStacktrace: IncludeMessageStacktrace.Value,
                        includeLogsStacktrace: IncludeLogsStacktrace.Value))
                    .SetRequestID(requestId);

                _ = UnityMcpPlugin.NotifyToolRequestCompleted(response);
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
            // Test started - could log this if needed
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // Only count actual tests, not test suites
            if (!result.Test.IsSuite)
            {
                var testResult = new TestResultData
                {
                    Name = result.Test.FullName,
                    Status = result.TestStatus.ToString(),
                    Duration = TimeSpan.FromSeconds(result.Duration),
                    Message = result.Message,
                    StackTrace = result.StackTrace
                };

                _results.Add(testResult);

                var statusEmoji = result.TestStatus switch
                {
                    TestStatus.Passed => "<color=green>✅</color>",
                    TestStatus.Failed => "<color=red>❌</color>",
                    TestStatus.Skipped => "<color=yellow>⚠️</color>",
                    _ => string.Empty
                };

                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                    Debug.Log($"[{nameof(TestResultCollector)}] {statusEmoji} Test finished ({_results.Count}/{_summary.TotalTests}): {result.Test.FullName} - {result.TestStatus}");

                // Update summary counts
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        _summary.PassedTests++;
                        break;
                    case TestStatus.Failed:
                        _summary.FailedTests++;
                        break;
                    case TestStatus.Skipped:
                        _summary.SkippedTests++;
                        break;
                }

                // Update duration as tests complete
                _summary.Duration = DateTime.Now - startTime;

                // Check if all tests are complete
                if (_results.Count >= _summary.TotalTests)
                {
                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                        Debug.Log($"[{nameof(TestResultCollector)}] All tests completed via TestFinished. Final duration: {_summary.Duration:mm\\:ss\\.fff}");
                }
            }
        }

        void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var entry = new TestLogEntry(type, condition, stackTrace);
            lock (_logsMutex)
            {
                _logs.Add(entry);
            }
        }

        string FormatTestResults(bool includeMessage, bool includeMessageStacktrace, bool includeLogs, bool includeLogsStacktrace)
        {
            var results = GetResults();
            var summary = GetSummary();
            var logs = GetLogs();

            var output = new StringBuilder();
            output.AppendLine("[Success] Test execution completed.");
            output.AppendLine();

            // Summary
            output.AppendLine("=== TEST SUMMARY ===");
            output.AppendLine($"Status: {summary.Status}");
            output.AppendLine($"Total: {summary.TotalTests}");
            output.AppendLine($"Passed: {summary.PassedTests}");
            output.AppendLine($"Failed: {summary.FailedTests}");
            output.AppendLine($"Skipped: {summary.SkippedTests}");
            output.AppendLine($"Duration: {summary.Duration:hh\\:mm\\:ss\\.fff}");
            output.AppendLine();

            // Individual test results
            if (results.Any())
            {
                output.AppendLine("=== TEST RESULTS ===");
                foreach (var result in results)
                {
                    output.AppendLine($"[{result.Status}] {result.Name}");
                    output.AppendLine($"  Duration: {result.Duration:ss\\.fff}s");

                    if (includeMessage)
                    {
                        if (!string.IsNullOrEmpty(result.Message))
                            output.AppendLine($"  Message: {result.Message}");
                    }

                    if (includeMessageStacktrace)
                    {
                        if (!string.IsNullOrEmpty(result.StackTrace))
                            output.AppendLine($"  Stack Trace: {result.StackTrace}");
                    }

                    output.AppendLine();
                }
            }

            // Console logs
            if (includeLogs && logs.Any())
            {
                var minLogLevel = TestLogEntry.ToLogLevel((LogType)IncludeLogsMinLevel.Value);
                output.AppendLine("=== CONSOLE LOGS ===");
                foreach (var log in logs)
                {
                    if (log.LogLevel < minLogLevel)
                        continue;
                    output.AppendLine(log.ToStringFormat(
                        includeType: true,
                        includeStacktrace: includeLogsStacktrace));
                }
            }

            return output.ToString();
        }

        public static int CountTests(ITestAdaptor test)
        {
            try
            {
                if (test == null)
                    return 0;

                if (test.HasChildren && test.Children != null)
                    return test.Children.Sum(CountTests);

                return test.IsSuite ? 0 : 1;
            }
            catch
            {
                return 0;
            }
        }
    }
}
