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
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_TestRunner
    {
        [McpPluginTool
        (
            "TestRunner_Run",
            Title = "Run Unity Tests"
        )]
        [Description(@"Execute Unity tests and return detailed results. Supports filtering by test mode, assembly, namespace, class, and method.
Be default recommended to use 'EditMode' for faster iteration during development.")]
        public static async Task<ResponseCallTool> Run
        (
            [Description("Test mode to run. Options: '" + nameof(TestMode.EditMode) + "', '" + nameof(TestMode.PlayMode) + "'. Default: '" + nameof(TestMode.EditMode) + "'")]
            TestMode testMode = TestMode.EditMode,
            [Description("Specific test assembly name to run (optional). Example: 'Assembly-CSharp-Editor-testable'")]
            string? testAssembly = null,
            [Description("Specific test namespace to run (optional). Example: 'MyTestNamespace'")]
            string? testNamespace = null,
            [Description("Specific test class name to run (optional). Example: 'MyTestClass'")]
            string? testClass = null,
            [Description("Specific fully qualified test method to run (optional). Example: 'MyTestNamespace.FixtureName.TestName'")]
            string? testMethod = null,

            [Description("Include test result messages in the test results (default: true). If just need pass/fail status, set to false.")]
            bool includeMessages = true,
            [Description("Include stack traces in the test results (default: false).")]
            bool includeStacktrace = false,

            [Description("Include console logs in the test results (default: false).")]
            bool includeLogs = false,
            [Description("Log type filter for console logs. Options: '" + nameof(LogType.Log) + "', '" + nameof(LogType.Warning) + "', '" + nameof(LogType.Assert) + "', '" + nameof(LogType.Error) + "', '" + nameof(LogType.Exception) + "'. (default: Warning)")]
            LogType logType = LogType.Warning,
            [Description("Include stack traces for console logs in the test results (default: false). This is huge amount of data, use only if really needed.")]
            bool includeLogsStacktrace = false,

            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            return await MainThread.Instance.RunAsync(async () =>
            {
                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                    Debug.Log($"[TestRunner] ------------------------------------- Preparing to run {testMode} tests.");
                try
                {
                    TestResultCollector.TestCallRequestID.Value = requestId;
                    TestResultCollector.IncludeMessage.Value = includeMessages;
                    TestResultCollector.IncludeMessageStacktrace.Value = includeStacktrace;

                    TestResultCollector.IncludeLogs.Value = includeLogs;
                    TestResultCollector.IncludeLogsMinLevel.Value = (int)logType;
                    TestResultCollector.IncludeLogsStacktrace.Value = includeLogsStacktrace;

                    // Create filter parameters
                    var filterParams = new TestFilterParameters(testAssembly, testNamespace, testClass, testMethod);

                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                        Debug.Log($"[TestRunner] Running {testMode} tests with filters: {filterParams}");

                    // Validate specific test mode filter
                    var validation = await ValidateTestFilters(TestRunnerApi, testMode, filterParams);
                    if (validation != null)
                        return ResponseCallTool.Error(validation).SetRequestID(requestId);

                    var filter = CreateTestFilter(testMode, filterParams);

                    // Delay test running, first need to return response to caller
                    MainThread.Instance.Run(() => TestRunnerApi.Execute(new ExecutionSettings(filter)));

                    return ResponseCallTool.Processing().SetRequestID(requestId);
                }
                catch (Exception ex)
                {
                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Error))
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"[TestRunner] ------------------------------------- Exception {testMode} tests.");
                    }
                    return ResponseCallTool.Error(Error.TestExecutionFailed(ex.Message)).SetRequestID(requestId);
                }
            }).Unwrap();
        }

        static Filter CreateTestFilter(TestMode testMode, TestFilterParameters filterParams)
        {
            var filter = new Filter
            {
                testMode = testMode
            };

            if (!string.IsNullOrEmpty(filterParams.TestAssembly))
                filter.assemblyNames = new[] { filterParams.TestAssembly };

            var groupNames = new List<string>();
            var testNames = new List<string>();

            // Handle specific test method in FixtureName.TestName format
            if (!string.IsNullOrEmpty(filterParams.TestMethod))
                testNames.Add(filterParams.TestMethod!);

            // Handle namespace filtering with regex (shared pattern ensures validation sync)
            if (!string.IsNullOrEmpty(filterParams.TestNamespace))
                groupNames.Add(CreateNamespaceRegexPattern(filterParams.TestNamespace!));

            // Handle class filtering with regex (shared pattern ensures validation sync)
            if (!string.IsNullOrEmpty(filterParams.TestClass))
                groupNames.Add(CreateClassRegexPattern(filterParams.TestClass!));

            if (groupNames.Any())
                filter.groupNames = groupNames.ToArray();

            if (testNames.Any())
                filter.testNames = testNames.ToArray();

            return filter;
        }

        /// <summary>
        /// Creates a regex pattern for namespace filtering that matches Unity's Filter.groupNames behavior.
        /// This ensures our validation logic (CountFilteredTests) matches exactly what Unity's TestRunner will execute.
        /// Pattern: "^{namespace}\." - matches tests in the specified namespace and its sub namespaces.
        /// </summary>
        /// <param name="namespaceName">The namespace to filter by</param>
        /// <returns>Regex pattern for Unity's Filter.groupNames field</returns>
        private static string CreateNamespaceRegexPattern(string namespaceName)
            => $"^{EscapeRegex(namespaceName)}\\.";

        /// <summary>
        /// Creates a regex pattern for class filtering that matches Unity's Filter.groupNames behavior.
        /// This ensures our validation logic (CountFilteredTests) matches exactly what Unity's TestRunner will execute.
        /// Pattern: "^.*\.{className}\.[^\.]+$" - matches any test class with the specified name followed by a method name.
        /// </summary>
        /// <param name="className">The class name to filter by</param>
        /// <returns>Regex pattern for Unity's Filter.groupNames field</returns>
        static string CreateClassRegexPattern(string className)
            => $"^.*\\.{EscapeRegex(className)}\\.[^\\.]+$";

        /// <summary>
        /// Escapes special regex characters to ensure literal string matching.
        /// Used by the shared regex pattern builders to safely handle user input that may contain regex meta characters.
        /// </summary>
        /// <param name="input">The string to escape</param>
        /// <returns>Regex-safe escaped string</returns>
        static string EscapeRegex(string input)
            => Regex.Escape(input);

        static async Task<int> GetMatchingTestCount(TestRunnerApi testRunnerApi, TestMode testMode, TestFilterParameters filterParams)
        {
            try
            {
                var tcs = new TaskCompletionSource<int>();

                testRunnerApi.RetrieveTestList(testMode, (testRoot) =>
                {
                    var testCount = testRoot != null
                        ? CountFilteredTests(testRoot, filterParams)
                        : 0;

                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                        Debug.Log($"[TestRunner] {testCount} {testMode} tests matched for {filterParams}");

                    tcs.SetResult(testCount);
                });

                // Wait for the test count result with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                    throw new OperationCanceledException("Test list retrieval timed out");

                return await tcs.Task;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static async Task<string?> ValidateTestFilters(TestRunnerApi testRunnerApi, TestMode testMode, TestFilterParameters filterParams)
        {
            try
            {
                var testCount = await GetMatchingTestCount(testRunnerApi, testMode, filterParams);
                if (testCount == 0)
                    return Error.NoTestsFound(filterParams);

                return null; // No error, tests found
            }
            catch (Exception ex)
            {
                return Error.TestExecutionFailed($"Filter validation failed: {ex.Message}");
            }
        }

        static int CountFilteredTests(ITestAdaptor test, TestFilterParameters filterParams)
        {
            // If no filters are specified, count all tests
            if (!filterParams.HasAnyFilter)
                return TestResultCollector.CountTests(test);

            var count = 0;

            // Check if this test matches the filters
            if (!test.IsSuite)
            {
                var matches = false;

                // Check assembly filter using UniqueName which contains assembly information
                if (!string.IsNullOrEmpty(filterParams.TestAssembly))
                {
                    var dllIndex = test.UniqueName.ToLowerInvariant().IndexOf(".dll");
                    if (dllIndex > 0)
                    {
                        var assemblyName = test.UniqueName[..dllIndex];
                        if (assemblyName.Equals(filterParams.TestAssembly, StringComparison.OrdinalIgnoreCase))
                            matches = true;
                    }
                }

                // Check namespace filter using same regex pattern as Filter.groupNames (ensures sync with Unity's execution)
                if (!matches && !string.IsNullOrEmpty(filterParams.TestNamespace))
                {
                    var namespacePattern = CreateNamespaceRegexPattern(filterParams.TestNamespace!);
                    if (Regex.IsMatch(test.FullName, namespacePattern))
                        matches = true;
                }

                // Check class filter using same regex pattern as Filter.groupNames (ensures sync with Unity's execution)
                if (!matches && !string.IsNullOrEmpty(filterParams.TestClass))
                {
                    var classPattern = CreateClassRegexPattern(filterParams.TestClass!);
                    if (Regex.IsMatch(test.FullName, classPattern))
                        matches = true;
                }

                // Check method filter (FixtureName.TestName format, same as Filter.testNames)
                if (!matches && !string.IsNullOrEmpty(filterParams.TestMethod))
                {
                    if (test.FullName.Equals(filterParams.TestMethod, StringComparison.OrdinalIgnoreCase))
                        matches = true;
                }

                if (matches)
                    count = 1;
            }

            // Recursively check children
            if (test.HasChildren)
            {
                foreach (var child in test.Children)
                    count += CountFilteredTests(child, filterParams);
            }

            return count;
        }
    }
}
