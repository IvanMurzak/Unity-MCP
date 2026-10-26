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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    [InitializeOnLoad]
    public static partial class Tool_Tests
    {
        static readonly object _lock = new();
        static volatile TestRunnerApi? _testRunnerApi = null!;
        static volatile TestResultCollector? _resultCollector = null!;
        static volatile bool _callbacksRegistered = false;

        // SessionState keys for persisting pending test run across domain reload
        const string PendingTestRunKey = "MCP_PendingTestRun";
        const string PendingTestModeKey = "MCP_PendingTestRun_TestMode";
        const string PendingTestAssemblyKey = "MCP_PendingTestRun_TestAssembly";
        const string PendingTestNamespaceKey = "MCP_PendingTestRun_TestNamespace";
        const string PendingTestClassKey = "MCP_PendingTestRun_TestClass";
        const string PendingTestMethodKey = "MCP_PendingTestRun_TestMethod";
        const string PendingTestQueryKey = "MCP_PendingTestRun_TestQuery";
        internal const string ActiveTestRunStartedUtcTicksKey = "Unity_MCP_TestRunner_ActiveStartedUtcTicks";
        internal const string RunSceneSetupKey = "Unity_MCP_TestRunner_RunSceneSetup";

        static readonly TimeSpan ActiveTestRunTimeoutGrace = TimeSpan.FromMinutes(1);
        static readonly TimeSpan MinimumActiveTestRunLease = TimeSpan.FromMinutes(10);
        internal static readonly TimeSpan TeardownTimeout = TimeSpan.FromSeconds(60);

        // The request CompleteTestRunAfterTeardown is waiting to answer.
        static string? _waitingRequestId;

        static Tool_Tests()
        {
            _testRunnerApi ??= CreateInstance();

            // Check for pending test run that was deferred due to script recompilation + domain reload
            if (HasPendingTestRun())
                EditorApplication.update += ResumePendingTestRunOnce;
        }

        public static TestRunnerApi TestRunnerApi
        {
            get
            {
                lock (_lock)
                {
                    if (_testRunnerApi == null)
                        _testRunnerApi = CreateInstance();
                    return _testRunnerApi;
                }
            }
        }
        public static TestRunnerApi CreateInstance()
        {
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Trace))
                Debug.Log($"[{nameof(TestRunnerApi)}] Creating new instance. Existing API: {_testRunnerApi != null}, Existing Collector: {_resultCollector != null}, Callbacks Registered: {_callbacksRegistered}");

            _resultCollector ??= new TestResultCollector();
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();

            // Only register callbacks once globally to prevent accumulation
            // Unity's TestRunnerApi maintains a static callback list, so multiple RegisterCallbacks calls add duplicates
            if (!_callbacksRegistered)
            {
                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Trace))
                    Debug.Log($"[{nameof(TestRunnerApi)}] Registering callbacks for the first (and only) time.");

                testRunnerApi.RegisterCallbacks(_resultCollector);
                _callbacksRegistered = true;
            }
            else
            {
                if (UnityMcpPlugin.IsLogEnabled(LogLevel.Trace))
                    Debug.LogWarning($"[{nameof(TestRunnerApi)}] Callbacks already registered globally - skipping registration.");
            }

            return testRunnerApi;
        }

        public static void Init()
        {
            // none
        }

        static bool HasPendingTestRun()
            => !string.IsNullOrEmpty(SessionState.GetString(PendingTestRunKey, string.Empty));

        internal static TimeSpan ActiveTestRunStaleAfter
        {
            get
            {
                var configuredTimeout = TimeSpan.FromMilliseconds(Math.Max(1000, UnityMcpPluginEditor.EffectiveToolTimeoutMs));
                var staleAfter = configuredTimeout + ActiveTestRunTimeoutGrace;
                return staleAfter < MinimumActiveTestRunLease ? MinimumActiveTestRunLease : staleAfter;
            }
        }

        internal static bool IsTestRunActive(out string activeRequestId)
        {
            activeRequestId = TestResultCollector.TestCallRequestID.Value;
            if (HasPendingTestRun())
                return true;

            if (string.IsNullOrEmpty(activeRequestId))
                return false;

            if (!IsActiveTestRunStale(DateTime.UtcNow))
                return true;

            ClearActiveTestRun(activeRequestId);
            activeRequestId = string.Empty;
            return false;
        }

        internal static bool TryBeginTestRun(string requestId, out string activeRequestId)
        {
            if (IsTestRunActive(out activeRequestId))
                return false;

            TestResultCollector.TestCallRequestID.Value = requestId;
            SetActiveTestRunStartedUtc(DateTime.UtcNow);
            activeRequestId = requestId;
            return true;
        }

        internal static void ClearActiveTestRun(string? requestId = null)
        {
            if (string.IsNullOrEmpty(requestId) || TestResultCollector.TestCallRequestID.Value == requestId)
            {
                TestResultCollector.TestCallRequestID.Value = string.Empty;
                SessionState.EraseString(ActiveTestRunStartedUtcTicksKey);
            }
        }

        /// <summary>
        /// Starts Unity's run, first recording the open scenes: Unity restores exactly these when its run is over
        /// (see <see cref="CompleteTestRunAfterTeardown"/>).
        /// </summary>
        static void ExecuteTestRun(Filter filter)
        {
            SessionState.SetString(RunSceneSetupKey, OpenScenePaths());
            var testRunnerApi = TestRunnerApi; // creates the collector if needed
            _resultCollector?.StartLogCapture();
            try
            {
                testRunnerApi.Execute(new ExecutionSettings(filter));
            }
            catch
            {
                // No run, so no RunFinished / OnError to stop the capture: it would collect every Editor log until the next run.
                _resultCollector?.StopLogCapture();
                throw;
            }
        }

        internal static string OpenScenePaths()
            => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path));

        /// <summary>
        /// Unity's run is over once the Editor has left Play Mode and the scenes open when the run started are open again
        /// (Unity's RestoreSceneSetupTask, near the end of its teardown). The test runner's own "run active" flag is internal.
        /// </summary>
        internal static bool IsTestRunTeardownComplete(bool isPlayingOrWillChangePlaymode, Func<string> openScenePaths, string runScenePaths)
            => !isPlayingOrWillChangePlaymode && openScenePaths() == runScenePaths;

        /// <summary>
        /// Delivers <paramref name="response"/> to <paramref name="requestId"/> and frees the test-run lock once Unity's run
        /// is over. Unity raises RunFinished (and OnError) before that: leaving Play Mode, the post-build cleanup and the
        /// scene restore follow over the next frames, and a run started meanwhile runs on top of this one (#1049).
        /// Gives up waiting after <paramref name="timeout"/> (default <see cref="TeardownTimeout"/>).
        /// </summary>
        internal static void CompleteTestRunAfterTeardown(string requestId, ResponseCallTool response, TimeSpan? timeout = null)
        {
            // OnError can follow RunFinished (a teardown task failing): the request is answered once.
            if (_waitingRequestId == requestId)
                return;
            _waitingRequestId = requestId;

            var runScenePaths = SessionState.GetString(RunSceneSetupKey, string.Empty);
            if (string.IsNullOrEmpty(runScenePaths))
                runScenePaths = OpenScenePaths();
            var waitFor = timeout ?? TeardownTimeout;
            var deadline = DateTime.UtcNow + waitFor;

            void WaitForTeardown()
            {
                var complete = IsTestRunTeardownComplete(EditorApplication.isPlayingOrWillChangePlaymode, OpenScenePaths, runScenePaths);
                if (!complete && DateTime.UtcNow < deadline)
                    return;

                EditorApplication.update -= WaitForTeardown;
                if (_waitingRequestId == requestId)
                    _waitingRequestId = null;
                if (!complete)
                    UnityMcpPluginEditor.Instance.LogWarn("Unity's test run did not finish its teardown within {timeout} s; answering anyway.",
                        typeof(Tool_Tests), waitFor.TotalSeconds);

                SessionState.EraseString(RunSceneSetupKey);
                CompleteTestRun(requestId, response);
            }

            EditorApplication.update += WaitForTeardown;
        }

        /// <summary>Frees the test-run lock and delivers <paramref name="response"/> to <paramref name="requestId"/>.</summary>
        static void CompleteTestRun(string requestId, ResponseCallTool response)
        {
            ClearActiveTestRun(requestId);
            _ = UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
            {
                RequestId = requestId,
                Result = response.SetRequestID(requestId)
            });
        }

        internal static void SetActiveTestRunStartedUtc(DateTime utcStarted)
        {
            SessionState.SetString(ActiveTestRunStartedUtcTicksKey, utcStarted.ToUniversalTime().Ticks.ToString());
        }

        static bool IsActiveTestRunStale(DateTime utcNow)
        {
            var rawTicks = SessionState.GetString(ActiveTestRunStartedUtcTicksKey, string.Empty);
            if (!long.TryParse(rawTicks, out var ticks))
                return true;

            var startedUtc = new DateTime(ticks, DateTimeKind.Utc);
            return utcNow - startedUtc > ActiveTestRunStaleAfter;
        }

        static void SavePendingTestRun(TestMode testMode, string? testAssembly, string? testNamespace, string? testClass, string? testMethod, string? query = null)
        {
            SessionState.SetString(PendingTestQueryKey, query ?? string.Empty);
            SessionState.SetString(PendingTestRunKey, "pending");
            SessionState.SetInt(PendingTestModeKey, (int)testMode);
            SessionState.SetString(PendingTestAssemblyKey, testAssembly ?? string.Empty);
            SessionState.SetString(PendingTestNamespaceKey, testNamespace ?? string.Empty);
            SessionState.SetString(PendingTestClassKey, testClass ?? string.Empty);
            SessionState.SetString(PendingTestMethodKey, testMethod ?? string.Empty);
        }

        static void ClearPendingTestRun()
        {
            SessionState.EraseString(PendingTestRunKey);
            SessionState.EraseInt(PendingTestModeKey);
            SessionState.EraseString(PendingTestAssemblyKey);
            SessionState.EraseString(PendingTestNamespaceKey);
            SessionState.EraseString(PendingTestClassKey);
            SessionState.EraseString(PendingTestMethodKey);
            SessionState.EraseString(PendingTestQueryKey);
        }

        static void ResumePendingTestRunOnce()
        {
            // If still compiling, wait for next update tick
            if (EditorApplication.isCompiling)
                return;

            // Compilation finished (or was never happening), unsubscribe
            EditorApplication.update -= ResumePendingTestRunOnce;

            var requestId = TestResultCollector.TestCallRequestID.Value;

            // Check for compilation failure
            if (EditorUtility.scriptCompilationFailed)
            {
                ClearPendingTestRun();
                FailDeferredTestRun(requestId,
                    $"Cannot run tests: compilation errors after script recompilation.\n\n{ScriptUtils.GetCompilationErrorDetails()}");
                return;
            }

            // Compilation succeeded — load filter params from SessionState and run tests
            var testMode = (TestMode)SessionState.GetInt(PendingTestModeKey, (int)TestMode.EditMode);
            var testAssembly = SessionState.GetString(PendingTestAssemblyKey, string.Empty);
            var testNamespace = SessionState.GetString(PendingTestNamespaceKey, string.Empty);
            var testClass = SessionState.GetString(PendingTestClassKey, string.Empty);
            var testMethod = SessionState.GetString(PendingTestMethodKey, string.Empty);
            var query = SessionState.GetString(PendingTestQueryKey, string.Empty);

            ClearPendingTestRun();

            // Normalize empty strings to null
            if (string.IsNullOrEmpty(testAssembly)) testAssembly = null;
            if (string.IsNullOrEmpty(testNamespace)) testNamespace = null;
            if (string.IsNullOrEmpty(testClass)) testClass = null;
            if (string.IsNullOrEmpty(testMethod)) testMethod = null;

            var filterParams = new TestFilterParameters(testAssembly, testNamespace, testClass, testMethod);

            if (!string.IsNullOrEmpty(query))
            {
                // The RQL selection is evaluated here, in the recompiled domain: the tests it names are the current ones.
                try
                {
                    if (!TryApplySelection(query, ref filterParams, out testMode, out var selectError))
                    {
                        FailDeferredTestRun(requestId, selectError!);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    FailDeferredTestRun(requestId, Error.TestExecutionFailed(ex.Message));
                    return;
                }
                _ = ValidateAndExecuteTestRun(requestId, testMode, filterParams);
                return;
            }

            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log($"[TestRunner] Resuming test run after recompilation. Mode: {testMode}, Filters: {filterParams}");

            // Started in this tick, before anything can reload the domain again; an empty run is answered as an error.
            ExecuteTestRun(CreateTestFilter(testMode, filterParams));
        }

        /// <summary>
        /// Validates the filters against Unity's test tree (never a silent subset or an empty run), then starts the run;
        /// a failure is delivered through <paramref name="requestId"/>. Used by the immediate path and a deferred RQL selection.
        /// </summary>
        static async Task ValidateAndExecuteTestRun(string requestId, TestMode testMode, TestFilterParameters filterParams)
        {
            try
            {
                // One filter for the check and the run: the check counts exactly what Unity will run.
                var filter = CreateTestFilter(testMode, filterParams);
                var validation = await ValidateTestFilters(TestRunnerApi, filter, filterParams);
                if (validation != null)
                {
                    FailDeferredTestRun(requestId, validation);
                    return;
                }

                ExecuteTestRun(filter);
            }
            catch (Exception ex)
            {
                FailDeferredTestRun(requestId, Error.TestExecutionFailed(ex.Message));
            }
        }

        /// <summary>Ends a deferred test run that cannot start and delivers <paramref name="message"/> to its request.</summary>
        static void FailDeferredTestRun(string requestId, string message)
        {
            ClearActiveTestRun(requestId);
            if (string.IsNullOrEmpty(requestId))
            {
                UnityMcpPluginEditor.Instance.LogError(
                    "A deferred test run could not start, but the pending MCP request id was missing. The error response cannot be delivered to the client: {message}",
                    typeof(Tool_Tests), message);
                return;
            }

            CompleteTestRun(requestId, ResponseCallValueTool<TestRunResponse>.Error(message));
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if any currently open scene has unsaved
        /// changes (<see cref="Scene.isDirty"/>). The exception message lists every dirty scene's
        /// name and path so the caller (LLM agent or human) can save them and retry.
        ///
        /// Running tests while a scene is dirty is unsafe: Unity may reload the scene when
        /// entering play mode, silently discarding the unsaved edits and producing a test run
        /// against a scene state that does not match either the in-memory scene or the asset
        /// on disk. This check aborts before any state is mutated (no test-run state, no
        /// AssetDatabase.Refresh, no domain reload is triggered).
        ///
        /// MUST be called on the Unity main thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if one or more open scenes have unsaved changes.
        /// </exception>
        internal static void ThrowIfAnyOpenSceneIsDirty()
        {
            var dirtyScenes = GetDirtyOpenScenes();
            if (dirtyScenes.Count == 0)
                return;

            throw new InvalidOperationException(FormatDirtyScenesMessage(dirtyScenes));
        }

        /// <summary>
        /// Returns every open scene whose <see cref="Scene.isDirty"/> is true.
        /// MUST be called on the Unity main thread.
        /// </summary>
        internal static List<Scene> GetDirtyOpenScenes()
        {
            var result = new List<Scene>(capacity: EditorSceneManager.sceneCount);
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    result.Add(scene);
            }
            return result;
        }

        internal static string FormatDirtyScenesMessage(IReadOnlyList<Scene> dirtyScenes)
        {
            var details = string.Join(", ", dirtyScenes.Select(FormatSceneForMessage));
            return $"Cannot run tests: {dirtyScenes.Count} open scene(s) have unsaved changes: {details}. " +
                "Save the scene(s) and try again.";
        }

        static string FormatSceneForMessage(Scene scene)
        {
            var name = string.IsNullOrEmpty(scene.name) ? "(untitled)" : scene.name;
            var path = string.IsNullOrEmpty(scene.path) ? "(unsaved)" : scene.path;
            return $"'{name}' ({path})";
        }

        private static class Error
        {
            public static string InvalidTestMode(string testMode)
                => $"Invalid test mode '{testMode}'. Valid modes: EditMode, PlayMode, All";

            public static string TestExecutionFailed(string reason)
                => $"Test execution failed: {reason}";

            public static string TestTimeout(int timeoutMs)
                => $"Test execution timed out after {timeoutMs} ms";

            public static string TestsAlreadyRunning(string activeRequestId)
            {
                var suffix = string.IsNullOrEmpty(activeRequestId)
                    ? string.Empty
                    : $" Active request id: {activeRequestId}.";

                return $"Cannot run tests: another test run is already in progress.{suffix} Wait for it to finish and try again.";
            }

            public const string EditorInPlayMode =
                "Cannot run tests: the Editor is in Play Mode (or a previous test run is still finishing). " +
                "Exit Play Mode or wait a few seconds and try again.";

            public static string NoTestsFound(TestFilterParameters filterParams)
            {
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(filterParams.TestAssembly)) filters.Add($"assembly '{filterParams.TestAssembly}'");
                if (!string.IsNullOrEmpty(filterParams.TestNamespace)) filters.Add($"namespace '{filterParams.TestNamespace}'");
                if (!string.IsNullOrEmpty(filterParams.TestClass)) filters.Add($"class '{filterParams.TestClass}'");
                if (!string.IsNullOrEmpty(filterParams.TestMethod)) filters.Add($"method '{filterParams.TestMethod}'");
                if (filterParams.HasGroups) filters.Add("the 'query' selection");

                var filterText = filters.Count > 0
                    ? $" matching {string.Join(", ", filters)}"
                    : string.Empty;

                return $"No tests found{filterText}. Please check that the specified assembly, namespace, class, and method names are correct and that your Unity project contains tests.";
            }
        }
    }
}
