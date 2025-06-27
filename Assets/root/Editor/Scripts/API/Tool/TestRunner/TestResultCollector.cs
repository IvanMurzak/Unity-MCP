#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestResultCollector : ICallbacks
    {
        private readonly List<TestResultData> _results = new();
        private readonly List<string> _logs = new();
        private readonly TaskCompletionSource<bool> _completionSource = new();
        private readonly TestSummaryData _summary = new();
        private DateTime _startTime;
        private readonly TestMode _testMode;
        private readonly int _runNumber;

        public List<TestResultData> GetResults() => _results;
        public TestSummaryData GetSummary() => _summary;
        public List<string> GetLogs() => _logs;
        public TestMode GetTestMode() => _testMode;

        public TestResultCollector(TestMode testMode, int runNumber = 1)
        {
            _testMode = testMode;
            _runNumber = runNumber;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _startTime = DateTime.Now;
            var testCount = CountTests(testsToRun);

            _summary.TotalTests = testCount;
            Debug.Log($"[TestRunner] Run {_runNumber} ({_testMode}) started: {testCount} tests.");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            var endTime = DateTime.Now;
            var duration = endTime - _startTime;
            _summary.Duration = duration;
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

            Debug.Log($"[TestRunner] Run {_runNumber} ({_testMode}) finished with {CountTests(result.Test)} test results. Result status: {result.TestStatus}");
            Debug.Log($"[TestRunner] Final duration: {duration:mm\\:ss\\.fff}. Completed: {_results.Count}/{_summary.TotalTests}");

            _completionSource.TrySetResult(true);
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
                    _ => ""
                };

                Debug.Log($"[TestRunner] {statusEmoji} Test finished: {result.Test.FullName} - {result.TestStatus} ({_results.Count}/{_summary.TotalTests})");

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
                _summary.Duration = DateTime.Now - _startTime;

                // Check if all tests are complete
                if (_results.Count >= _summary.TotalTests)
                {
                    Debug.Log($"[TestRunner] All tests completed via TestFinished. Final duration: {_summary.Duration:mm\\:ss\\.fff}");
                    _completionSource.TrySetResult(true);
                }
            }
        }

        public async Task WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                var completedTask = await Task.WhenAny(_completionSource.Task, tcs.Task);
                if (completedTask == tcs.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await _completionSource.Task; // Re-await to get the result or exception
            }
        }

        public string FormatTestResults()
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

                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        output.AppendLine($"  Message: {result.Message}");
                    }

                    if (!string.IsNullOrEmpty(result.StackTrace))
                    {
                        output.AppendLine($"  Stack Trace: {result.StackTrace}");
                    }
                    output.AppendLine();
                }
            }

            // Console logs
            if (logs.Any())
            {
                output.AppendLine("=== CONSOLE LOGS ===");
                foreach (var log in logs)
                {
                    output.AppendLine(log);
                }
            }

            return output.ToString();
        }

        public static int CountTests(ITestAdaptor test)
        {
            if (!test.HasChildren)
                return test.IsSuite
                    ? 0
                    : 1;

            return test.Children.Sum(CountTests);
        }
    }
}