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
using UnityEngine;
using UnityEngine.AI;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    using NavMeshModifier = global::Unity.AI.Navigation.NavMeshModifier;

    public class Unity_AI_Navigation_NavMeshModifier_AdapterTests : NavigationTestBase
    {
        private const string Modifiers = "objects | get components | where $ is NavMeshModifier";

        [Test]
        public void AddAndOverrideTheAreaByName()
        {
            new GameObject("Water");
            Ok(M("objects | where name == \"Water\" | call AddComponent(\"NavMeshModifier\")"));
            Ok(M(Modifiers + " | set overrideArea = true, areaName = \"Jump\", ignoreFromBuild = true, applyToChildren = false"));
            var m = Object.FindAnyObjectByType<NavMeshModifier>();
            Assert.IsTrue(m.overrideArea);
            Assert.AreEqual(2, m.area);
            Assert.IsTrue(m.ignoreFromBuild);
            Assert.IsFalse(m.applyToChildren);
            Assert.AreEqual("Jump", Cell(Q(Modifiers + " | select areaName")));
        }

        [Test]
        public void AffectedAgentsAreNames()
        {
            var m = new GameObject("Water").AddComponent<NavMeshModifier>();
            Assert.AreEqual("[\"All\"]", Cell(Q(Modifiers + " | select affectedAgents")).Replace(" ", ""));
            Ok(M(Modifiers + " | set affectedAgents = [\"Humanoid\"]"));
            Assert.IsTrue(m.AffectsAgentType(0));
            Assert.IsFalse(m.AffectsAgentType(12345));
            Ok(M(Modifiers + " | set affectedAgents = []"));
            Assert.IsFalse(m.AffectsAgentType(0));
            Ok(M(Modifiers + " | set affectedAgents = [\"All\"]"));
            Assert.IsTrue(m.AffectsAgentType(12345));
        }

        [Test]
        public void UnknownNamesAreRejected()
        {
            var m = new GameObject("Water").AddComponent<NavMeshModifier>();
            var r = M(Modifiers + " | set areaName = \"Lava\"");
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("Walkable", Table(r), "the error lists the defined areas");
            Assert.IsFalse(M(Modifiers + " | set affectedAgents = [\"Giant\"]").Ok);
            Assert.AreEqual(0, m.area);
            Assert.IsTrue(m.AffectsAgentType(0));
        }

        [Test]
        public void IgnoredGeometryIsNotBaked()
        {
            var surface = CreateGround();
            surface.gameObject.AddComponent<NavMeshModifier>();
            Ok(M(Modifiers + " | set ignoreFromBuild = true"));
            Ok(M(Surfaces + " | call Bake()"));
            Assert.IsFalse(NavMesh.SamplePosition(new Vector3(1, 0, 1), out _, 0.5f, NavMesh.AllAreas));
        }
    }
}
