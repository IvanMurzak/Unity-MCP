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
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Vector2_AdapterTests : AdapterTestBase
    {
        private RqlTestBehaviour _b = null!;
        private const string B = "objects | get components | where $ is RqlTestBehaviour";

        [SetUp]
        public void Holder() => _b = new GameObject("Holder").AddComponent<RqlTestBehaviour>();

        [Test]
        public void EncodesCompactlyAndAcceptsTextListAndObjectLiterals()
        {
            AssertOk(M(B + " | set offset = \"(1.5, -2)\""));
            Assert.AreEqual("(1.5, -2)", Cell(Q(B + " | select offset")));
            AssertOk(M(B + " | set offset = [3, 4]"));
            Assert.AreEqual(new Vector2(3, 4), _b.offset);
            AssertOk(M(B + " | set offset = {x: 5, y: 6}"));
            Assert.AreEqual(new Vector2(5, 6), _b.offset);
        }

        [Test]
        public void ComparesApproximately()
        {
            _b.offset = new Vector2(1, 2.000001f);
            Assert.AreEqual("Holder", Cell(Q(B + " | where offset == \"(1, 2)\" | select gameObject.name")));
            Assert.AreEqual("Holder", Cell(Q(B + " | where offset == [1, 2] | select gameObject.name")));
        }

        [Test]
        public void RejectsTheWrongNumberOfComponents() =>
            AssertFails(M(B + " | set offset = \"(1, 2, 3)\""), "coercion-failed", "(x, y)");
    }
}
