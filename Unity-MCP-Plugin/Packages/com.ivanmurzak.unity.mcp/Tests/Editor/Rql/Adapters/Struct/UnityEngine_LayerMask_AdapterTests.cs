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
    public class UnityEngine_LayerMask_AdapterTests : AdapterTestBase
    {
        private RqlTestBehaviour _b = null!;
        private const string B = "objects | get components | where $ is RqlTestBehaviour";

        [SetUp]
        public void Holder() => _b = new GameObject("Holder").AddComponent<RqlTestBehaviour>();

        [Test]
        public void AcceptsNamesListsAndNumbers()
        {
            AssertOk(M(B + " | set layers = \"Water\""));
            Assert.AreEqual(1 << LayerMask.NameToLayer("Water"), _b.layers.value);
            AssertOk(M(B + " | set layers = [\"Default\", \"UI\"]"));
            Assert.AreEqual((1 << 0) | (1 << LayerMask.NameToLayer("UI")), _b.layers.value);
            AssertOk(M(B + " | set layers = \"Everything\""));
            Assert.AreEqual(~0, _b.layers.value);
            AssertOk(M(B + " | set layers = 5"));
            Assert.AreEqual("5", Cell(Q(B + " | select layers")));
        }

        [Test]
        public void RejectsUnknownLayers() =>
            AssertFails(M(B + " | set layers = \"NoSuchLayer\""), "coercion-failed", "no layer named 'NoSuchLayer'");
    }
}
