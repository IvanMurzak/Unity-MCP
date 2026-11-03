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
    using NavMeshModifierVolume = global::Unity.AI.Navigation.NavMeshModifierVolume;

    public class Unity_AI_Navigation_NavMeshModifierVolume_AdapterTests : NavigationTestBase
    {
        private const string Volumes = "objects | get components | where $ is NavMeshModifierVolume";

        [Test]
        public void AddAndConfigure()
        {
            new GameObject("Mud");
            Ok(M("objects | where name == \"Mud\" | call AddComponent(\"NavMeshModifierVolume\")"));
            Ok(M(Volumes + " | set size = \"(2, 3, 4)\", center = [0, 1.5, 0], areaName = \"Not Walkable\", affectedAgents = [\"Humanoid\"]"));
            var v = Object.FindAnyObjectByType<NavMeshModifierVolume>();
            Assert.AreEqual(new Vector3(2, 3, 4), v.size);
            Assert.AreEqual(new Vector3(0, 1.5f, 0), v.center);
            Assert.AreEqual(1, v.area);
            Assert.IsTrue(v.AffectsAgentType(0));
            Assert.IsFalse(v.AffectsAgentType(12345));
        }

        [Test]
        public void NotWalkableVolumeCutsAHole()
        {
            CreateGround();
            new GameObject("Hole").AddComponent<NavMeshModifierVolume>();
            Ok(M(Volumes + " | set size = \"(4, 4, 4)\", center = \"(0, 0, 0)\", areaName = \"Not Walkable\""));
            Ok(M(Surfaces + " | call Bake()"));
            Assert.IsFalse(NavMesh.SamplePosition(Vector3.zero, out _, 0.5f, NavMesh.AllAreas), "inside the volume");
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(4, 0, 4), out _, 0.5f, NavMesh.AllAreas), "outside the volume");
        }
    }
}
