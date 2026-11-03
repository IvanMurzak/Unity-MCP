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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    public class UnityEngine_TerrainData_AdapterTests : TerrainTestBase
    {
        [Test]
        public void SizeAndResolutionsAreWritableMembers()
        {
            Ok(T + " | set terrainData.size = \"(200, 80, 300)\", terrainData.heightmapResolution = 65, terrainData.alphamapResolution = 64");
            Assert.AreEqual(new Vector3(200f, 80f, 300f), Data.size);
            Assert.AreEqual(65, Data.heightmapResolution);
            Assert.AreEqual(64, Data.alphamapResolution);
            Assert.AreEqual("65", Cell(Q(T + " | select terrainData.heightmapResolution")));
        }

        [Test]
        public void InvalidResolutionsAreRejectedWithTheRule()
        {
            Fails(T + " | set terrainData.heightmapResolution = 300", "nearest to 300: 257");
            Fails(T + " | set terrainData.alphamapResolution = 100", "power of two");
            Assert.AreEqual(33, Data.heightmapResolution);
            Assert.AreEqual(16, Data.alphamapResolution);
        }

        [Test]
        public void ArrayElementWritesAreAssignedBack()
        {
            var layer = LayerAsset("Grass");
            Data.terrainLayers = new[] { layer };
            Ok(T + " | set terrainData.terrainLayers[0].tileSize = \"(20, 25)\"");
            Assert.AreEqual(new Vector2(20f, 25f), layer.tileSize);
            Assert.AreEqual("Grass", Cell(Q(T + " | select terrainData.terrainLayers[0].name")));
        }

        [Test]
        public void TreeInstancesAreReadOnlyWhenNamedAndWritable()
        {
            var prefab = PrefabAsset("Oak");
            Data.treePrototypes = new[] { new TreePrototype { prefab = prefab } };
            Data.SetTreeInstances(new[] { new TreeInstance { position = new Vector3(0.5f, 0f, 0.5f), heightScale = 1f, widthScale = 1f, color = Color.white, lightmapColor = Color.white } }, true);

            StringAssert.DoesNotContain("treeInstances", Table(Q(T + " | get terrainData")));
            Assert.AreEqual("1", Cell(Q(T + " | select terrainData.treeInstanceCount")));
            Assert.AreEqual("1", Cell(Q(T + " | select n=(terrainData.treeInstances.count())")));
            Assert.AreEqual("(0.5, 0, 0.5)", Cell(Q(T + " | select terrainData.treeInstances[0].position")));

            Ok(T + " | set terrainData.treeInstances[0].heightScale = 3");
            Assert.AreEqual(3f, Data.GetTreeInstance(0).heightScale, 1e-5f);
        }

        [Test]
        public void SetDetailResolutionIsCallable()
        {
            Ok(T + " | get terrainData | call SetDetailResolution(256, 16)");
            Assert.AreEqual(256, Data.detailResolution);
            Assert.AreEqual(16, Data.detailResolutionPerPatch);
        }

        [Test]
        public void TheSchemaDescribesTheRules()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "TerrainData");
            StringAssert.Contains("heightmapResolution", schema);
            StringAssert.Contains("SetDetailResolution(", schema);
        }
    }
}
