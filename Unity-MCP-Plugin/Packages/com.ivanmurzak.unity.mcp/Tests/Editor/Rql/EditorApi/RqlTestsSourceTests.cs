/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityTest = UnityEngine.TestTools.UnityTestAttribute;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    // Probe fixtures the tests source has to describe: a category, a parameterized method, a test inherited from an
    // abstract base, an explicit test. They pass trivially when the suite runs them.
    [Category("RqlProbe")]
    public class RqlTestsProbe
    {
        [Test] public void Plain() { }
        [TestCase(1)] [TestCase(2)] public void Cases(int n) => Assert.Greater(n, 0);
        [Test, Explicit("probe")] public void OnlyByName() { }
    }

    public abstract class RqlTestsProbeBase
    {
        [Test] public void Inherited() { }
    }

    public class RqlTestsProbeConcrete : RqlTestsProbeBase { }

    /// <summary>The <c>tests</c> / <c>testResults</c> sources and tests-run's RQL selection and result projection.</summary>
    public class RqlTestsSourceTests : AdapterTestBase
    {
        const string Probe = "com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi.RqlTestsProbe";

        [Test]
        public void TestsAreListedWithTheirFixtureModeAndCategories()
        {
            Assert.AreEqual(3L, Count("tests | where fixture == \"RqlTestsProbe\" | count"));
            Assert.AreEqual("EditMode", Cell(Q($"tests | where fullName == \"{Probe}.Plain\" | select mode")));
            Assert.AreEqual("com.IvanMurzak.Unity.MCP.Editor.Tests", Cell(Q($"tests | where fullName == \"{Probe}.Plain\" | select assembly")));
            Assert.AreEqual(3L, Count("tests | where categories contains \"RqlProbe\" | count"));
        }

        [Test]
        public void ParameterizedExplicitAndInheritedTestsAreDescribed()
        {
            Assert.AreEqual("true", Cell(Q($"tests | where fullName == \"{Probe}.Cases\" | select parameterized")));
            Assert.AreEqual("false", Cell(Q($"tests | where fullName == \"{Probe}.Plain\" | select parameterized")));
            Assert.AreEqual("true", Cell(Q($"tests | where fullName == \"{Probe}.OnlyByName\" | select explicit")));
            // The abstract base is not a fixture; its test runs in (and is named after) the concrete class.
            Assert.AreEqual(1L, Count("tests | where name == \"Inherited\" | count"));
            Assert.AreEqual("RqlTestsProbeConcrete", Cell(Q("tests | where name == \"Inherited\" | select fixture")));
        }

        [Test]
        public void PlayModeTestsAreTaggedPlayMode()
        {
            Assert.AreEqual("PlayMode", Cell(Q("tests | where fixture == \"DemoTest\" and namespace == \"com.IvanMurzak.Unity.MCP.Runtime.Tests\" | select mode")));
        }

        [Test]
        public void GroupPatternsSelectExactlyTheTestAndItsCases()
        {
            var pattern = Tool_Tests.CreateTestsRegexPattern(new[] { Probe + ".Cases", Probe + ".Plain" });
            Assert.IsTrue(Regex.IsMatch(Probe + ".Cases(1)", pattern));
            Assert.IsTrue(Regex.IsMatch(Probe + ".Cases", pattern));
            Assert.IsFalse(Regex.IsMatch(Probe + ".CasesMore", pattern));
            Assert.IsFalse(Regex.IsMatch("Other." + Probe + ".Cases", pattern));
            Assert.IsTrue(Regex.IsMatch(Probe + ".Plain", pattern));
            Assert.IsFalse(Regex.IsMatch(Probe + ".OnlyByName", pattern));
        }

        [Test]
        public void AQuerySelectsTestsAndTheirMode()
        {
            Assert.IsTrue(Tool_Tests.TrySelectTests("tests | where fixture == \"RqlTestsProbe\"", out var groups, out var names, out var mode, out var error), error);
            Assert.AreEqual(TestMode.EditMode, mode);
            Assert.AreEqual(3, names.Count);
            Assert.AreEqual(1, groups!.Length, "one combined pattern");
            // A projected column works as well as whole tests.
            Assert.IsTrue(Tool_Tests.TrySelectTests("tests | where fixture == \"RqlTestsProbe\" | select fullName", out _, out names, out _, out error), error);
            Assert.AreEqual(3, names.Count);
            Assert.IsTrue(Tool_Tests.TrySelectTests("tests | where fixture == \"DemoTest\" and namespace == \"com.IvanMurzak.Unity.MCP.Runtime.Tests\"", out _, out mode, out error), error);
            Assert.AreEqual(TestMode.PlayMode, mode);
        }

        [Test]
        public void SelectionsThatCannotRunAreRefused()
        {
            Assert.IsFalse(Tool_Tests.TrySelectTests("tests | where fixture == \"NoSuchFixture\"", out _, out _, out var error));
            StringAssert.Contains("no tests", error);
            Assert.IsFalse(Tool_Tests.TrySelectTests("tests | where fixture == \"RqlTestsProbe\" or namespace == \"com.IvanMurzak.Unity.MCP.Runtime.Tests\"", out _, out _, out error));
            StringAssert.Contains("mixes EditMode and PlayMode", error);
            Assert.IsFalse(Tool_Tests.TrySelectTests("objects | select name", out _, out _, out error));
            Assert.IsFalse(Tool_Tests.TrySelectTests("tests | where", out _, out _, out error));
            StringAssert.Contains("query failed", error);
        }

        [UnityTest]
        public IEnumerator TheSelectionMatchesUnitysOwnTestTree()
        {
            // The patterns are what Unity receives as Filter.groupNames: they must hit exactly the probe's cases in Unity's
            // own test tree (Plain, Cases(1), Cases(2); the explicit test is filtered out by the query).
            Assert.IsTrue(Tool_Tests.TrySelectTests("tests | where fixture == \"RqlTestsProbe\" and explicit == false", out var groups, out _, out var error), error);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            ITestAdaptor? root = null;
            api.RetrieveTestList(TestMode.EditMode, r => root = r);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (root == null && DateTime.UtcNow < deadline) yield return null;
            Assert.IsNotNull(root, "Unity's test tree was not retrieved");
            var hits = Leaves(root!).Where(t => groups!.Any(g => Regex.IsMatch(t.FullName, g))).Select(t => t.FullName).OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(new[] { Probe + ".Cases(1)", Probe + ".Cases(2)", Probe + ".Plain" }, hits);
            // tests-run refuses a selection whose names are missing from Unity's tree instead of running a subset.
            CollectionAssert.AreEqual(new[] { "No.Such.Fixture.Test" },
                Tool_Tests.UnmatchedNames(root!, new[] { Probe + ".Plain", Probe + ".Cases", "No.Such.Fixture.Test" }));
            Object.DestroyImmediate(api);
        }

        static System.Collections.Generic.IEnumerable<ITestAdaptor> Leaves(ITestAdaptor t) =>
            t.HasChildren ? t.Children.SelectMany(Leaves) : new[] { t };

        [Test]
        public void ResultsAreQueryableAndProjected()
        {
            RqlTests.SetLastResults(new[]
            {
                new RqlTestResult { name = "A.Pass", status = "Passed", durationMs = 1.5 },
                new RqlTestResult { name = "A.Fail", status = "Failed", durationMs = 3, message = "Expected 1 but was 2", stackTrace = "at A.Fail()" },
            });
            Assert.AreEqual(1L, Count("testResults | where status == \"Failed\" | count"));

            var summary = new TestSummaryData { Status = TestRunStatus.Failed, TotalTests = 2, PassedTests = 1, FailedTests = 1, Duration = TimeSpan.FromSeconds(1.2) };
            var response = TestResultCollector.ProjectedResponse(summary, "where status == \"Failed\" | select name message");
            var text = Text(response);
            StringAssert.StartsWith("Failed: 1 passed, 1 failed, 0 skipped of 2 in 1.2s", text);
            StringAssert.Contains("A.Fail", text);
            StringAssert.Contains("Expected 1 but was 2", text);
            StringAssert.DoesNotContain("A.Pass", text);
            StringAssert.DoesNotContain("at A.Fail()", text);   // not selected

            var broken = Text(TestResultCollector.ProjectedResponse(summary, "where"));
            StringAssert.Contains("result query failed", broken);
            StringAssert.StartsWith("Failed: 1 passed", broken);
        }

        [Test]
        public void TheToolDescribesQueryAndResult()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var tool = UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Single(t => t.Name == Tool_Tests.TestsRunToolId);
            var schema = tool.InputSchema!.ToString();
            StringAssert.Contains("query", schema);
            StringAssert.Contains("result", schema);
        }
    }
}
