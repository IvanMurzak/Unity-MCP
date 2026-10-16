/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class Unity_Mathematics_quaternion_AdapterTests : MathematicsTestBase
    {
        [Test]
        public void FourNumbersAreAQuaternion()
        {
            Ok(M("probe | set q = \"(0, 0, 0, 1)\""));
            Assert.AreEqual(quaternion.identity, Probe.q);
            Assert.AreEqual("(0, 0, 0, 1)", Cell(Q("probe | select q")));
        }

        [Test]
        public void ThreeNumbersAreEulerDegrees()
        {
            Ok(M("probe | set q = \"(0, 90, 0)\""));
            var euler = ((Quaternion)Probe.q).eulerAngles;
            Assert.AreEqual(90f, euler.y, 1e-3f);
            Assert.AreEqual(1L, Count("probe | where q == \"(0, 90, 0)\" | count"));
        }

        [Test]
        public void OtherWidthsAreRejected()
        {
            var bad = M("probe | set q = [1, 2]");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("euler", Table(bad));
        }
    }
}
