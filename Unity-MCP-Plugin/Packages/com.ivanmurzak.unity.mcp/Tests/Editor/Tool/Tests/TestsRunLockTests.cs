/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The tests-run lock is held until Unity's run is over, not just until RunFinished, and a run that fails with an
    /// error frees it too (#1049).
    /// </summary>
    public class TestsRunLockTests
    {
        // The lock and scene setup of the run executing these tests, when it was started by tests-run: restored after each
        // test, or that run would finish without a request to answer.
        string _runRequestId = string.Empty;
        string _runStartedUtcTicks = string.Empty;
        string _runSceneSetup = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _runRequestId = TestResultCollector.TestCallRequestID.Value;
            _runStartedUtcTicks = SessionState.GetString(Tool_Tests.ActiveTestRunStartedUtcTicksKey, string.Empty);
            _runSceneSetup = SessionState.GetString(Tool_Tests.RunSceneSetupKey, string.Empty);
            Tool_Tests.ClearActiveTestRun();
        }

        [TearDown]
        public void TearDown()
        {
            Tool_Tests.ClearActiveTestRun();
            TestResultCollector.TestCallRequestID.Value = _runRequestId;
            RestoreSessionString(Tool_Tests.ActiveTestRunStartedUtcTicksKey, _runStartedUtcTicks);
            RestoreSessionString(Tool_Tests.RunSceneSetupKey, _runSceneSetup);
        }

        static void RestoreSessionString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                SessionState.EraseString(key);
            else
                SessionState.SetString(key, value);
        }

        [Test]
        public void TestResultCollector_ImplementsErrorCallbacks()
        {
            // Unity reports a failed run only through IErrorCallbacks.OnError, never through RunFinished.
            Assert.IsTrue(typeof(IErrorCallbacks).IsAssignableFrom(typeof(TestResultCollector)));
        }

        [TestCase(true, "Assets/Main.unity", ExpectedResult = false, TestName = "IsTestRunTeardownComplete_WhileInPlayMode_IsFalse")]
        [TestCase(false, "Assets/InitTestScene0.unity", ExpectedResult = false, TestName = "IsTestRunTeardownComplete_WhileTheRunSceneIsOpen_IsFalse")]
        [TestCase(false, "Assets/Main.unity", ExpectedResult = true, TestName = "IsTestRunTeardownComplete_WhenScenesAreRestoredOutOfPlayMode_IsTrue")]
        public bool IsTestRunTeardownComplete(bool isPlaying, string openScenePaths)
            => Tool_Tests.IsTestRunTeardownComplete(isPlaying, () => openScenePaths, "Assets/Main.unity");

        [UnityTest]
        public IEnumerator CompleteTestRunAfterTeardown_WhenScenesAreRestored_FreesTheLock()
        {
            Assert.IsTrue(Tool_Tests.TryBeginTestRun("request-restored", out _));
            SessionState.SetString(Tool_Tests.RunSceneSetupKey, Tool_Tests.OpenScenePaths());

            Tool_Tests.CompleteTestRunAfterTeardown("request-restored", ResponseCallTool.Success("done"));

            for (var i = 0; i < 3; i++)
                yield return null;

            Assert.IsFalse(Tool_Tests.IsTestRunActive(out _));
        }

        [UnityTest]
        public IEnumerator CompleteTestRunAfterTeardown_UntilScenesAreRestored_KeepsTheLock()
        {
            Assert.IsTrue(Tool_Tests.TryBeginTestRun("request-tearing-down", out _));
            SessionState.SetString(Tool_Tests.RunSceneSetupKey, "Assets/NotOpen.unity");

            var timeout = TimeSpan.FromSeconds(5);
            var started = DateTime.UtcNow;
            Tool_Tests.CompleteTestRunAfterTeardown("request-tearing-down", ResponseCallTool.Success("done"), timeout);

            for (var i = 0; i < 3; i++)
                yield return null;

            // CI runners can take seconds for a few frames: the lock is checked only while the wait has not timed out.
            var isActive = Tool_Tests.IsTestRunActive(out var activeRequestId);
            if (DateTime.UtcNow - started < timeout)
            {
                Assert.IsTrue(isActive);
                Assert.AreEqual("request-tearing-down", activeRequestId);
            }

            // Past the timeout the request is answered and the lock freed anyway.
            // The warning is logged only at the Warning level or below (the plugin's configured log level).
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Warning))
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("did not finish its teardown"));
            var deadline = started + timeout + TimeSpan.FromSeconds(10);
            while (Tool_Tests.IsTestRunActive(out _) && DateTime.UtcNow < deadline)
                yield return null;

            Assert.IsFalse(Tool_Tests.IsTestRunActive(out _));
        }

        // Another run holds the Test Runner: the Editor cannot take the call now (503), not the agent's error; nothing is
        // started, and it is not marked not-executed (the server must not keep re-sending it while the other run lasts).
        [UnityTest]
        public IEnumerator Run_WhileAnotherRunIsActive_IsUnavailable()
        {
            Assert.IsTrue(Tool_Tests.TryBeginTestRun("request-other", out _));

            var task = Tool_Tests.Run(requestId: "request-busy");
            while (!task.IsCompleted)
                yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Error, response.Status);
            Assert.AreEqual(ResponseErrorKind.Unavailable, response.ErrorKind);
            Assert.IsNull(response.HttpStatusCode);
            Assert.AreEqual("request-busy", response.RequestID);
            StringAssert.Contains("another test run is already in progress", response.GetMessage());
            Assert.IsTrue(Tool_Tests.IsTestRunActive(out var activeRequestId));
            Assert.AreEqual("request-other", activeRequestId);
        }

        [Test]
        public void RunErrorMessage_ListsTheErrorsLoggedDuringTheRun()
        {
            var logs = new List<TestLogEntry>
            {
                new TestLogEntry(LogType.Log, "a plain log"),
                new TestLogEntry(LogType.Exception, "InvalidOperationException: prebuild setup failure"),
                new TestLogEntry(LogType.Error, "Multiple unexpected errors happened while running tests."),
            };

            var text = TestResultCollector.RunErrorMessage("Multiple unexpected errors happened while running tests.", logs);

            StringAssert.StartsWith("Test run failed: Multiple unexpected errors happened while running tests.", text);
            StringAssert.Contains("InvalidOperationException: prebuild setup failure", text);
            StringAssert.DoesNotContain("a plain log", text);
            // Unity logs the message itself as an error too: it is not repeated.
            Assert.AreEqual(text.IndexOf("Multiple unexpected", StringComparison.Ordinal),
                text.LastIndexOf("Multiple unexpected", StringComparison.Ordinal));
        }

        [Test]
        public void RunErrorMessage_KeepsTheLastTenErrors()
        {
            var logs = new List<TestLogEntry>();
            for (var i = 0; i < 15; i++)
                logs.Add(new TestLogEntry(LogType.Error, $"error #{i:00}"));

            var text = TestResultCollector.RunErrorMessage("failed", logs);

            StringAssert.DoesNotContain("error #04", text);
            StringAssert.Contains("error #05", text);
            StringAssert.Contains("error #14", text);
        }
    }
}
