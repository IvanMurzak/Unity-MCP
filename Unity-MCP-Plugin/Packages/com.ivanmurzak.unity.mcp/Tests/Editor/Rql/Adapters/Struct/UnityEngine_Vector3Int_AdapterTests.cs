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
    public class UnityEngine_Vector3Int_AdapterTests : AdapterTestBase
    {
        private RqlTestBehaviour _b = null!;
        private const string B = "objects | get components | where $ is RqlTestBehaviour";

        [SetUp]
        public void Holder() => _b = new GameObject("Holder").AddComponent<RqlTestBehaviour>();

        [Test]
        public void WholeNumbersInTextListAndObjectForm()
        {
            AssertOk(M(B + " | set cell = \"(1, -2, 3)\""));
            Assert.AreEqual(new Vector3Int(1, -2, 3), _b.cell);
            Assert.AreEqual("(1, -2, 3)", Cell(Q(B + " | select cell")));
            AssertOk(M(B + " | set cell = [4, 5, 6]"));
            Assert.AreEqual(new Vector3Int(4, 5, 6), _b.cell);
            Assert.AreEqual("Holder", Cell(Q(B + " | where cell == \"(4, 5, 6)\" | select gameObject.name")));
        }

        [Test]
        public void FractionsAreRejected() =>
            AssertFails(M(B + " | set cell = \"(1.5, 0, 0)\""), "coercion-failed", "whole numbers");
    }
}
