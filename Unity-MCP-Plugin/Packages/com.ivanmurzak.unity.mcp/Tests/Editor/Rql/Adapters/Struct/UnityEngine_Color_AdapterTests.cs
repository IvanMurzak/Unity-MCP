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
    public class UnityEngine_Color_AdapterTests : AdapterTestBase
    {
        private Light _light = null!;
        private const string L = "objects | get components | where $ is Light";

        [SetUp]
        public void Lamp() => _light = new GameObject("Lamp").AddComponent<Light>();

        [Test]
        public void EncodesAsFourComponents()
        {
            _light.color = new Color(1, 0.5f, 0, 1);
            Assert.AreEqual("(1, 0.5, 0, 1)", Cell(Q(L + " | select color")));
        }

        [Test]
        public void AcceptsHexThreeAndFourComponentsAndObjectLiterals()
        {
            AssertOk(M(L + " | set color = \"#00ff00\""));
            Assert.AreEqual(Color.green, _light.color);
            AssertOk(M(L + " | set color = [0, 0, 1]"));
            Assert.AreEqual(Color.blue, _light.color);
            AssertOk(M(L + " | set color = \"(1, 0, 0, 0.5)\""));
            Assert.AreEqual(new Color(1, 0, 0, 0.5f), _light.color);
            AssertOk(M(L + " | set color = {r: 1, g: 1, b: 1, a: 1}"));
            Assert.AreEqual(Color.white, _light.color);
        }

        [Test]
        public void ComparesAgainstAnyLiteralForm()
        {
            _light.color = Color.red;
            Assert.AreEqual("Lamp", Cell(Q(L + " | where color == \"#ff0000\" | select gameObject.name")));
            Assert.AreEqual("Lamp", Cell(Q(L + " | where color == [1, 0, 0, 1] | select gameObject.name")));
        }

        [Test]
        public void RejectsMalformedColors() =>
            AssertFails(M(L + " | set color = \"#nothex\""), "coercion-failed", "#RRGGBB");
    }
}
