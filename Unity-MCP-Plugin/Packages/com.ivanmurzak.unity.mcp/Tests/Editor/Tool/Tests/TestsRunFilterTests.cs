/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The filter validation counts what Unity will run: assembly, groupNames (namespace / class / RQL) and testNames are
    /// ANDed, as Filter.assemblyNames, groupNames and testNames are.
    /// </summary>
    public class TestsRunFilterTests
    {
        const string Unique = "EditorTests.dll/Ns.Sub.Fixture/Method";
        const string Full = "Ns.Sub.Fixture.Method";

        static bool Matches(string? assembly = null, string? ns = null, string? cls = null, string? method = null, string[]? groups = null)
        {
            var p = new TestFilterParameters(assembly, ns, cls, method) { Groups = groups };
            return Tool_Tests.Matches(Unique, Full, p);
        }

        [Test]
        public void EachFieldAloneSelectsItsTests()
        {
            Assert.IsTrue(Matches(assembly: "EditorTests"));
            Assert.IsTrue(Matches(ns: "Ns"));
            Assert.IsTrue(Matches(cls: "Fixture"));
            Assert.IsTrue(Matches(method: Full));
            Assert.IsFalse(Matches(assembly: "Other"));
            Assert.IsFalse(Matches(ns: "Other"));
            Assert.IsFalse(Matches(cls: "Other"));
            Assert.IsFalse(Matches(method: "Ns.Sub.Fixture.Other"));
        }

        [Test]
        public void DifferentFieldsMustAllMatch()
        {
            Assert.IsFalse(Matches(assembly: "EditorTests", cls: "Missing"));
            Assert.IsFalse(Matches(assembly: "Other", ns: "Ns"));
            Assert.IsFalse(Matches(ns: "Ns", method: "Ns.Sub.Fixture.Other"));
            Assert.IsFalse(Matches(cls: "Fixture", method: "Ns.Sub.Fixture.Other"));
            Assert.IsTrue(Matches(assembly: "EditorTests", ns: "Ns", cls: "Fixture", method: Full));
        }

        [Test]
        public void NamespaceClassAndSelectionShareOneField()
        {
            Assert.IsTrue(Matches(ns: "Missing", cls: "Fixture"));
            Assert.IsTrue(Matches(cls: "Missing", ns: "Ns"));
            Assert.IsTrue(Matches(ns: "Missing", groups: new[] { @"^Ns\.Sub\.Fixture\.Method$" }));
            Assert.IsFalse(Matches(ns: "Missing", cls: "Missing2"));
        }
    }
}
