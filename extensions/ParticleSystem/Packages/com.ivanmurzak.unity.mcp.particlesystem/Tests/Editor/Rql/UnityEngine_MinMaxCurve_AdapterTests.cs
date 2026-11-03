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

    public class UnityEngine_MinMaxCurve_AdapterTests : ParticleTestBase
    {
        [Test]
        public void ANumberIsAConstant()
        {
            Assert.IsTrue(M(P + " | set main.startLifetime = 3").Ok);
            Assert.AreEqual(ParticleSystemCurveMode.Constant, Ps.main.startLifetime.mode);
            Assert.AreEqual(3f, Ps.main.startLifetime.constant, 1e-6f);
            Assert.AreEqual("3", Cell(Q(P + " | select main.startLifetime")));
        }

        [Test]
        public void TwoNumbersAreARandomRange()
        {
            Assert.IsTrue(M(P + " | set emission.rateOverTime = [5, 25]").Ok);
            Assert.AreEqual(ParticleSystemCurveMode.TwoConstants, Ps.emission.rateOverTime.mode);
            Assert.AreEqual(5f, Ps.emission.rateOverTime.constantMin, 1e-6f);
            Assert.AreEqual(25f, Ps.emission.rateOverTime.constantMax, 1e-6f);
            StringAssert.Contains("[5,25]", Cell(Q(P + " | select emission.rateOverTime")).Replace(" ", ""));
        }

        [Test]
        public void CurvesAreReportedByMode()
        {
            var main = Ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(2f, AnimationCurve.Linear(0, 0, 1, 1));
            StringAssert.Contains("Curve", Cell(Q(P + " | select main.startSize")));
        }

        [Test]
        public void OtherValuesAreRejected()
        {
            var bad = M(P + " | set main.startLifetime = \"long\"");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("expected a number or [min, max]", Table(bad));
            var badRange = M(P + " | set main.startLifetime = [1, \"x\"]");
            Assert.IsFalse(badRange.Ok);
            StringAssert.Contains("[1]", Table(badRange));
        }

        [Test]
        public void ComparesInTheWrittenForms()
        {
            Assert.IsTrue(M(P + " | set main.startLifetime = 3, emission.rateOverTime = [5, 25]").Ok);
            Assert.AreEqual(1L, Count(P + " | where main.startLifetime == 3 | count"));
            Assert.AreEqual(0L, Count(P + " | where main.startLifetime == 4 | count"));
            Assert.AreEqual(1L, Count(P + " | where emission.rateOverTime == [5, 25] | count"));
            Assert.AreEqual(0L, Count(P + " | where emission.rateOverTime == 5 | count"));
        }
    }
}
