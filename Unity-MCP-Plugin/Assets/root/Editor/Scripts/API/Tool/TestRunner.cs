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
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    [InitializeOnLoad]
    public static partial class Tool_TestRunner
    {
        static readonly object _lock = new();
        static volatile TestRunnerApi? _testRunnerApi = null!;
        static volatile TestResultCollector? _resultCollector = null!;
        static volatile bool _callbacksRegistered = false;

        static Tool_TestRunner()
        {
            _testRunnerApi ??= CreateInstance();
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

        private static class Error
        {
            public static string InvalidTestMode(string testMode)
                => $"[Error] Invalid test mode '{testMode}'. Valid modes: EditMode, PlayMode, All";

            public static string TestExecutionFailed(string reason)
                => $"[Error] Test execution failed: {reason}";

            public static string TestTimeout(int timeoutMs)
                => $"[Error] Test execution timed out after {timeoutMs} ms";

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

                return $"[Error] No tests found{filterText}. Please check that the specified assembly, namespace, class, and method names are correct and that your Unity project contains tests.";
            }
        }
    }
}
