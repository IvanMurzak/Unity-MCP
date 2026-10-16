/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                     │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2026 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    using PivotLocation = UnityEngine.ProBuilder.PivotLocation;
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;
    using ShapeGenerator = UnityEngine.ProBuilder.ShapeGenerator;
    using ShapeType = UnityEngine.ProBuilder.ShapeType;

    /// <summary>
    /// A fresh scene with one ProBuilder cube ("Cube": 1 × 1 × 1, 6 faces, 24 vertices) per test. Assets a test creates go to
    /// <see cref="Folder"/>, which is deleted afterwards.
    /// </summary>
    public abstract class ProBuilderTestBase
    {
        protected const string Folder = "Assets/RqlProBuilderTests";
        protected const string Meshes = "probuilder | get meshes";
        protected const string M = Meshes + " | where gameObject.name == \"Cube\"";

        protected ProBuilderMesh Cube = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            Cube = CreateCube("Cube");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static ProBuilderMesh CreateCube(string name, Vector3? position = null)
        {
            var mesh = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            mesh.gameObject.name = name;
            if (position.HasValue) mesh.transform.position = position.Value;
            return mesh;
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult Mut(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A query or mutation that must succeed.</summary>
        protected static QueryResult Ok(QueryResult r)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r;
        }

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql) => Ok(Mut(rql));

        /// <summary>A mutation that must fail with a message containing <paramref name="expected"/>.</summary>
        protected static void Fails(string rql, string expected)
        {
            var r = Mut(rql);
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        protected static string Cell(QueryResult r, int column = 0, int row = 0)
        {
            Assert.IsTrue(r.Ok, Table(r));
            return r.Rows![row][column]!.ToJsonString().Trim('"');
        }

        /// <summary>The <c>result</c> column of a call receipt.</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static int[] Facing(ProBuilderMesh mesh, Vector3 axis) =>
            Enumerable.Range(0, mesh.faces.Count)
                .Where(i =>
                {
                    var f = mesh.faces[i].indexes;
                    var p = mesh.positions;
                    var n = Vector3.Cross(p[f[1]] - p[f[0]], p[f[2]] - p[f[0]]).normalized;
                    return Vector3.Dot(n, axis) > 0.7f;
                })
                .ToArray();

        protected static string Ints(params int[] values) => "[" + string.Join(", ", values) + "]";

        protected static int SharedVertexCount(ProBuilderMesh mesh) => mesh.GetComponent<MeshFilter>().sharedMesh.vertexCount;

        protected static Material MaterialAsset(string name)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlProBuilderTests");
            var material = new Material(Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, $"{Folder}/{name}.mat");
            return material;
        }
    }
}
