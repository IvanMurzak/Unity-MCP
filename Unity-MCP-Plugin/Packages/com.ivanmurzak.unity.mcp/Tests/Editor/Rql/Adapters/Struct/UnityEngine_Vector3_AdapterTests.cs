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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters
{
    public class UnityEngine_Vector3_AdapterTests : AdapterTestBase
    {
        private GameObject _go = null!;
        private const string Go = "objects | where name == \"Probe\"";

        [SetUp]
        public void Probe() => _go = new GameObject("Probe");

        [Test]
        public void EncodesCompactlyAndAcceptsTextListAndObjectLiterals()
        {
            AssertOk(M(Go + " | set transform.position = \"(1, 2, 3)\""));
            Assert.AreEqual("(1, 2, 3)", Cell(Q(Go + " | select transform.position")));
            AssertOk(M(Go + " | set transform.position = [4, 5, 6]"));
            Assert.AreEqual(new Vector3(4, 5, 6), _go.transform.position);
            AssertOk(M(Go + " | set transform.position = {x: 7, y: 8, z: 9}"));
            Assert.AreEqual(new Vector3(7, 8, 9), _go.transform.position);
        }

        [Test]
        public void WritesOneComponentThroughThePath()
        {
            _go.transform.position = new Vector3(1, 2, 3);
            AssertOk(M(Go + " | set transform.position.y = transform.position.y + 10"));
            Assert.AreEqual(new Vector3(1, 12, 3), _go.transform.position);
        }

        [Test]
        public void ComparesApproximately()
        {
            _go.transform.position = new Vector3(0, 1.000001f, 0);
            Assert.AreEqual("Probe", Cell(Q("objects | where transform.position == \"(0, 1, 0)\" | select name")));
            Assert.AreEqual("Probe", Cell(Q("objects | where transform.position == [0, 1, 0] | select name")));
            Assert.AreEqual(0, Q("objects | where transform.position == \"(0, 2, 0)\" | select name").Rows!.Count);
        }

        [Test]
        public void RejectsMalformedValues()
        {
            AssertFails(M(Go + " | set transform.position = \"(1, 2)\""), "coercion-failed", "(x, y, z)");
            AssertFails(M(Go + " | set transform.position = [1, \"two\", 3]"), "coercion-failed");
            Assert.AreEqual(Vector3.zero, _go.transform.position);
        }
    }
}
