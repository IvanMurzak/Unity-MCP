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
    public class UnityEngine_Quaternion_AdapterTests : AdapterTestBase
    {
        private GameObject _go = null!;
        private const string Go = "objects | where name == \"Probe\"";

        [SetUp]
        public void Probe() => _go = new GameObject("Probe");

        [Test]
        public void EncodesCompactlyAndAcceptsTextAndListLiterals()
        {
            Assert.AreEqual("(0, 0, 0, 1)", Cell(Q(Go + " | select transform.rotation")));
            var q = Quaternion.Euler(0, 90, 0);
            AssertOk(M(Go + $" | set transform.rotation = [{q.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}, {q.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}, 0, {q.w.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}]"));
            Assert.AreEqual(90f, _go.transform.eulerAngles.y, 1e-3f);
            AssertOk(M(Go + " | set transform.rotation = \"(0, 0, 0, 1)\""));
            Assert.AreEqual(Quaternion.identity, _go.transform.rotation);
        }

        [Test]
        public void ThreeNumbersAreRejectedWithAHintTowardsEulerAngles()
        {
            AssertFails(M(Go + " | set transform.rotation = \"(0, 90, 0)\""), "coercion-failed", "(x, y, z, w)");
            StringAssert.Contains("eulerAngles", Schema("Quaternion"));
        }
    }
}
