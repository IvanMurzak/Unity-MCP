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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Tests
    {
        public const string TestsRunToolId = "tests-run";
        [AiTool
        (
            TestsRunToolId,
            Title = "Tests / Run",
            Enabled = true
        )]
        [AiSkillDescription("Execute Unity tests (`EditMode` or `PlayMode`) and return per-test results. " +
            "Supports filtering by test assembly, namespace, class, and method. " +
            "Refreshes the AssetDatabase first; defers execution across domain reloads if scripts changed. " +
            "Precondition: every open scene must be saved — dirty scenes abort the run.")]
        [AiSkillBody("Execute Unity tests and return detailed results. " +
            "Supports filtering by test mode, assembly, namespace, class, and method. " +
            "Recommended to use '" + nameof(TestMode.EditMode) + "' for faster iteration during development. " +
            "Precondition: every open scene MUST be saved (no unsaved changes). If any open scene is dirty, " +
            "this tool throws an InvalidOperationException listing the dirty scenes; save them and retry.\n\n" +
            "## Filters\n\n" +
            "- `testMode` (default `EditMode`) — `EditMode` or `PlayMode`. EditMode is faster; prefer it during iteration.\n" +
            "- `testAssembly` / `testNamespace` / `testClass` / `testMethod` — optional, layered filters. Namespace and " +
            "class filters become regex `groupNames` so the validation count and Unity's execution stay in sync. " +
            "`testMethod` must be fully qualified (`Namespace.FixtureName.TestName`).\n" +
            "- `query` — RQL over the `tests` source instead of the four filters: any selection, run as ONE Test Runner run, " +
            "e.g. `tests | where fixture == \"PlayerTests\" or categories contains \"Smoke\"`. One mode per run; the mode comes " +
            "from the selected tests. Explore first with rql-query: `tests | where fullName contains \"Spline\" | select fullName mode`.\n" +
            "- `result` — RQL stages applied to the results (source `testResults`: name status durationMs message stackTrace), " +
            "e.g. `where status == \"Failed\" | select name message`; the answer is then a summary line plus compact RQL text.\n\n" +
            "## Response toggles\n\n" +
            "- `includePassingTests` (default `false`) — include details for passing tests; otherwise only failing test details are returned.\n" +
            "- `includeMessages` (default `true`) — include per-test result messages.\n" +
            "- `includeStacktrace` (default `false`) — include stack traces for failing tests.\n" +
            "- `includeLogs` (default `false`) — include console logs captured during the run.\n" +
            "- `logType` (default `Warning`) — minimum log severity to include.\n" +
            "- `includeLogsStacktrace` (default `false`) — include log stack traces (large payload; use sparingly).\n\n" +
            "## Domain reloads\n\n" +
            "If the AssetDatabase refresh triggers compilation, the tool persists the run parameters to `SessionState`, " +
            "returns `Processing`, and resumes the run automatically after the reload completes. Pre-existing " +
            "compilation errors short-circuit the run and return the error details so the caller can fix the project first.")]
        [Description("Execute Unity tests and return detailed results. " +
            "Supports filtering by test mode, assembly, namespace, class, and method. " +
            "Recommended to use '" + nameof(TestMode.EditMode) + "' for faster iteration during development. " +
            "Precondition: every open scene MUST be saved (no unsaved changes). If any open scene is dirty, " +
            "this tool throws an InvalidOperationException listing the dirty scenes; save them and retry.")]
        public static async Task<ResponseCallValueTool<TestRunResponse>> Run
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
            [Description("RQL over the 'tests' source selecting the tests (instead of the filters above), run as one run. " +
                "Example: tests | where fullName contains \"Player\" and mode == \"EditMode\"")]
            string? query = null,
            [Description("RQL stages applied to the results (source 'testResults': name status durationMs message stackTrace); " +
                "the answer is a summary line plus compact text. Example: where status == \"Failed\" | select name message")]
            string? result = null,

            [Description("Include details for all tests, both passing and failing (default: false). If you just need details for failing tests, set to false.")]
            bool includePassingTests = false,
            [Description("Include test result messages in the test results (default: true). If you just need pass/fail status, set to false.")]
            bool includeMessages = true,
            [Description("Include stack traces in the test results (default: false).")]
            bool includeStacktrace = false,

            [Description("Include console logs in the test results (default: false).")]
            bool includeLogs = false,
            [Description("Log type filter for console logs. Options: '" + nameof(LogType.Log) + "', '" + nameof(LogType.Warning) + "', '" + nameof(LogType.Assert) + "', '" + nameof(LogType.Error) + "', '" + nameof(LogType.Exception) + "'. (default: '" + nameof(LogType.Warning) + "')")]
            LogType logType = LogType.Warning,
            [Description("Include stack traces for console logs in the test results (default: false). This is huge amount of data, use only if really needed.")]
            bool includeLogsStacktrace = false,

            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallValueTool<TestRunResponse>.Error("Original request with valid RequestID must be provided.");

            return await MainThread.Instance.RunAsync(() =>
            {
                if (IsTestRunActive(out var activeRequestId))
                    return ResponseCallValueTool<TestRunResponse>
                        .Error(Error.TestsAlreadyRunning(activeRequestId))
                        .SetRequestID(requestId);

                // Abort before any state mutation if any open scene has unsaved changes.
                // Running tests against a dirty scene is unsafe: Unity may reload the scene on
                // play-mode entry, silently discarding edits and producing non-reproducible
                // results. Throws InvalidOperationException listing every dirty scene so the
                // caller can save and retry. MUST run before test-run state writes and before
                // AssetDatabase.Refresh so nothing is side-effected on abort.
                ThrowIfAnyOpenSceneIsDirty();

                // An RQL selection replaces the four filters. It is evaluated only after the AssetDatabase.Refresh below
                // (and, when scripts recompile, in the new domain): tests written since the last compile must be selectable.
                var selection = string.IsNullOrWhiteSpace(query) ? null : query;
                if (selection != null && (!string.IsNullOrEmpty(testAssembly) || !string.IsNullOrEmpty(testNamespace) ||
                    !string.IsNullOrEmpty(testClass) || !string.IsNullOrEmpty(testMethod)))
                    return ResponseCallValueTool<TestRunResponse>
                        .Error("Give either 'query' or the testAssembly / testNamespace / testClass / testMethod filters, not both.")
                        .SetRequestID(requestId);

                if (!TryBeginTestRun(requestId, out activeRequestId))
                    return ResponseCallValueTool<TestRunResponse>
                        .Error(Error.TestsAlreadyRunning(activeRequestId))
                        .SetRequestID(requestId);

                // Save display options to SessionState BEFORE AssetDatabase.Refresh —
                // these must be persisted before a potential domain reload
                TestResultCollector.IncludePassingTests.Value = includePassingTests;
                TestResultCollector.IncludeMessage.Value = includeMessages;
                TestResultCollector.IncludeMessageStacktrace.Value = includeStacktrace;

                TestResultCollector.IncludeLogs.Value = includeLogs;
                TestResultCollector.IncludeLogsMinLevel.Value = (int)logType;
                TestResultCollector.IncludeLogsStacktrace.Value = includeLogsStacktrace;
                TestResultCollector.ResultQuery.Value = result ?? string.Empty;

                // Trigger AssetDatabase.Refresh to detect and compile any changed scripts
                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                    Debug.Log($"[TestRunner] Refreshing AssetDatabase before running {testMode} tests...");

                UnityEditor.AssetDatabase.Refresh();

                // Check if compilation was triggered (scripts changed on disk)
                if (UnityEditor.EditorApplication.isCompiling)
                {
                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                        Debug.Log($"[TestRunner] Scripts are compiling. Deferring test run until after domain reload.");

                    // Save filter params to SessionState (survives domain reload)
                    SavePendingTestRun(testMode, testAssembly, testNamespace, testClass, testMethod, selection);

                    // Register resume callback now — handles compilation failure (no domain reload).
                    // If compilation succeeds, domain reload destroys this registration,
                    // but the static constructor re-registers it after reload.
                    UnityEditor.EditorApplication.update += ResumePendingTestRunOnce;

                    return ResponseCallValueTool<TestRunResponse>
                        .Processing()
                        .SetRequestID(requestId);
                }

                // Check for pre-existing compilation errors (no new compilation triggered)
                if (UnityEditor.EditorUtility.scriptCompilationFailed)
                {
                    ClearActiveTestRun(requestId);
                    var errorDetails = ScriptUtils.GetCompilationErrorDetails();
                    return ResponseCallValueTool<TestRunResponse>
                        .Error($"Cannot run tests: Unity project has compilation errors.\n\n{errorDetails}")
                        .SetRequestID(requestId);
                }

                // No compilation needed — run tests immediately
                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                    Debug.Log($"[TestRunner] No compilation needed. Running {testMode} tests immediately.");

                try
                {
                    var filterParams = new TestFilterParameters(testAssembly, testNamespace, testClass, testMethod);
                    if (selection != null)
                    {
                        if (!TryApplySelection(selection, ref filterParams, out testMode, out var selectError))
                        {
                            ClearActiveTestRun(requestId);
                            return ResponseCallValueTool<TestRunResponse>.Error(selectError!).SetRequestID(requestId);
                        }
                    }

                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                        Debug.Log($"[TestRunner] Running {testMode} tests with filters: {filterParams}");

                    // Validation waits for Unity's test tree (up to TestListTimeout on a cold Editor), longer than the
                    // server's plugin timeout (10 s by default), after which the server retries this call with the same
                    // request id and gets "already running". So answer Processing now; validation failures and the run's
                    // results are delivered through the request id.
                    _ = ValidateAndExecuteTestRun(requestId, testMode, filterParams);

                    return ResponseCallValueTool<TestRunResponse>.Processing().SetRequestID(requestId);
                }
                catch (Exception ex)
                {
                    if (UnityMcpPlugin.IsLogEnabled(LogLevel.Error))
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"[TestRunner] ------------------------------------- Exception {testMode} tests.");
                    }
                    ClearActiveTestRun(requestId);
                    return ResponseCallValueTool<TestRunResponse>.Error(Error.TestExecutionFailed(ex.Message)).SetRequestID(requestId);
                }
            });
        }

        /// <summary>Select the tests of <paramref name="query"/> into <paramref name="filterParams"/> (groups and names).</summary>
        static bool TryApplySelection(string query, ref TestFilterParameters filterParams, out TestMode mode, out string? error)
        {
            if (!TrySelectTests(query, out var groups, out var names, out mode, out error)) return false;
            filterParams.Groups = groups;
            filterParams.SelectedNames = names;
            return true;
        }

        internal static bool TrySelectTests(string query, out string[]? groups, out TestMode mode, out string? error)
            => TrySelectTests(query, out groups, out _, out mode, out error);

        /// <summary>
        /// Run <paramref name="query"/> (RQL over the <c>tests</c> source) and turn the selected tests into one anchored
        /// groupNames pattern. The mode comes from the tests; a selection mixing EditMode and PlayMode is refused.
        /// </summary>
        internal static bool TrySelectTests(string query, out string[]? groups, out List<string> names, out TestMode mode, out string? error)
        {
            groups = null;
            names = new List<string>();
            mode = TestMode.EditMode;
            error = null;
            // The whole selection is needed, not a page of it: lift the row / output budgets to the engine's hard limits.
            var r = Rql.RqlUnity.Engine.Execute(query, new global::Rql.Budgets { MaxRows = 10_000, MaxOutputBytes = 64 * 1024 * 1024 });
            if (!r.Ok)
            {
                error = "query failed: " + Rql.ToolFormat.RqlToolBinding.Instance.Show(r);
                return false;
            }
            if (r.Truncated)
            {
                error = $"the query selects more tests than one run takes ({r.Total}); narrow it down";
                return false;
            }
            names = SelectedFullNames(r);
            if (names.Count == 0)
            {
                error = "the query selected no tests (it must return tests from the 'tests' source, or rows with a fullName column): " + query;
                return false;
            }
            var known = Rql.RqlTests.ByName;
            var unknown = names.Where(n => !known.ContainsKey(n)).ToList();
            if (unknown.Count > 0)
            {
                error = $"not tests of the 'tests' source: {string.Join(", ", unknown.Take(5))}";
                return false;
            }
            var modes = names.Select(n => known[n].mode).Distinct().ToList();
            if (modes.Count > 1)
            {
                error = "the selection mixes EditMode and PlayMode tests; run one mode at a time (add: where mode == \"EditMode\")";
                return false;
            }
            mode = modes[0] == nameof(TestMode.PlayMode) ? TestMode.PlayMode : TestMode.EditMode;
            groups = new[] { CreateTestsRegexPattern(names) };
            return true;
        }

        /// <summary>The selected full names: a fullName column (or the only column) of rows, or the tests themselves.</summary>
        static List<string> SelectedFullNames(global::Rql.QueryResult r)
        {
            static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
            IEnumerable<string?> names;
            if (r.Columns != null && r.Rows != null)
            {
                var column = r.Columns.FindIndex(c => string.Equals(c, "fullName", StringComparison.OrdinalIgnoreCase));
                if (column < 0 && r.Columns.Count == 1) column = 0;
                names = column < 0 ? Enumerable.Empty<string?>() : r.Rows.Select(row => Str(row[column]));
            }
            else
                names = (r.Items ?? new List<JsonNode?>()).Select(i => Str((i as JsonObject)?["fullName"]) ?? Str(i));
            return names.Where(n => n != null).Distinct(StringComparer.Ordinal).ToList()!;
        }

        /// <summary>
        /// One groupNames pattern selecting exactly these tests, parameterized cases included:
        /// <c>^(?:Ns\.Fixture\.A|Ns\.Fixture\.B)(\(.*\))?$</c>. One pattern, not one per test: .NET caches only 15 regexes.
        /// </summary>
        internal static string CreateTestsRegexPattern(IEnumerable<string> fullNames)
            => "^(?:" + string.Join("|", fullNames.Distinct(StringComparer.Ordinal).Select(EscapeRegex)) + @")(\(.*\))?$";

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

            if (filterParams.Groups != null)
                groupNames.AddRange(filterParams.Groups);

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

        /// <summary>
        /// Unity's own test tree for <paramref name="testMode"/>: how many tests the filters select, and which of the RQL
        /// selection's names (<see cref="TestFilterParameters.SelectedNames"/>) match no test there (a fixture with
        /// arguments, a generic fixture: the tests source names them differently).
        /// </summary>
        static async Task<(int count, List<string> unmatched)> MatchTestTree(TestRunnerApi testRunnerApi, TestMode testMode, TestFilterParameters filterParams)
        {
            var tcs = new TaskCompletionSource<(int, List<string>)>();

            testRunnerApi.RetrieveTestList(testMode, (testRoot) =>
            {
                var testCount = testRoot != null
                    ? CountFilteredTests(testRoot, filterParams)
                    : 0;
                var unmatched = filterParams.SelectedNames == null ? new List<string>()
                    : testRoot == null ? filterParams.SelectedNames.ToList()
                    : UnmatchedNames(testRoot, filterParams.SelectedNames);

                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                    Debug.Log($"[TestRunner] {testCount} {testMode} tests matched for {filterParams}");

                tcs.SetResult((testCount, unmatched));
            });

            // The first retrieval after the Editor starts builds the whole test tree, which can take well over a few
            // seconds; a timeout is reported as such (ValidateTestFilters), never as "no tests found".
            var timeoutTask = Task.Delay(TestListTimeout);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
                throw new TimeoutException($"Unity's test list was not retrieved within {TestListTimeout.TotalSeconds:0} s; try again");

            return await tcs.Task;
        }

        static readonly TimeSpan TestListTimeout = TimeSpan.FromSeconds(60);

        internal static List<string> UnmatchedNames(ITestAdaptor root, IReadOnlyList<string> names)
        {
            // A leaf's full name is the method's, with "(args)" for a parameterized case.
            var found = new HashSet<string>(StringComparer.Ordinal);
            void Walk(ITestAdaptor t)
            {
                if (t.HasChildren) foreach (var c in t.Children) Walk(c);
                else if (!t.IsSuite)
                {
                    var name = t.FullName;
                    var paren = name.IndexOf('(');
                    found.Add(paren > 0 && name.EndsWith(")") ? name.Substring(0, paren) : name);
                }
            }
            Walk(root);
            return names.Where(n => !found.Contains(n)).ToList();
        }

        static async Task<string?> ValidateTestFilters(TestRunnerApi testRunnerApi, TestMode testMode, TestFilterParameters filterParams)
        {
            try
            {
                var (testCount, unmatched) = await MatchTestTree(testRunnerApi, testMode, filterParams);
                if (testCount == 0)
                    return Error.NoTestsFound(filterParams);
                // Never run a silent subset of an RQL selection.
                if (unmatched.Count > 0)
                    return Error.TestExecutionFailed($"{unmatched.Count} selected test(s) are not in Unity's {testMode} test tree " +
                        $"(fixtures with arguments or generic fixtures are listed differently): {string.Join(", ", unmatched.Take(5))}");

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

                // Check the RQL selection with the same pattern Unity receives as Filter.groupNames
                if (!matches && filterParams.HasGroups)
                    matches = filterParams.Groups!.Any(pattern => Regex.IsMatch(test.FullName, pattern));

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
