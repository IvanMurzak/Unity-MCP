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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    using NavMeshSurface = global::Unity.AI.Navigation.NavMeshSurface;

    /// <summary>
    /// A fresh scene per test; the Humanoid agent type and area slot 3 (the project settings the tests write) are restored
    /// afterwards so the test project's ProjectSettings stay unchanged.
    /// </summary>
    public abstract class NavigationTestBase
    {
        protected const string Surfaces = "objects | get components | where $ is NavMeshSurface";
        protected const string Agents = "objects | get components | where $ is NavMeshAgent";

        private float _radius, _height, _slope, _climb;
        private string _area3Name = "";
        private float _area3Cost;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            var humanoid = NavigationApi.Instance.agentTypes[0];
            (_radius, _height, _slope, _climb) = (humanoid.agentRadius, humanoid.agentHeight, humanoid.agentSlope, humanoid.agentClimb);
            var area3 = NavigationApi.Instance.areas[3];
            (_area3Name, _area3Cost) = (area3.name, area3.cost);
        }

        [TearDown]
        public void TearDown()
        {
            var humanoid = NavigationApi.Instance.agentTypes[0];
            humanoid.agentRadius = _radius;
            humanoid.agentHeight = _height;
            humanoid.agentSlope = _slope;
            humanoid.agentClimb = _climb;
            var area3 = NavigationApi.Instance.areas[3];
            area3.name = _area3Name;
            area3.cost = _area3Cost;
        }

        /// <summary>A 10 × 10 plane with a NavMeshSurface collecting everything ("Ground").</summary>
        protected static NavMeshSurface CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            return ground.AddComponent<NavMeshSurface>();
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        protected static void Ok(QueryResult r) => Assert.IsTrue(r.Ok, Table(r));

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r)
        {
            Ok(r);
            return r.Rows![0][0]!.ToJsonString().Trim('"');
        }
    }
}
