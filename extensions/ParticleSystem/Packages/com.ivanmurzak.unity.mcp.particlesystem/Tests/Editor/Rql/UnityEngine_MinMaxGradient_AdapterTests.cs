/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ParticleSystem) │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    public class UnityEngine_MinMaxGradient_AdapterTests : ParticleTestBase
    {
        [Test]
        public void AColorInTheColorAdaptersFormat()
        {
            // "#ff0000" is the Color adapter's own format: the MinMaxGradient adapter reaches it through the kernel.
            Assert.IsTrue(M(P + " | set main.startColor = \"#ff0000\"").Ok);
            Assert.AreEqual(ParticleSystemGradientMode.Color, Ps.main.startColor.mode);
            Assert.AreEqual(Color.red, Ps.main.startColor.color);
            Assert.AreEqual("(1, 0, 0, 1)", Cell(Q(P + " | select main.startColor")));
        }

        [Test]
        public void TwoColorsAreARandomRange()
        {
            var r = M(P + " | set main.startColor = [\"#ff0000\", \"(0, 0, 1, 1)\"]");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(ParticleSystemGradientMode.TwoColors, Ps.main.startColor.mode);
            Assert.AreEqual(Color.blue, Ps.main.startColor.colorMax);
        }

        [Test]
        public void GradientKeysThroughTheGradientAdapter()
        {
            var r = M(P + " | set main.startColor = [[\"#ff0000\", 0], [\"#00ff00\", 0.5], [\"#0000ff\", 1]]");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(ParticleSystemGradientMode.Gradient, Ps.main.startColor.mode);
            Assert.AreEqual(3, Ps.main.startColor.gradient.colorKeys.Length);
            StringAssert.Contains("(0, 1, 0, 1)", Table(Q(P + " | select c=main.startColor")));
        }

        [Test]
        public void NestedErrorsKeepTheirPath()
        {
            var bad = M(P + " | set main.startColor = [[\"#ff0000\", 0], [\"nope\", 1]]");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("[1].color", Table(bad));
        }

        [Test]
        public void ComparesInTheWrittenForms()
        {
            Assert.IsTrue(M(P + " | set main.startColor = \"#ff0000\"").Ok);
            Assert.AreEqual(1L, Count(P + " | where main.startColor == \"(1, 0, 0, 1)\" | count"));
            Assert.AreEqual(0L, Count(P + " | where main.startColor == \"#0000ff\" | count"));
            Assert.IsTrue(M(P + " | set main.startColor = [\"#ff0000\", \"#0000ff\"]").Ok);
            Assert.AreEqual(1L, Count(P + " | where main.startColor == [\"#ff0000\", \"#0000ff\"] | count"));
        }
    }
}
