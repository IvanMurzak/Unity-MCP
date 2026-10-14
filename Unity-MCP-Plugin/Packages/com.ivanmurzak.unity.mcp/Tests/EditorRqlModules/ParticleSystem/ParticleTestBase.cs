/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    /// <summary>A fresh scene with one particle system ("Sparks") per test.</summary>
    public abstract class ParticleTestBase
    {
        protected ParticleSystem Ps = null!;
        protected const string P = "objects | get components | where $ is ParticleSystem";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            Ps = new GameObject("Sparks").AddComponent<ParticleSystem>();
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        protected static long Count(string rql)
        {
            var r = Q(rql);
            Assert.IsTrue(r.Ok, Table(r));
            return r.Value!.GetValue<long>();
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r.Rows![0][0]!.ToJsonString().Trim('"');
        }
    }
}
