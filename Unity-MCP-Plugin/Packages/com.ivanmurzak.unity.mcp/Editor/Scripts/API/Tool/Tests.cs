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
    [McpPluginToolType]
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

        static void SavePendingTestRun(TestMode testMode, string? testAssembly, string? testNamespace, string? testClass, string? testMethod)
        {
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
                TestResultCollector.TestCallRequestID.Value = string.Empty;

                var errorDetails = ScriptUtils.GetCompilationErrorDetails();
                var response = ResponseCallValueTool<TestRunResponse>
                    .Error($"Cannot run tests: compilation errors after script recompilation.\n\n{errorDetails}")
                    .SetRequestID(requestId);

                _ = UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
                {
                    RequestId = requestId,
                    Result = response
                });
                return;
            }

            // Compilation succeeded — load filter params from SessionState and run tests
            var testMode = (TestMode)SessionState.GetInt(PendingTestModeKey, (int)TestMode.EditMode);
            var testAssembly = SessionState.GetString(PendingTestAssemblyKey, string.Empty);
            var testNamespace = SessionState.GetString(PendingTestNamespaceKey, string.Empty);
            var testClass = SessionState.GetString(PendingTestClassKey, string.Empty);
            var testMethod = SessionState.GetString(PendingTestMethodKey, string.Empty);

            ClearPendingTestRun();

            // Normalize empty strings to null
            if (string.IsNullOrEmpty(testAssembly)) testAssembly = null;
            if (string.IsNullOrEmpty(testNamespace)) testNamespace = null;
            if (string.IsNullOrEmpty(testClass)) testClass = null;
            if (string.IsNullOrEmpty(testMethod)) testMethod = null;

            var filterParams = new TestFilterParameters(testAssembly, testNamespace, testClass, testMethod);

            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log($"[TestRunner] Resuming test run after recompilation. Mode: {testMode}, Filters: {filterParams}");

            var filter = CreateTestFilter(testMode, filterParams);
            TestRunnerApi.Execute(new ExecutionSettings(filter));
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if any currently open scene has unsaved
        /// changes (<see cref="Scene.isDirty"/>). The exception message lists every dirty scene's
        /// name and path so the caller (LLM agent or human) can save them and retry.
        ///
        /// Running tests while a scene is dirty is unsafe: Unity may reload the scene when
        /// entering play mode, silently discarding the unsaved edits and producing a test run
        /// against a scene state that does not match either the in-memory scene or the asset
        /// on disk. This check aborts before any state is mutated (no PlayerPrefs, no
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

            public static string NoTestsFound(TestFilterParameters filterParams)
            {
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(filterParams.TestAssembly)) filters.Add($"assembly '{filterParams.TestAssembly}'");
                if (!string.IsNullOrEmpty(filterParams.TestNamespace)) filters.Add($"namespace '{filterParams.TestNamespace}'");
                if (!string.IsNullOrEmpty(filterParams.TestClass)) filters.Add($"class '{filterParams.TestClass}'");
                if (!string.IsNullOrEmpty(filterParams.TestMethod)) filters.Add($"method '{filterParams.TestMethod}'");

                var filterText = filters.Count > 0
                    ? $" matching {string.Join(", ", filters)}"
                    : string.Empty;

                return $"No tests found{filterText}. Please check that the specified assembly, namespace, class, and method names are correct and that your Unity project contains tests.";
            }
        }
    }
}
