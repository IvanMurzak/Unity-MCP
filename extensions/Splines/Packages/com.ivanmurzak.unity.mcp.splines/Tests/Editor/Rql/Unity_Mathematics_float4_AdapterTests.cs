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
    public class Unity_Mathematics_float4_AdapterTests : MathematicsTestBase
    {
        [Test]
        public void WrittenAndPrintedAsATuple()
        {
            Ok(M("probe | set f4 = [1, 2, 3, 4]"));
            Assert.AreEqual(new float4(1, 2, 3, 4), Probe.f4);
            Assert.AreEqual("(1, 2, 3, 4)", Cell(Q("probe | select f4")));
            Assert.AreEqual(1L, Count("probe | where f4 == \"(1, 2, 3, 4)\" | count"));
        }

        [Test]
        public void WrongWidthIsRejected() => Assert.IsFalse(M("probe | set f4 = \"(1, 2)\"").Ok);
    }
}
