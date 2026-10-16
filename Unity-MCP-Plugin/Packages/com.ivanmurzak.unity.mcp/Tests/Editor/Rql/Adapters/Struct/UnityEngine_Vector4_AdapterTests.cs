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
    public class UnityEngine_Vector4_AdapterTests : AdapterTestBase
    {
        private RqlTestBehaviour _b = null!;
        private const string B = "objects | get components | where $ is RqlTestBehaviour";

        [SetUp]
        public void Holder() => _b = new GameObject("Holder").AddComponent<RqlTestBehaviour>();

        [Test]
        public void EncodesCompactlyAndAcceptsTextListAndObjectLiterals()
        {
            AssertOk(M(B + " | set weights = \"(1, 2, 3, 4)\""));
            Assert.AreEqual("(1, 2, 3, 4)", Cell(Q(B + " | select weights")));
            AssertOk(M(B + " | set weights = [5, 6, 7, 8]"));
            Assert.AreEqual(new Vector4(5, 6, 7, 8), _b.weights);
            AssertOk(M(B + " | set weights = {x: 1, y: 1, z: 1, w: 1}"));
            Assert.AreEqual(Vector4.one, _b.weights);
        }

        [Test]
        public void ComparesApproximately()
        {
            _b.weights = new Vector4(1, 2, 3, 4);
            Assert.AreEqual("Holder", Cell(Q(B + " | where weights == \"(1, 2, 3, 4)\" | select gameObject.name")));
        }

        [Test]
        public void RejectsTheWrongNumberOfComponents() =>
            AssertFails(M(B + " | set weights = [1, 2, 3]"), "coercion-failed", "(x, y, z, w)");
    }
}
