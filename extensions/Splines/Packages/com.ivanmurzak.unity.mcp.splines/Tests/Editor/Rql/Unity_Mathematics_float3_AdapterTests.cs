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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    public class Unity_Mathematics_float3_AdapterTests : MathematicsTestBase
    {
        [Test]
        public void WrittenAndPrintedAsATuple()
        {
            Ok(M("probe | set f3 = \"(1, 2, 3)\""));
            Assert.AreEqual(new float3(1, 2, 3), Probe.f3);
            Assert.AreEqual("(1, 2, 3)", Cell(Q("probe | select f3")));
            Ok(M("probe | set f3 = {x: 4, y: 5, z: 6}"));
            Assert.AreEqual(new float3(4, 5, 6), Probe.f3);
            Ok(M("probe | set f3.y = 7"));
            Assert.AreEqual(7f, Probe.f3.y);
        }

        [Test]
        public void ComparesApproximately()
        {
            Probe.f3 = new float3(1, 2, 3.000001f);
            Assert.AreEqual(1L, Count("probe | where f3 == \"(1, 2, 3)\" | count"));
            Assert.AreEqual(0L, Count("probe | where f3 == [1, 2, 4] | count"));
        }

        [Test]
        public void TextThatIsNotATupleIsRejected()
        {
            var bad = M("probe | set f3 = \"up\"");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("(x, y, z)", Table(bad));
        }
    }
}
