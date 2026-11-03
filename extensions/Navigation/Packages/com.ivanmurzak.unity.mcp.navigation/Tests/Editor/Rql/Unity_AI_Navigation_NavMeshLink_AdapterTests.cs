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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    using NavMeshLink = global::Unity.AI.Navigation.NavMeshLink;

    public class Unity_AI_Navigation_NavMeshLink_AdapterTests : NavigationTestBase
    {
        private const string Links = "objects | get components | where $ is NavMeshLink";

        [Test]
        public void AddAndConfigure()
        {
            new GameObject("Gap");
            Ok(M("objects | where name == \"Gap\" | call AddComponent(\"NavMeshLink\")"));
            Ok(M(Links + " | set startPoint = \"(0, 0, -1)\", endPoint = \"(0, 1, 3)\", width = 1.5, bidirectional = false, costModifier = 3, areaName = \"Jump\", agentType = \"Humanoid\""));
            var l = Object.FindAnyObjectByType<NavMeshLink>();
            Assert.AreEqual(new Vector3(0, 0, -1), l.startPoint);
            Assert.AreEqual(new Vector3(0, 1, 3), l.endPoint);
            Assert.AreEqual(1.5f, l.width, 1e-6f);
            Assert.IsFalse(l.bidirectional);
            Assert.AreEqual(3f, l.costModifier, 1e-6f);
            Assert.AreEqual(2, l.area);
            Assert.AreEqual("Jump", Cell(Q(Links + " | select areaName")));
        }

        [Test]
        public void NegativeCostModifierGoesThroughTheProperty()
        {
            var l = new GameObject("Gap").AddComponent<NavMeshLink>();
            Ok(M(Links + " | set costModifier = -1"));
            Assert.Less(l.costModifier, 0f, "negative = use the area cost (stored as a flag plus a positive value)");
            Assert.AreEqual("-1", Cell(Q(Links + " | select costModifier")));
        }

        [Test]
        public void BackingFieldsAreHidden()
        {
            new GameObject("Gap").AddComponent<NavMeshLink>();
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "NavMeshLink");
            StringAssert.DoesNotContain("m_CostModifier", schema);
            StringAssert.DoesNotContain("m_IsOverridingCost", schema);
            StringAssert.Contains("costModifier", schema);
            StringAssert.DoesNotContain("costOverride", schema, "[Obsolete] members stay hidden");
        }
    }
}
