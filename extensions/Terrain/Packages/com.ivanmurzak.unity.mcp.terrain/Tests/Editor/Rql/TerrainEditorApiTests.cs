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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    using Terrain = UnityEngine.Terrain;
    using TerrainData = UnityEngine.TerrainData;
    using TerrainLayer = UnityEngine.TerrainLayer;

    public class TerrainEditorApiTests : TerrainTestBase
    {
        private static GameObject? Find(string name) => GameObject.Find(name);

        [Test]
        public void CreateMakesATerrainWithATerrainDataAsset()
        {
            var r = Ok($"terrain | call Create(\"Island\", \"{Folder}/Data/Island.asset\", 65, \"(500, 120, 400)\", \"(10, 0, 20)\")");
            StringAssert.Contains("GameObject#", Result(r));
            var go = Find("Island");
            Assert.IsNotNull(go);
            var terrain = go!.GetComponent<Terrain>();
            Assert.IsNotNull(terrain);
            Assert.IsNotNull(go.GetComponent<TerrainCollider>());
            Assert.AreEqual(new Vector3(10f, 0f, 20f), go.transform.position);
            Assert.AreEqual(65, terrain.terrainData.heightmapResolution);
            Assert.AreEqual(new Vector3(500f, 120f, 400f), terrain.terrainData.size);
            Assert.AreEqual($"{Folder}/Data/Island.asset", AssetDatabase.GetAssetPath(terrain.terrainData));
        }

        [Test]
        public void CreateWithDefaultsAndUndo()
        {
            EnsureFolder();
            Ok("terrain | call Create(\"DefaultTerrain\")");
            var go = Find("DefaultTerrain");
            Assert.IsNotNull(go);
            var data = go!.GetComponent<Terrain>().terrainData;
            Assert.AreEqual(513, data.heightmapResolution);
            Assert.AreEqual(new Vector3(1000f, 600f, 1000f), data.size);
            var path = AssetDatabase.GetAssetPath(data);
            StringAssert.StartsWith("Assets/TerrainData_DefaultTerrain", path);
            try
            {
                Undo.PerformUndo();
                Assert.IsNull(Find("DefaultTerrain"));
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void CreateRejectsBadArguments()
        {
            Fails("terrain | call Create(\"Bad\", null, 300)", "nearest to 300: 257");
            Fails("terrain | call Create(\"Bad\", \"Data/Bad.asset\")", "must start with Assets/");
            Fails($"terrain | call Create(\"Bad\", \"{Folder}/Bad.txt\")", "must end with .asset");
            Fails("terrain | call Create(\"Bad\", null, 33, \"(0, 10, 10)\")", "size must be positive");
            Assert.IsNull(Find("Bad"));
        }

        [Test]
        public void CreateLayerMakesATerrainLayerAsset()
        {
            TextureAsset("Sand");
            var r = Ok($"terrain | call CreateLayer(\"{Folder}/Layers/Sand.terrainlayer\", unity.asset(\"{Folder}/Sand.asset\"), \"(8, 8)\")");
            StringAssert.Contains("TerrainLayer#", Result(r));
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{Folder}/Layers/Sand.terrainlayer");
            Assert.IsNotNull(layer);
            Assert.AreEqual(new Vector2(8f, 8f), layer.tileSize);
            Assert.AreEqual("Sand", layer.diffuseTexture.name);
            Fails($"terrain | call CreateLayer(\"{Folder}/Layers/Sand.terrainlayer\")", "already exists");

            Ok(T + $" | call AddLayer(unity.asset(\"{Folder}/Layers/Sand.terrainlayer\"))");
            Assert.AreEqual(layer, Data.terrainLayers[0]);
        }
    }
}
