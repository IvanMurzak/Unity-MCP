/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.FilterMatchData
{
    // Fixtures the filter tests count in Unity's real test tree (they also run, and pass, as ordinary tests).
    public class FilterParamFixture
    {
        [TestCase(1)] [TestCase(2)] public void Method(int x) { }
        [Test] public void Plain() { }
    }
    public class FilterOtherFixture { [Test] public void Plain() { } }
}

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.FilterMatchData.FilterWidget
{
    public class Thing { [Test] public void T() { } }
}

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The tests-run filter check counts what Unity runs: each expectation is what com.unity.test-framework executes for
    /// that filter (checked against real runs on 1.1.33, 1.6.0 and Unity 6000.6).
    /// </summary>
    public class TestFilterMatchTests
    {
        const string Ns = "com.IvanMurzak.Unity.MCP.Editor.Tests.FilterMatchData";
        const string Asm = "com.IvanMurzak.Unity.MCP.Editor.Tests";
        const string Param = Ns + ".FilterParamFixture.Method";
        const string Other = Ns + ".FilterOtherFixture.Plain";

        static int Count(ITestAdaptor root, string? asm = null, string? ns = null, string? cls = null, string? method = null, string[]? query = null)
        {
            var filterParams = new TestFilterParameters(asm, ns, cls, method)
            {
                Groups = query == null ? null : new[] { Tool_Tests.CreateTestsRegexPattern(query) }
            };
            return TestFilterMatch.Count(root, Tool_Tests.CreateTestFilter(TestMode.EditMode, filterParams));
        }

        [UnityEngine.TestTools.UnityTest]
        public IEnumerator CountsWhatUnityRuns() => UnityTestTree.EditMode(root =>
        {
            Assert.AreEqual(5, Count(root, ns: Ns), "namespace");
            Assert.AreEqual(5, Count(root, Asm, Ns), "assembly + namespace");
            Assert.AreEqual(1, Count(root, Asm.ToUpperInvariant(), cls: "FilterOtherFixture"), "the assembly name ignores case");
            Assert.AreEqual(0, Count(root, Asm, cls: "Missing"), "fields are ANDed");
            Assert.AreEqual(0, Count(root, cls: "FilterParamFixture", method: Other), "fields are ANDed");
            Assert.AreEqual(5, Count(root, ns: Ns, cls: "Missing"), "namespace and class share groupNames: ORed");
            Assert.AreEqual(2, Count(root, method: Param), "a parameterized method selects its cases");
            Assert.AreEqual(2, Count(root, ns: Ns, method: Param), "a parameterized method selects its cases");
            Assert.AreEqual(0, Count(root, method: Param.ToLowerInvariant()), "test names are case-sensitive");
            Assert.AreEqual(1, Count(root, cls: "FilterWidget"), "the class pattern also matches a namespace segment");
            Assert.AreEqual(4, Count(root, ns: Ns, method: "!" + Other), "'!' excludes");
            Assert.AreEqual(3, Count(root, query: new[] { Param, Other }), "RQL selection");
            Assert.AreEqual(Count(root, Asm), Count(root, Asm, Asm), "a namespace pattern also matches the assembly's file name");
        });

        [Test]
        public void EmptyRun_IsAnError()
        {
            Assert.IsNotNull(TestResultCollector.EmptyRunError(0, TestStatus.Passed));
            Assert.IsNotNull(TestResultCollector.EmptyRunError(0, TestStatus.Skipped));
            Assert.IsNull(TestResultCollector.EmptyRunError(0, TestStatus.Failed), "a failed run keeps its own answer");
            Assert.IsNull(TestResultCollector.EmptyRunError(1, TestStatus.Passed));
        }
    }
}
