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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestResultCollector : ICallbacks, IErrorCallbacks
    {
        static volatile int counter = 0;

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

        public static SessionStateString TestCallRequestID = new("Unity_MCP_TestRunner_TestCallRequestID");

        public static SessionStateBool IncludePassingTests = new("Unity_MCP_TestRunner_IncludePassingTests");
        public static SessionStateBool IncludeMessage = new("Unity_MCP_TestRunner_IncludeMessage", true);
        public static SessionStateBool IncludeMessageStacktrace = new("Unity_MCP_TestRunner_IncludeStacktrace");

        public static SessionStateBool IncludeLogs = new("Unity_MCP_TestRunner_IncludeLogs");
        public static SessionStateInt IncludeLogsMinLevel = new("Unity_MCP_TestRunner_IncludeLogsMinLevel", (int)LogType.Warning);
        public static SessionStateBool IncludeLogsStacktrace = new("Unity_MCP_TestRunner_IncludeLogsStacktrace");

        /// <summary>RQL stages applied to the results (tests-run 'result'); empty = the structured response.</summary>
        public static SessionStateString ResultQuery = new("Unity_MCP_TestRunner_ResultQuery");

        public TestResultCollector()
        {
            int newCount = System.Threading.Interlocked.Increment(ref counter);

            UnityMcpPluginEditor.Instance.LogTrace("Ctor", typeof(TestResultCollector));

            if (newCount > 1)
                throw new InvalidOperationException($"Only one instance of {nameof(TestResultCollector)} is allowed. Current count: {newCount}");
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            UnityMcpPluginEditor.Instance.LogInfo("RunStarted", typeof(TestResultCollector));

            startTime = DateTime.Now;
            var testCount = CountTests(testsToRun);

            // tests-run captures from before Unity's run (Tool_Tests.ExecuteTestRun): keep the prebuild setup's logs. Subscribe
            // again all the same: entering Play Mode reloads the domain.
            StartLogCapture(clearLogs: string.IsNullOrEmpty(TestCallRequestID.Value));
            _results.Clear();
            _summary.Clear();
            _summary.TotalTests = testCount;

            UnityMcpPluginEditor.Instance.LogInfo("Run {testMode} started: {testCount} tests.",
                typeof(TestResultCollector), TestModeAsString, testCount);
        }

        /// <summary>
        /// Clears the captured logs and captures the new ones. tests-run calls it before Unity starts the run: a run can fail
        /// before <see cref="RunStarted"/> (a throwing IPrebuildSetup), and <see cref="OnError"/> reports this run's errors.
        /// </summary>
        internal void StartLogCapture(bool clearLogs = true)
        {
            if (clearLogs)
            {
                lock (_logsMutex)
                {
                    _logs.Clear();
                }
            }

            // Subscribe to log messages (using threaded version to catch logs from all threads)
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        internal void StopLogCapture()
            => Application.logMessageReceivedThreaded -= OnLogMessageReceived;

        public void RunFinished(ITestResultAdaptor result)
        {
            UnityMcpPluginEditor.Instance.LogInfo("RunFinished", typeof(TestResultCollector));

            StopLogCapture();

            var duration = DateTime.Now - startTime;
            _summary.Duration = duration;
            // result.Test is the whole unfiltered tree (and so is RunStarted's before com.unity.test-framework 1.4): take every
            // count from the run's root result, so the summary adds up (inconclusive tests are reported as skipped, like their rows).
            _summary.PassedTests = result.PassCount;
            _summary.FailedTests = result.FailCount;
            _summary.SkippedTests = result.SkipCount + result.InconclusiveCount;
            _summary.TotalTests = _summary.PassedTests + _summary.FailedTests + _summary.SkippedTests;
            // A suite-level failure (OneTimeTearDown error, cancelled run) fails the root without failing any test.
            if (_summary.FailedTests > 0 || result.TestStatus == TestStatus.Failed)
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

            UnityMcpPluginEditor.Instance.LogInfo("Run {testMode} finished with {totalTests} test results. Result status: {status}",
                typeof(TestResultCollector), TestModeAsString, _summary.TotalTests, result.TestStatus);
            UnityMcpPluginEditor.Instance.LogInfo("Final duration: {duration:mm\\:ss\\.fff}. Completed: {completed}/{total}",
                typeof(TestResultCollector), duration, _results.Count, _summary.TotalTests);

            EnsureMcpConnection();

            // The testResults RQL source: this run's results, also after the request is answered.
            Rql.RqlTests.SetLastResults(_results.Select(r => new Rql.RqlTestResult
            {
                name = r.Name,
                status = r.Status.ToString(),
                durationMs = System.Math.Round(r.Duration.TotalMilliseconds, 1),
                message = string.IsNullOrEmpty(r.Message) ? null : r.Message,
                stackTrace = string.IsNullOrEmpty(r.StackTrace) ? null : r.StackTrace,
            }));

            var requestId = TestCallRequestID.Value;
            var resultQuery = ResultQuery.Value;
            if (string.IsNullOrEmpty(requestId) == false)
            {
                var mcpPlugin = UnityMcpPluginEditor.Instance.McpPluginInstance;
                ResponseCallTool response;
                try
                {
                    var emptyRunError = EmptyRunError(_summary.TotalTests, result.TestStatus);
                    if (emptyRunError != null)
                        response = ResponseCallValueTool<TestRunResponse>.Error(emptyRunError);
                    else if (string.IsNullOrWhiteSpace(resultQuery) == false)
                    {
                        // tests-run declares a TestRunResponse output schema: the summary stays structured, the text is the projection.
                        var summaryOnly = new TestRunResponse { Summary = _summary };
                        response = ProjectedResponse(_summary, resultQuery!,
                            mcpPlugin?.McpManager.Reflector.JsonSerializer.SerializeToNode(summaryOnly));
                    }
                    else
                    {
                        var structuredResponse = CreateStructuredResponse(
                            includePassingTests: IncludePassingTests.Value,
                            includeMessage: IncludeMessage.Value,
                            includeLogs: IncludeLogs.Value,
                            includeMessageStacktrace: IncludeMessageStacktrace.Value,
                            includeLogsStacktrace: IncludeLogsStacktrace.Value);
                        if (mcpPlugin == null) throw new InvalidOperationException("MCP Plugin instance is not available.");
                        response = ResponseCallValueTool<TestRunResponse>
                            .SuccessStructured(mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(structuredResponse));
                    }
                }
                catch (Exception ex)
                {
                    // Still answer and free the lock: otherwise both wait for the whole lease.
                    UnityMcpPluginEditor.Instance.LogError("Building the test run response failed: {message}",
                        typeof(TestResultCollector), ex.Message);
                    response = ResponseCallValueTool<TestRunResponse>.Error($"Test run finished, but building its response failed: {ex.Message}");
                }

                Tool_Tests.CompleteTestRunAfterTeardown(requestId, response);
            }
            else
            {
                Tool_Tests.ClearActiveTestRun(requestId);
                UnityMcpPluginEditor.Instance.LogTrace(
                    "Test run finished without a pending MCP request ID (likely a manual test run from the Unity Editor). No results will be delivered over MCP.",
                    typeof(TestResultCollector));
            }
        }

        /// <summary>
        /// Unity reports a run that fails with an error (a throwing IPrebuildSetup, an exception in one of its own tasks) here
        /// and never calls <see cref="RunFinished"/> for it: answer the request with the error and free the lock.
        /// </summary>
        public void OnError(string message)
        {
            // Stop capturing first: this method's own error log must not be listed among the run's errors.
            StopLogCapture();
            UnityMcpPluginEditor.Instance.LogError("Test run failed: {message}", typeof(TestResultCollector), message);

            var requestId = TestCallRequestID.Value;
            if (string.IsNullOrEmpty(requestId))
                return;

            EnsureMcpConnection();
            Tool_Tests.CompleteTestRunAfterTeardown(requestId, ResponseCallValueTool<TestRunResponse>.Error(RunErrorMessage(message, GetLogs())));
        }

        /// <summary>
        /// The error for a run Unity finished without running a single test, else null: the filter check before the run
        /// should refuse such filters, and an empty run must never read as a result.
        /// </summary>
        internal static string? EmptyRunError(int totalTests, TestStatus runStatus)
            => totalTests == 0 && runStatus != TestStatus.Failed
                ? "Unity ran no tests: the filters select no test in its test tree."
                : null;

        /// <summary><paramref name="message"/> plus the last errors logged during the run: Unity's message alone names no cause.</summary>
        internal static string RunErrorMessage(string message, IReadOnlyList<TestLogEntry> logs)
        {
            const int maxErrors = 10;
            var errors = logs
                .Where(log => log.LogLevel >= TestLogEntry.ToLogLevel(LogType.Assert))
                .Select(log => log.Condition)
                .Where(condition => condition != message)
                .ToList();
            var text = $"Test run failed: {message}";
            if (errors.Count > 0)
                text += "\n\nErrors logged during the run:\n- " + string.Join("\n- ", errors.Skip(Math.Max(0, errors.Count - maxErrors)));
            return text;
        }

        static void EnsureMcpConnection()
        {
            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();

            if (!EnvironmentUtils.IsCi())
                UnityMcpPluginEditor.ConnectIfNeeded();
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
                    Status = ConvertTestStatus(result.TestStatus),
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

                UnityMcpPluginEditor.Instance.LogInfo("{emoji} Test finished ({counter}/{total}): {testName} - {testStatus}",
                    typeof(TestResultCollector), statusEmoji, _results.Count, _summary.TotalTests, result.Test.FullName, result.TestStatus);

                // Update duration as tests complete
                _summary.Duration = DateTime.Now - startTime;

                // Check if all tests are complete
                if (_results.Count >= _summary.TotalTests)
                {
                    UnityMcpPluginEditor.Instance.LogInfo("All tests completed via TestFinished. Final duration: {duration:mm\\:ss\\.fff}",
                        typeof(TestResultCollector), _summary.Duration);
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

        /// <summary>
        /// The answer for a tests-run 'result' query: one summary line, then the query over <c>testResults</c> as compact
        /// RQL text. A query that fails still answers with the summary and the query's error. <paramref name="structured"/>
        /// (the summary as a <see cref="TestRunResponse"/>) is attached as structured content: MCP clients reject a success
        /// without it from a tool that declares an output schema.
        /// </summary>
        internal static ResponseCallTool ProjectedResponse(TestSummaryData summary, string resultQuery, System.Text.Json.Nodes.JsonNode? structured = null)
        {
            var stages = resultQuery.Trim();
            var query = stages.StartsWith("testResults", System.StringComparison.Ordinal) ? stages : "testResults | " + stages;
            var head = $"{summary.Status}: {summary.PassedTests} passed, {summary.FailedTests} failed, {summary.SkippedTests} skipped " +
                $"of {summary.TotalTests} in {summary.Duration.TotalSeconds:0.0}s";
            var r = Rql.RqlUnity.Engine.Execute(query);
            var body = (r.Ok ? "" : "result query failed: ") + Rql.ToolFormat.RqlToolBinding.Instance.Show(r);
            var text = ResponseCallTool.Success(head + "\n" + body);
            if (structured == null)
                return text;
            var response = ResponseCallValueTool<TestRunResponse>.SuccessStructured(structured);
            response.Content = text.Content;
            return response;
        }

        TestRunResponse CreateStructuredResponse(bool includePassingTests, bool includeMessage, bool includeMessageStacktrace, bool includeLogs, bool includeLogsStacktrace)
        {
            var results = GetResults();
            var summary = GetSummary();
            var logs = GetLogs();

            var response = new TestRunResponse
            {
                Summary = summary,
                Results = new List<TestResultData>()
            };

            // Filter test results based on includePassingTests, includeMessage and includeMessageStacktrace
            foreach (var result in results)
            {
                // Skip passing tests if includePassingTests is false
                if (!includePassingTests && result.Status == TestResultStatus.Passed)
                    continue;

                var filteredResult = new TestResultData
                {
                    Name = result.Name,
                    Status = result.Status,
                    Duration = result.Duration,
                    Message = includeMessage ? result.Message : null,
                    StackTrace = includeMessageStacktrace ? result.StackTrace : null
                };
                response.Results.Add(filteredResult);
            }

            // Include logs if requested
            if (includeLogs && logs.Any())
            {
                var minLogLevel = TestLogEntry.ToLogLevel((LogType)IncludeLogsMinLevel.Value);
                response.Logs = logs
                    .Where(log => log.LogLevel >= minLogLevel)
                    .Select(log => includeLogsStacktrace
                        ? log
                        : new TestLogEntry(log.Type, log.Condition, null, log.Timestamp))
                    .ToList();
            }

            return response;
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

        static TestResultStatus ConvertTestStatus(TestStatus testStatus)
        {
            return testStatus switch
            {
                TestStatus.Passed => TestResultStatus.Passed,
                TestStatus.Failed => TestResultStatus.Failed,
                TestStatus.Skipped => TestResultStatus.Skipped,
                _ => TestResultStatus.Skipped
            };
        }

    }
}
