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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
// LogAssert lives in UnityEngine.TestTools.

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class BaseTest
    {
        protected Microsoft.Extensions.Logging.ILogger _logger = null!;

        [UnitySetUp]
        public virtual IEnumerator SetUp()
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] SetUp");

            UnityMcpPluginEditor.InitSingletonIfNeeded();

            _logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tests");

            yield return null;
        }
        [UnityTearDown]
        public virtual IEnumerator TearDown()
        {
            Debug.Log($"[{GetType().GetTypeShortName()}] TearDown");

            DestroyAllGameObjectsInActiveScene();

            yield return null;
        }

        protected static void DestroyAllGameObjectsInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                UnityEngine.Object.DestroyImmediate(go);
        }

        private (ResponseData<ResponseCallTool> result, string json) CallToolInternal(string toolName, string json)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector;

            Debug.Log($"{toolName} Started with JSON:\n{json}");

            var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var request = new RequestCallTool(toolName, parameters!);
            var task = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(request);
            var result = task.Result;

            Debug.Log($"{toolName} Completed");

            var jsonResult = result.ToJson(reflector)!;
            Debug.Log($"{toolName} Result:\n{jsonResult}");

            return (result, jsonResult);
        }

        /// <summary>
        /// Starts a tool call from a background thread via <see cref="Task.Run(Action)"/>.
        /// Returns a Task that resolves to the raw tool response + rendered JSON.
        /// The caller must pump Unity's main-thread loop (e.g. by yielding <c>null</c> in a
        /// <c>[UnityTest]</c> coroutine) until the task completes so that any
        /// <see cref="MainThread.Instance.Run"/> dispatches posted via
        /// <c>EditorApplication.update</c> can actually execute.
        /// </summary>
        private Task<(ResponseData<ResponseCallTool> result, string json)> CallToolFromBackgroundThreadInternal(string toolName, string json)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector;

            Debug.Log($"{toolName} Started (background thread) with JSON:\n{json}");

            return Task.Run(async () =>
            {
                Assert.IsFalse(MainThread.Instance.IsMainThread,
                    "Task.Run should schedule work onto the thread pool, not the Unity main thread.");

                var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                var request = new RequestCallTool(toolName, parameters!);
                var result = await UnityMcpPluginEditor.Instance.Tools!.RunCallTool(request)
                    .ConfigureAwait(false);

                var jsonResult = result.ToJson(reflector)!;
                return (result, jsonResult);
            });
        }

        /// <summary>
        /// Yields the coroutine until the provided task is complete, allowing Unity's
        /// main thread to keep ticking (so background-thread tool calls that dispatch
        /// work back to the main thread can make progress).
        /// </summary>
        protected static IEnumerator WaitForTask(Task task, float timeoutSeconds = 30f)
        {
            var startTime = Time.realtimeSinceStartup;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup - startTime > timeoutSeconds)
                    throw new TimeoutException($"Task did not complete within {timeoutSeconds} seconds.");
                yield return null;
            }
            if (task.IsFaulted)
                throw task.Exception ?? new Exception("Task faulted without exception.");
        }

        /// <summary>
        /// Calls a tool from a background thread and asserts the result is a success.
        /// Must be used inside a <c>[UnityTest]</c> coroutine via <c>yield return</c>
        /// so Unity's main-thread loop keeps running.
        /// </summary>
        protected IEnumerator RunToolFromBackgroundThread(string toolName, string json, Action<ResponseData<ResponseCallTool>>? onResult = null)
        {
            var task = CallToolFromBackgroundThreadInternal(toolName, json);
            yield return WaitForTask(task);

            var (result, jsonResult) = task.Result;

            Debug.Log($"{toolName} Completed (background thread)");
            Debug.Log($"{toolName} Result:\n{jsonResult}");

            Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed with error status: {result.Message}");
            Assert.IsNotNull(result.Message, $"Tool call returned null message");
            Assert.IsFalse(result.Message!.Contains("[Error]"), $"Tool call failed with error: {result.Message}");
            Assert.IsNotNull(result.Value, $"Tool call returned null value");
            Assert.IsFalse(result.Value!.Status == ResponseStatus.Error, $"Tool call failed");
            Assert.IsFalse(jsonResult.Contains("[Error]"), $"Tool call failed with error in JSON: {jsonResult}");
            Assert.IsFalse(jsonResult.Contains("[Warning]"), $"Tool call contains warnings in JSON: {jsonResult}");

            onResult?.Invoke(result);
        }

        /// <summary>
        /// Calls a tool from a background thread and returns the raw JSON result string
        /// without asserting success. Must be used inside a <c>[UnityTest]</c> coroutine.
        /// </summary>
        protected IEnumerator RunToolRawFromBackgroundThread(string toolName, string json, Action<string> onJson)
        {
            var task = CallToolFromBackgroundThreadInternal(toolName, json);
            yield return WaitForTask(task);

            var (_, jsonResult) = task.Result;
            Debug.Log($"{toolName} Result (background thread):\n{jsonResult}");
            onJson(jsonResult);
        }

        /// <summary>
        /// Cooperative main-thread runner: invokes <c>RunCallTool</c> on the current
        /// (main) thread but polls for completion via coroutine yield so Unity can keep
        /// ticking <c>EditorApplication.update</c>. This is essential for tools that
        /// internally <c>await Task.Yield()</c> on the main-thread sync context
        /// (e.g. <c>package-list</c>, <c>package-search</c>, <c>scene-unload</c>) —
        /// a blocking <c>task.Result</c> on the main thread would deadlock because the
        /// awaited continuation is scheduled back onto the blocked thread.
        /// </summary>
        private IEnumerator CallToolOnMainThreadCoop(string toolName, string json, Action<ResponseData<ResponseCallTool>, string> onComplete)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector;

            Debug.Log($"{toolName} Started (main thread coop) with JSON:\n{json}");

            var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var request = new RequestCallTool(toolName, parameters!);
            var task = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(request);

            yield return WaitForTask(task);

            var result = task.Result;
            var jsonResult = result.ToJson(reflector)!;
            Debug.Log($"{toolName} Completed (main thread coop). Result:\n{jsonResult}");
            onComplete(result, jsonResult);
        }

        /// <summary>
        /// Cooperative main-thread invocation that asserts the tool succeeded.
        /// See <see cref="CallToolOnMainThreadCoop"/> for why this is coroutine-based.
        /// </summary>
        protected IEnumerator RunToolMainThreadCoop(string toolName, string json)
        {
            yield return CallToolOnMainThreadCoop(toolName, json, (result, jsonResult) =>
            {
                Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed with error status: {result.Message}");
                Assert.IsNotNull(result.Message, $"Tool call returned null message");
                Assert.IsFalse(result.Message!.Contains("[Error]"), $"Tool call failed with error: {result.Message}");
                Assert.IsNotNull(result.Value, $"Tool call returned null value");
                Assert.IsFalse(result.Value!.Status == ResponseStatus.Error, $"Tool call failed");
                Assert.IsFalse(jsonResult.Contains("[Error]"), $"Tool call failed with error in JSON: {jsonResult}");
                Assert.IsFalse(jsonResult.Contains("[Warning]"), $"Tool call contains warnings in JSON: {jsonResult}");
            });
        }

        /// <summary>
        /// Cooperative main-thread invocation that does NOT assert success — returns the
        /// raw JSON via the callback. Useful for thread-safety smoke tests where we only
        /// care about the absence of a main-thread violation.
        /// </summary>
        protected IEnumerator RunToolRawMainThreadCoop(string toolName, string json, Action<string> onJson)
        {
            yield return CallToolOnMainThreadCoop(toolName, json, (_, jsonResult) => onJson(jsonResult));
        }

        /// <summary>
        /// Thread-safety smoke test: invokes the tool from both the main thread
        /// (cooperatively) and a background thread and only asserts the absence of a
        /// Unity main-thread violation. Business-level errors (invalid input, unsupported
        /// state, etc.) are tolerated — this helper is for tools where crafting a full
        /// happy-path input is impractical, but we still want to prove the dispatcher /
        /// body does not throw
        /// <c>UnityException: ... can only be called from the main thread</c>.
        /// </summary>
        protected IEnumerator RunToolExpectNoThreadViolation(string toolName, string json)
        {
            const string MainThreadViolationSnippet = "can only be called from the main thread";

            // Force-ignore failing log messages for this helper: we tolerate any
            // business-level errors (missing camera, invalid ref, etc.) that the tool
            // might surface through Debug.LogError on either thread.
            LogAssert.ignoreFailingMessages = true;

            string? mainJson = null;
            yield return RunToolRawMainThreadCoop(toolName, json, j => mainJson = j);
            Assert.IsNotNull(mainJson, $"[{toolName}] main-thread call produced no JSON.");
            Assert.IsFalse(mainJson!.Contains(MainThreadViolationSnippet),
                $"[{toolName}] main-thread call surfaced a thread-safety error:\n{mainJson}");

            string? bgJson = null;
            yield return RunToolRawFromBackgroundThread(toolName, json, j => bgJson = j);
            Assert.IsNotNull(bgJson, $"[{toolName}] background-thread call produced no JSON.");
            Assert.IsFalse(bgJson!.Contains(MainThreadViolationSnippet),
                $"[{toolName}] background-thread call surfaced a thread-safety error:\n{bgJson}");
        }

        /// <summary>
        /// Happy-path thread coverage: asserts that the tool succeeds on both the main
        /// thread (via cooperative coroutine) and a background thread. The main-thread
        /// call uses a coroutine-based runner so async tools that <c>await Task.Yield()</c>
        /// on the main-thread sync context can complete without deadlocking.
        /// <paramref name="mainJson"/> is used for the main-thread call and
        /// <paramref name="backgroundJson"/> (defaulting to <paramref name="mainJson"/>)
        /// for the background-thread call, so tests can target different entities when
        /// the main-thread call mutated state.
        /// </summary>
        protected IEnumerator RunToolBothThreads(string toolName, string mainJson, string? backgroundJson = null)
        {
            yield return RunToolMainThreadCoop(toolName, mainJson);
            yield return RunToolFromBackgroundThread(toolName, backgroundJson ?? mainJson);
        }

        /// <summary>
        /// Calls a tool and returns the raw JSON result string without asserting success.
        /// Useful for testing error responses.
        /// </summary>
        protected virtual string RunToolRaw(string toolName, string json)
        {
            return CallToolInternal(toolName, json).json;
        }

        protected virtual ResponseData<ResponseCallTool> RunTool(string toolName, string json)
        {
            var (result, jsonResult) = CallToolInternal(toolName, json);

            Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed with error status: {result.Message}");
            Assert.IsNotNull(result.Message, $"Tool call returned null message");
            Assert.IsFalse(result.Message!.Contains("[Error]"), $"Tool call failed with error: {result.Message}");
            Assert.IsNotNull(result.Value, $"Tool call returned null value");
            Assert.IsFalse(result.Value!.Status == ResponseStatus.Error, $"Tool call failed");
            Assert.IsFalse(jsonResult.Contains("[Error]"), $"Tool call failed with error in JSON: {jsonResult}");
            Assert.IsFalse(jsonResult.Contains("[Warning]"), $"Tool call contains warnings in JSON: {jsonResult}");

            return result;
        }
    }
}
