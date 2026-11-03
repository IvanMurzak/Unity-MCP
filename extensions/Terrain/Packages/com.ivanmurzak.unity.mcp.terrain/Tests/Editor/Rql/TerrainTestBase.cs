/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Terrain)        │
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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;
    using TerrainLayer = UnityEngine.TerrainLayer;

    /// <summary>
    /// A fresh scene with one terrain ("Ground": 33 × 33 heightmap, 100 × 50 × 100 world units, in-memory TerrainData) per test.
    /// Assets a test creates go to <see cref="Folder"/>, which is deleted afterwards.
    /// </summary>
    public abstract class TerrainTestBase
    {
        protected const string Folder = "Assets/RqlTerrainTests";
        protected const string T = "objects | get components | where $ is Terrain";

        protected Terrain Ground = null!;
        protected TerrainData Data => Ground.terrainData;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RqlUnity.Reset();
            var data = new TerrainData { heightmapResolution = 33, alphamapResolution = 16, size = new Vector3(100f, 50f, 100f) };
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Ground";
            Ground = go.GetComponent<Terrain>();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        /// <summary>A mutation that must succeed.</summary>
        protected static QueryResult Ok(string rql)
        {
            var r = M(rql);
            Assert.IsTrue(r.Ok, Table(r));
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

        /// <summary>The <c>result</c> column of a call receipt.</summary>
        protected static string Result(QueryResult r) => Cell(r, 2);

        protected static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RqlTerrainTests");
        }

        protected static Texture2D TextureAsset(string name = "Grass")
        {
            EnsureFolder();
            var texture = new Texture2D(4, 4) { name = name };
            AssetDatabase.CreateAsset(texture, $"{Folder}/{name}.asset");
            return texture;
        }

        protected static TerrainLayer LayerAsset(string name)
        {
            EnsureFolder();
            var layer = new TerrainLayer { name = name, diffuseTexture = TextureAsset(name + "Texture") };
            AssetDatabase.CreateAsset(layer, $"{Folder}/{name}.terrainlayer");
            return layer;
        }

        protected static GameObject PrefabAsset(string name = "Tree")
        {
            EnsureFolder();
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(source);
            return prefab;
        }
    }
}
