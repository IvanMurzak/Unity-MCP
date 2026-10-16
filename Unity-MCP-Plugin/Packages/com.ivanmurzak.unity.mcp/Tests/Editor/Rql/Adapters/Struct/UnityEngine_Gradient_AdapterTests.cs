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
using System.Linq;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Gradient_AdapterTests : AdapterTestBase
    {
        private RqlTestBehaviour _b = null!;
        private const string B = "objects | get components | where $ is RqlTestBehaviour";

        [SetUp]
        public void Holder() => _b = new GameObject("Holder").AddComponent<RqlTestBehaviour>();

        [Test]
        public void KeysAreWrittenAndReadAsColorTimePairs()
        {
            AssertOk(M(B + " | set gradient = [[\"#ff0000\", 0], [\"(0, 0, 1, 0.5)\", 1]]"));
            Assert.AreEqual(2, _b.gradient.colorKeys.Length);
            Assert.AreEqual(Color.red, _b.gradient.colorKeys[0].color);
            Assert.AreEqual(0.5f, _b.gradient.Evaluate(1).a, 1e-3f);
            var text = Table(Q(B + " | select gradient"));
            StringAssert.Contains("(1, 0, 0, 1)", text);
            StringAssert.Contains("(0, 0, 1, 0.5)", text);
        }

        [Test]
        public void AcceptsObjectKeys()
        {
            AssertOk(M(B + " | set gradient = [{color: \"#00ff00\", time: 0}, {color: \"#0000ff\", time: 1}]"));
            Assert.AreEqual(Color.green, _b.gradient.colorKeys[0].color);
        }

        [Test]
        public void ComparesKeyByKeyThroughTheColorAdapter()
        {
            AssertOk(M(B + " | set gradient = [[\"#ff0000\", 0], [\"#0000ff\", 1]]"));
            Assert.AreEqual("Holder", Cell(Q(B + " | where gradient == [[\"(1, 0, 0, 1)\", 0], [[0, 0, 1], 1]] | select gameObject.name")));
            Assert.AreEqual(0, Q(B + " | where gradient == [[\"#ff0000\", 0]] | select gameObject.name").Rows!.Count);
        }

        [Test]
        public void ReportsWhereABadKeyIs()
        {
            AssertFails(M(B + " | set gradient = [[\"#ff0000\", 0], [\"nope\", 1]]"), "coercion-failed", "[1].color");
            var nine = "[" + string.Join(", ", Enumerable.Range(0, 9).Select(i => $"[\"#ffffff\", {i / 8f:0.###}]")) + "]";
            AssertFails(M(B + " | set gradient = " + nine), "coercion-failed", "1..8 keys");
        }

        [Test]
        public void ComparesAlphaAsItIsRead()
        {
            AssertOk(M(B + " | set gradient = [[\"(1, 0, 0, 1)\", 0], [\"(0, 0, 1, 0.5)\", 1]]"));
            Assert.AreEqual("Holder", Cell(Q(B + " | where gradient == [[\"(1, 0, 0, 1)\", 0], [\"(0, 0, 1, 0.5)\", 1]] | select gameObject.name")));
            Assert.AreEqual(0, Q(B + " | where gradient == [[\"#ff0000\", 0], [\"#0000ff\", 1]] | select gameObject.name").Rows!.Count);
        }
    }
}
