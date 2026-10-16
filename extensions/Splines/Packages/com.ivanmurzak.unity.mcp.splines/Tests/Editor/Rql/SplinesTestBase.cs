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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using Unity.Mathematics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    /// <summary>A fresh scene with one SplineContainer "Road" (spline 0: AutoSmooth knots at x = 0, 5, 10) per test.</summary>
    public abstract class SplinesTestBase
    {
        protected SplineContainer Road = null!;
        protected const string C = "objects | get components | where $ is SplineContainer";
        protected const string R = C + " | where gameObject.name == \"Road\"";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            Road = new GameObject("Road").AddComponent<SplineContainer>();
            Road.Spline.Add(new float3(0, 0, 0), TangentMode.AutoSmooth);
            Road.Spline.Add(new float3(5, 0, 0), TangentMode.AutoSmooth);
            Road.Spline.Add(new float3(10, 0, 0), TangentMode.AutoSmooth);
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        protected static QueryResult Ok(QueryResult r)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        protected static long Count(string rql) => Ok(Q(rql)).Value!.GetValue<long>();

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0)
        {
            Ok(r);
            return r.Rows![0][column]!.ToJsonString().Trim('"');
        }

        /// <summary>The result column of a call receipt ($ref | method | result).</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        /// <summary>Parse the compact "(x, y, z)" vector output.</summary>
        protected static Vector3 Vec(string text)
        {
            var p = text.Trim('(', ')', '"').Split(',');
            float F(int i) => float.Parse(p[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            return new Vector3(F(0), F(1), F(2));
        }

        protected static void AreClose(Vector3 expected, Vector3 actual, float tolerance = 1e-4f) =>
            Assert.Less(Vector3.Distance(expected, actual), tolerance, $"expected {expected}, got {actual}");
    }
}
