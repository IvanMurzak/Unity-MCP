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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
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
            // if (UnityMcpPlugin.IsLogActive(MCP.Utils.LogLevel.Trace))
            //     Debug.Log($"[{nameof(TestRunnerApi)}] Ctor.");

            _resultCollector ??= new TestResultCollector();
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(_resultCollector);
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
