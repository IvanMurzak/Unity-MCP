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
        // The scene setup of the run executing these tests, when it was started by tests-run.
        string _runSceneSetup = string.Empty;

        [SetUp]
        public void SetUp()
        {
            Tool_Tests.ClearActiveTestRun();
            _runSceneSetup = SessionState.GetString(Tool_Tests.RunSceneSetupKey, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            Tool_Tests.ClearActiveTestRun();
            if (string.IsNullOrEmpty(_runSceneSetup))
                SessionState.EraseString(Tool_Tests.RunSceneSetupKey);
            else
                SessionState.SetString(Tool_Tests.RunSceneSetupKey, _runSceneSetup);
        }

        [Test]
        public void TestResultCollector_ImplementsErrorCallbacks()
        {
            // Unity reports a failed run only through IErrorCallbacks.OnError, never through RunFinished.
            Assert.IsTrue(typeof(IErrorCallbacks).IsAssignableFrom(typeof(TestResultCollector)));
        }

        [Test]
        public void IsTestRunTeardownComplete_WhileInPlayMode_IsFalse()
        {
            Assert.IsFalse(Tool_Tests.IsTestRunTeardownComplete(true, "Assets/Main.unity", "Assets/Main.unity"));
        }

        [Test]
        public void IsTestRunTeardownComplete_WhileTheRunSceneIsOpen_IsFalse()
        {
            Assert.IsFalse(Tool_Tests.IsTestRunTeardownComplete(false, "Assets/InitTestScene0.unity", "Assets/Main.unity"));
        }

        [Test]
        public void IsTestRunTeardownComplete_WhenScenesAreRestoredOutOfPlayMode_IsTrue()
        {
            Assert.IsTrue(Tool_Tests.IsTestRunTeardownComplete(false, "Assets/Main.unity", "Assets/Main.unity"));
        }

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

            Tool_Tests.CompleteTestRunAfterTeardown("request-tearing-down", ResponseCallTool.Success("done"), TimeSpan.FromSeconds(1));

            for (var i = 0; i < 3; i++)
                yield return null;

            Assert.IsTrue(Tool_Tests.IsTestRunActive(out var activeRequestId));
            Assert.AreEqual("request-tearing-down", activeRequestId);

            // Past the timeout the request is answered and the lock freed anyway.
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("did not finish its teardown"));
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (Tool_Tests.IsTestRunActive(out _) && DateTime.UtcNow < deadline)
                yield return null;

            Assert.IsFalse(Tool_Tests.IsTestRunActive(out _));
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
