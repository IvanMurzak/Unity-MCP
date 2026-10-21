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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.YOUR_FEATURE_NAME
{
    using GameObject = UnityEngine.GameObject;
    using Light = UnityEngine.Light;
    using LightType = UnityEngine.LightType;

    /// <summary>
    /// A fresh empty scene with one point light ("Bulb", intensity 1) per test, and a fresh RQL engine (modules re-discovered).
    /// <see cref="Q"/> runs a read-only query (rql-query), <see cref="M"/> allows mutations (rql-mutate).
    /// </summary>
    public abstract class YOUR_FEATURE_NAMETestBase
    {
        protected const string L = "objects | get components | where $ is Light";

        protected Light Bulb = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            Bulb = new GameObject("Bulb").AddComponent<Light>();
            Bulb.type = LightType.Point;
            Bulb.intensity = 1f;
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql)
        {
            var r = M(rql);
            Assert.IsTrue(r.Ok, rql + "\n" + Table(r));
            return r;
        }

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>.</summary>
        protected static void Fails(string rql, string expected)
        {
            var r = M(rql);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r.Rows![0][column]!.ToJsonString().Trim('"');
        }

        /// <summary>The <c>result</c> column of a call receipt ($ref, method, result).</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        /// <summary>What rql-schema prints: the source list (null) or one type's members.</summary>
        protected static string Schema(string? typeName) => RqlUnitySchema.Render(RqlUnity.Engine, typeName);
    }
}
