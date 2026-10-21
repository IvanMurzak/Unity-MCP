/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    public class UnityEngine_LayerMask_AdapterTests : NavigationTestBase
    {
        [Test]
        public void NumbersNamesAndLists()
        {
            var s = CreateGround();
            Ok(M(Surfaces + " | set layerMask = 5"));
            Assert.AreEqual(5, s.layerMask.value);
            Assert.AreEqual("5", Cell(Q(Surfaces + " | select layerMask")));
            Ok(M(Surfaces + " | set layerMask = \"Water\""));
            Assert.AreEqual(1 << 4, s.layerMask.value);
            Ok(M(Surfaces + " | set layerMask = [\"Default\", \"Water\"]"));
            Assert.AreEqual(1 | (1 << 4), s.layerMask.value);
            Ok(M(Surfaces + " | set layerMask = \"Everything\""));
            Assert.AreEqual(-1, s.layerMask.value);
            Assert.AreEqual(1L, Q(Surfaces + " | where layerMask == -1 | count").Value!.GetValue<long>());
        }

        [Test]
        public void UnknownLayerChangesNothing()
        {
            var s = CreateGround();
            var r = M(Surfaces + " | set layerMask = [\"Default\", \"Lava\"]");
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("Lava", Table(r));
            Assert.AreEqual(-1, s.layerMask.value);
        }
    }
}
