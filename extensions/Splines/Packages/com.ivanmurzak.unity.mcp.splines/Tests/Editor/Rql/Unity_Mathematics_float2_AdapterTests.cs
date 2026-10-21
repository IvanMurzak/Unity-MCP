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
    public class Unity_Mathematics_float2_AdapterTests : MathematicsTestBase
    {
        [Test]
        public void WrittenAndPrintedAsATuple()
        {
            Ok(M("probe | set f2 = \"(1.5, -2)\""));
            Assert.AreEqual(new float2(1.5f, -2f), Probe.f2);
            Assert.AreEqual("(1.5, -2)", Cell(Q("probe | select f2")));
            Ok(M("probe | set f2 = [3, 4]"));
            Assert.AreEqual(new float2(3, 4), Probe.f2);
            Assert.AreEqual(1L, Count("probe | where f2 == \"(3, 4)\" | count"));
        }

        [Test]
        public void WrongWidthIsRejected()
        {
            var bad = M("probe | set f2 = \"(1, 2, 3)\"");
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("(x, y)", Table(bad));
        }
    }
}
