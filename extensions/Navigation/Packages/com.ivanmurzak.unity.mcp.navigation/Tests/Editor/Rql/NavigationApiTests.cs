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
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    using NavMeshModifier = global::Unity.AI.Navigation.NavMeshModifier;

    /// <summary>The navigation source: agent-type bake settings, areas, BakeAll / ClearAll.</summary>
    public class NavigationApiTests : NavigationTestBase
    {
        private const string Humanoid = "navigation | get agentTypes | where name == \"Humanoid\"";

        [Test]
        public void AgentTypesListTheProjectSettings()
        {
            var r = Q("navigation | get agentTypes | select agentTypeID name agentRadius agentSlope");
            Ok(r);
            Assert.AreEqual(NavMesh.GetSettingsCount(), r.Rows!.Count);
            Assert.AreEqual(0, r.Rows[0][0]!.GetValue<long>());
            Assert.AreEqual("Humanoid", r.Rows[0][1]!.GetValue<string>());
            Assert.AreEqual(NavMesh.GetSettingsByID(0).agentRadius, r.Rows[0][2]!.GetValue<double>(), 1e-5);
        }

        [Test]
        public void BakeSettingsAreWrittenToTheAgentType()
        {
            Ok(M(Humanoid + " | set agentRadius = 0.3, agentHeight = 1.7, agentSlope = 30, agentClimb = 0.25"));
            var s = NavMesh.GetSettingsByID(0);
            Assert.AreEqual(0.3f, s.agentRadius, 1e-5f);
            Assert.AreEqual(1.7f, s.agentHeight, 1e-5f);
            Assert.AreEqual(30f, s.agentSlope, 1e-5f);
            Assert.AreEqual(0.25f, s.agentClimb, 1e-5f);
            // a surface baking for the type sees it
            CreateGround();
            Assert.AreEqual(0.3, double.Parse(Cell(Q(Surfaces + " | select buildSettings.agentRadius")), System.Globalization.CultureInfo.InvariantCulture), 1e-5);
        }

        [Test]
        public void BakeSettingWritesAreUndoable()
        {
            var before = NavMesh.GetSettingsByID(0).agentRadius;
            Ok(M(Humanoid + " | set agentRadius = 0.31"));
            Undo.PerformUndo();
            Assert.AreEqual(before, NavMesh.GetSettingsByID(0).agentRadius, 1e-5f);
        }

        [Test]
        public void OutOfRangeSettingsChangeNothing()
        {
            var before = NavMesh.GetSettingsByID(0);
            var r = M(Humanoid + " | set agentRadius = 0.35, agentSlope = 75");
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("60", Table(r));
            Assert.AreEqual(before.agentRadius, NavMesh.GetSettingsByID(0).agentRadius, 1e-5f, "all-or-nothing");
            Assert.AreEqual(before.agentSlope, NavMesh.GetSettingsByID(0).agentSlope, 1e-5f);
        }

        [Test]
        public void AreasNameTheIndices()
        {
            var r = Q("navigation | get areas | where name != \"\" | select index name cost");
            Ok(r);
            StringAssert.Contains("Walkable", Table(r));
            StringAssert.Contains("Jump", Table(r));

            Ok(M("navigation | get areas | where index == 3 | set name = \"Water\", cost = 4"));
            Assert.AreEqual(3, NavMesh.GetAreaFromName("Water"));
            Assert.AreEqual(4f, NavMesh.GetAreaCost(3), 1e-5f);
            var m = new GameObject("Lake").AddComponent<NavMeshModifier>();
            Ok(M("objects | get components | where $ is NavMeshModifier | set overrideArea = true, areaName = \"Water\""));
            Assert.AreEqual(3, m.area);
        }

        [Test]
        public void BuiltInAreasCannotBeRenamed()
        {
            var r = M("navigation | get areas | where index == 0 | set name = \"Floor\"");
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(0, NavMesh.GetAreaFromName("Walkable"));
        }

        [Test]
        public void BakeAllAndClearAll()
        {
            var a = CreateGround();
            var b = CreateGround();
            b.transform.position = new Vector3(20, 0, 0);
            var r = M("navigation | call BakeAll()");
            Ok(r);
            StringAssert.Contains("2", Table(r));
            Assert.IsNotNull(a.navMeshData);
            Assert.IsNotNull(b.navMeshData);
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(20, 0, 1), out _, 0.5f, NavMesh.AllAreas));

            Ok(M("navigation | call ClearAll()"));
            Assert.IsNull(a.navMeshData);
            Assert.IsNull(b.navMeshData);
        }
    }
}
