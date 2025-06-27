#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class CombinedTestResultCollector
    {
        private readonly List<TestResultData> _allResults = new();
        private readonly List<string> _allLogs = new();
        private readonly List<TestResultCollector> _collectors = new();
        private TestSummaryData _combinedSummary = new();
        private TestSummaryData _editModeSummary = new();
        private TestSummaryData _playModeSummary = new();

        public List<TestResultData> GetResults() => _allResults;
        public TestSummaryData GetSummary() => _combinedSummary;
        public TestSummaryData GetEditModeSummary() => _editModeSummary;
        public TestSummaryData GetPlayModeSummary() => _playModeSummary;
        public List<string> GetLogs() => _allLogs;
        public List<TestResultCollector> GetAllCollectors() => _collectors;

        public void AddResults(TestResultCollector collector)
        {
            var results = collector.GetResults();
            var summary = collector.GetSummary();
            var logs = collector.GetLogs();
            var testMode = collector.GetTestMode();

            _collectors.Add(collector);
            _allResults.AddRange(results);
            _allLogs.AddRange(logs);

            _combinedSummary.TotalTests += summary.TotalTests;
            _combinedSummary.PassedTests += summary.PassedTests;
            _combinedSummary.FailedTests += summary.FailedTests;
            _combinedSummary.SkippedTests += summary.SkippedTests;

            if (testMode == TestMode.EditMode)
            {
                _editModeSummary = summary;
            }
            else if (testMode == TestMode.PlayMode)
            {
                _playModeSummary = summary;
            }
        }

        public void SetTotalDuration(TimeSpan duration)
        {
            _combinedSummary.Duration = duration;
            _combinedSummary.Status = _combinedSummary.FailedTests > 0
                ? TestRunStatus.Failed
                : TestRunStatus.Passed;
        }

        public string FormatCombinedResults()
        {
            var results = GetResults();
            var summary = GetSummary();
            var editModeSummary = GetEditModeSummary();
            var playModeSummary = GetPlayModeSummary();
            var logs = GetLogs();

            var output = new StringBuilder();
            output.AppendLine("[Success] Combined test execution completed.");
            output.AppendLine();

            // Combined Summary
            output.AppendLine("=== COMBINED TEST SUMMARY ===");
            var overallStatusColored = summary.Status == TestRunStatus.Passed
                ? "<color=green>✅</color>"
                : "<color=red>❌</color>";
            output.AppendLine($"Overall Status: {summary.Status} {overallStatusColored}");
            output.AppendLine($"Total Tests: {summary.TotalTests}");
            output.AppendLine($"Total Passed: {summary.PassedTests}");
            output.AppendLine($"Total Failed: {summary.FailedTests}");
            output.AppendLine($"Total Skipped: {summary.SkippedTests}");
            output.AppendLine($"Total Duration: {summary.Duration:hh\\:mm\\:ss\\.fff}");
            output.AppendLine();

            // EditMode Summary
            if (editModeSummary.TotalTests > 0)
            {
                var editModeStatusColored = editModeSummary.Status == TestRunStatus.Passed
                    ? "<color=green>✅</color>"
                    : "<color=red>❌</color>";
                output.AppendLine("=== EDITMODE TEST SUMMARY ===");
                output.AppendLine($"Status: {editModeSummary.Status} {editModeStatusColored}");
                output.AppendLine($"Total: {editModeSummary.TotalTests}");
                output.AppendLine($"Passed: {editModeSummary.PassedTests}");
                output.AppendLine($"Failed: {editModeSummary.FailedTests}");
                output.AppendLine($"Skipped: {editModeSummary.SkippedTests}");
                output.AppendLine($"Duration: {editModeSummary.Duration:hh\\:mm\\:ss\\.fff}");
                output.AppendLine();
            }

            // PlayMode Summary
            if (playModeSummary.TotalTests > 0)
            {
                var playModeStatusColored = playModeSummary.Status == TestRunStatus.Passed
                    ? "<color=green>✅</color>"
                    : "<color=red>❌</color>";
                output.AppendLine("=== PLAYMODE TEST SUMMARY ===");
                output.AppendLine($"Status: {playModeSummary.Status} {playModeStatusColored}");
                output.AppendLine($"Total: {playModeSummary.TotalTests}");
                output.AppendLine($"Passed: {playModeSummary.PassedTests}");
                output.AppendLine($"Failed: {playModeSummary.FailedTests}");
                output.AppendLine($"Skipped: {playModeSummary.SkippedTests}");
                output.AppendLine($"Duration: {playModeSummary.Duration:hh\\:mm\\:ss\\.fff}");
                output.AppendLine();
            }

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
    }
}