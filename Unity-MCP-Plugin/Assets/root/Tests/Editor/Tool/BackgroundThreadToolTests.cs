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
using System.Collections;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Regression coverage for <see href="https://github.com/IvanMurzak/Unity-MCP/issues/633">#633</see>.
    ///
    /// Every MCP tool must be safe to invoke from any thread — the MCP server
    /// dispatches tool calls from a SignalR background thread, so tools that touch
    /// Unity APIs must marshal back to the main thread via <c>MainThread.Instance.Run</c>.
    ///
    /// These tests exercise a representative cross-section of tools via
    /// <see cref="BaseTest.RunToolFromBackgroundThread"/>, which schedules the actual
    /// <c>RunCallTool</c> invocation on the thread pool. The test coroutine then pumps
    /// Unity's main loop (via <c>yield return null</c> inside <c>WaitForTask</c>) so that
    /// any main-thread dispatches posted by the tool can run to completion.
    ///
    /// A tool that forgets to wrap its Unity API usage in <c>MainThread.Instance.Run</c>
    /// will throw <c>UnityException: get_* can only be called from the main thread</c>
    /// and fail these tests.
    /// </summary>
    public class BackgroundThreadToolTests : BaseTest
    {
        const string TestGameObjectName = "BgThreadTest_GameObject";

        [SetUp]
        public void BgSetUp()
        {
            // The MCP plugin may be asynchronously attempting to (re)connect to the
            // MCP server during these tests. In a test environment the server is not
            // running, so Unity surfaces a SocketException on the log bus that NUnit's
            // LogAssert flags as a failure even though it is unrelated to the tool call.
            // We are explicitly not asserting against console output here — we only
            // care that the tool call itself succeeds — so ignore unexpected log noise.
            LogAssert.ignoreFailingMessages = true;
        }

        // ---------- GameObject tools ----------

        [UnityTest]
        public IEnumerator GameObjectCreate_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_GameObject.GameObjectCreateToolId,
                $@"{{ ""name"": ""{TestGameObjectName}"" }}");

            var go = GameObject.Find(TestGameObjectName);
            Assert.IsNotNull(go, "GameObject should have been created on the main thread even though the tool call originated on a background thread.");
        }

        [UnityTest]
        public IEnumerator GameObjectFind_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject(TestGameObjectName);

            yield return RunToolFromBackgroundThread(
                "gameobject-find",
                $@"{{ ""gameObjectRef"": {{ ""instanceID"": {go.GetInstanceID()} }} }}");
        }

        [UnityTest]
        public IEnumerator GameObjectDestroy_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject(TestGameObjectName);
            var id = go.GetInstanceID();

            yield return RunToolFromBackgroundThread(
                "gameobject-destroy",
                $@"{{ ""gameObjectRef"": {{ ""instanceID"": {id} }} }}");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentListAll_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_GameObject.ComponentListToolId,
                @"{ ""search"": ""Transform"", ""page"": 0, ""pageSize"": 5 }");
        }

        [UnityTest]
        public IEnumerator GameObjectComponentAdd_FromBackgroundThread_Succeeds()
        {
            var go = new GameObject(TestGameObjectName);
            var id = go.GetInstanceID();

            yield return RunToolFromBackgroundThread(
                "gameobject-component-add",
                $@"{{
                    ""gameObjectRef"": {{ ""instanceID"": {id} }},
                    ""componentNames"": [ ""UnityEngine.BoxCollider"" ]
                }}");

            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "BoxCollider should be added by the background-thread tool call.");
        }

        // ---------- Console tools ----------

        [UnityTest]
        public IEnumerator ConsoleGetLogs_FromBackgroundThread_Succeeds()
        {
            Debug.Log("BgThreadTest log line");
            // Allow log collector to flush.
            yield return null;
            yield return null;

            yield return RunToolFromBackgroundThread(
                Tool_Console.ConsoleGetLogsToolId,
                @"{ ""maxEntries"": 50 }");
        }

        [UnityTest]
        public IEnumerator ConsoleClearLogs_FromBackgroundThread_Succeeds()
        {
            // Regression for #632/#637: ClearLogs calls Debug.ClearDeveloperConsole()
            // which MUST run on the main thread. This is the canonical background-thread
            // failure mode that motivated this issue.
            yield return RunToolFromBackgroundThread(
                Tool_Console.ConsoleClearLogsToolId,
                @"{}");
        }

        // ---------- Editor tools ----------

        [UnityTest]
        public IEnumerator EditorApplicationGetState_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Editor.EditorApplicationGetStateToolId,
                @"{}");
        }

        [UnityTest]
        public IEnumerator EditorSelectionGet_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Editor_Selection.EditorSelectionGetToolId,
                @"{}");
        }

        // ---------- Scene tools ----------

        [UnityTest]
        public IEnumerator SceneListOpened_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Scene.SceneListOpenedToolId,
                @"{}");
        }

        // ---------- Assets tools ----------

        [UnityTest]
        public IEnumerator AssetsFind_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Assets.AssetsFindToolId,
                @"{ ""filter"": ""t:Scene"", ""maxResults"": 5 }");
        }

        [UnityTest]
        public IEnumerator AssetsShaderListAll_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Assets_Shader.AssetsShaderListAllToolId,
                @"{}");
        }

        // ---------- Package tools ----------

        [UnityTest]
        public IEnumerator PackageList_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Package.PackageListToolId,
                @"{}");
        }

        // ---------- Tool listing ----------

        [UnityTest]
        public IEnumerator ToolList_FromBackgroundThread_Succeeds()
        {
            yield return RunToolFromBackgroundThread(
                Tool_Tool.ToolListId,
                @"{}");
        }
    }
}
