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

    public class UnityEngine_Terrain_AdapterTests : TerrainTestBase
    {
        private float H(int x, int y) => Data.GetHeights(x, y, 1, 1)[0, 0];

        // ------------------------------------------------------------ heights

        [Test]
        public void SetHeightsFillsTheWholeHeightmap()
        {
            Assert.AreEqual((33 * 33).ToString(), Result(Ok(T + " | call SetHeights(0.25)")));
            Assert.AreEqual(0.25f, H(0, 0), 1e-3f);
            Assert.AreEqual(0.25f, H(32, 32), 1e-3f);
        }

        [Test]
        public void SetHeightsFillsARegion()
        {
            Assert.AreEqual("16", Result(Ok(T + " | call SetHeights(0.8, 10, 20, 4, 4)")));
            Assert.AreEqual(0.8f, H(10, 20), 1e-3f);
            Assert.AreEqual(0.8f, H(13, 23), 1e-3f);
            Assert.AreEqual(0f, H(14, 20), 1e-3f);
            Assert.AreEqual(0f, H(10, 24), 1e-3f);
        }

        [Test]
        public void SetHeightsRejectsBadInputWithoutWriting()
        {
            Fails(T + " | call SetHeights(1.5)", "normalized");
            Fails(T + " | call SetHeights(0.5, 30, 30, 8, 8)", "leaves the heightmap");
            Fails(T + " | call SetHeights(0.5, 40, 0)", "outside the heightmap");
            Assert.AreEqual(0f, H(30, 30), 1e-6f);
        }

        [Test]
        public void SetHeightsIsUndoable()
        {
            Ok(T + " | call SetHeights(0.5)");
            Assert.AreEqual(0.5f, H(5, 5), 1e-3f);
            Undo.PerformUndo();
            Assert.AreEqual(0f, H(5, 5), 1e-3f);
        }

        [Test]
        public void SetHeightGridWritesRows()
        {
            Assert.AreEqual("6", Result(Ok(T + " | call SetHeightGrid([[0.1, 0.2, 0.3], [0.4, 0.5, 0.6]], 5, 7)")));
            Assert.AreEqual(0.1f, H(5, 7), 1e-3f);
            Assert.AreEqual(0.3f, H(7, 7), 1e-3f);
            Assert.AreEqual(0.4f, H(5, 8), 1e-3f);
            Assert.AreEqual(0.6f, H(7, 8), 1e-3f);
        }

        [Test]
        public void SetHeightGridRejectsRaggedOrOutOfRangeRows()
        {
            Fails(T + " | call SetHeightGrid([[0.1, 0.2], [0.3]])", "rectangular");
            Fails(T + " | call SetHeightGrid([[0.1, 2]])", "normalized");
            Fails(T + " | call SetHeightGrid([[0.1, 0.2]], 32, 0)", "leaves the heightmap");
        }

        // ------------------------------------------------------------ layers

        [Test]
        public void AddPaintAndRemoveLayers()
        {
            LayerAsset("Grass");
            LayerAsset("Rock");
            Assert.AreEqual("0", Result(Ok(T + $" | call AddLayer(unity.asset(\"{Folder}/Grass.terrainlayer\"))")));
            Assert.AreEqual("1", Result(Ok(T + $" | call AddLayer(unity.asset(\"{Folder}/Rock.terrainlayer\"))")));
            Assert.AreEqual(2, Data.terrainLayers.Length);
            Fails(T + $" | call AddLayer(unity.asset(\"{Folder}/Rock.terrainlayer\"))", "already on the terrain");

            Assert.AreEqual("16", Result(Ok(T + " | call PaintLayer(1, 1, 0, 0, 4, 4)")));
            var painted = Data.GetAlphamaps(0, 0, 1, 1);
            Assert.AreEqual(1f, painted[0, 0, 1], 1e-3f);
            Assert.AreEqual(0f, painted[0, 0, 0], 1e-3f);
            var untouched = Data.GetAlphamaps(8, 8, 1, 1);
            Assert.AreEqual(1f, untouched[0, 0, 0], 1e-3f);

            Ok(T + " | call PaintLayer(0, 0.25, 0, 0, 1, 1)");
            var mixed = Data.GetAlphamaps(0, 0, 1, 1);
            Assert.AreEqual(0.25f, mixed[0, 0, 0], 1e-3f);
            Assert.AreEqual(0.75f, mixed[0, 0, 1], 1e-3f);

            Assert.AreEqual("1", Result(Ok(T + " | call RemoveLayer(0)")));
            Assert.AreEqual("Rock", Data.terrainLayers[0].name);
            // The paint follows its layer: the cells painted with Rock keep it (the removed Grass channel does not take over).
            Assert.AreEqual(1f, Data.GetAlphamaps(2, 2, 1, 1)[0, 0, 0], 1e-3f);
        }

        [Test]
        public void PaintLayerValidatesTheLayerAndRegion()
        {
            Fails(T + " | call PaintLayer(0)", "AddLayer first");
            LayerAsset("Grass");
            Ok(T + $" | call AddLayer(unity.asset(\"{Folder}/Grass.terrainlayer\"))");
            Fails(T + " | call PaintLayer(3)", "does not exist");
            Fails(T + " | call PaintLayer(0, 2)", "strength");
            Fails(T + " | call PaintLayer(0, 1, 10, 10, 10, 10)", "leaves the alphamap");
            Fails(T + " | call RemoveLayer(5)", "does not exist");
        }

        // ------------------------------------------------------------ trees and details

        [Test]
        public void TreesArePlacedScatteredAndCleared()
        {
            Fails(T + " | call PlaceTrees(0, 5)", "AddTreePrototype first");
            PrefabAsset("Oak");
            Assert.AreEqual("0", Result(Ok(T + $" | call AddTreePrototype(unity.asset(\"{Folder}/Oak.prefab\"))")));

            Assert.AreEqual("10", Result(Ok(T + " | call PlaceTrees(0, 10, 0.2, 0.2, 0.4, 0.4)")));
            Assert.AreEqual(10, Data.treeInstanceCount);
            foreach (var tree in Data.treeInstances)
            {
                Assert.That(tree.position.x, Is.InRange(0.2f, 0.4f));
                Assert.That(tree.position.z, Is.InRange(0.2f, 0.4f));
            }

            Assert.AreEqual("2", Result(Ok(T + " | call PlaceTreesAt([\"(0.5, 0.5)\", \"(0.9, 0.1)\"], 0, 2)")));
            Assert.AreEqual(12, Data.treeInstanceCount);
            Assert.AreEqual(0.9f, Data.GetTreeInstance(11).position.x, 1e-4f);
            Assert.AreEqual(2f, Data.GetTreeInstance(11).heightScale, 1e-4f);

            Fails(T + " | call PlaceTreesAt([\"(1.5, 0.5)\"])", "normalized");
            Fails(T + " | call PlaceTrees(1, 5)", "does not exist");
            Fails(T + " | call PlaceTrees(0, 5, 0.5, 0, 0.2, 1)", "min <= max");

            Assert.AreEqual("12", Result(Ok(T + " | call ClearTrees()")));
            Assert.AreEqual(0, Data.treeInstanceCount);
        }

        [Test]
        public void PlacingTreesIsUndoable()
        {
            PrefabAsset("Oak");
            Ok(T + $" | call AddTreePrototype(unity.asset(\"{Folder}/Oak.prefab\"))");
            Ok(T + " | call PlaceTrees(0, 3)");
            Assert.AreEqual(3, Data.treeInstanceCount);
            Undo.PerformUndo();
            Assert.AreEqual(0, Data.treeInstanceCount);
        }

        [Test]
        public void DetailPrototypesFromATextureOrAPrefab()
        {
            TextureAsset("Blade");
            PrefabAsset("Bush");
            Assert.AreEqual("0", Result(Ok(T + $" | call AddDetailPrototype(unity.asset(\"{Folder}/Blade.asset\"))")));
            Assert.AreEqual("1", Result(Ok(T + $" | call AddDetailPrototype(unity.asset(\"{Folder}/Bush.prefab\"))")));
            Assert.AreEqual(DetailRenderMode.GrassBillboard, Data.detailPrototypes[0].renderMode);
            Assert.IsTrue(Data.detailPrototypes[1].usePrototypeMesh);
            Fails(T + " | call AddDetailPrototype(null)", "expected a Texture2D");
        }

        // ------------------------------------------------------------ neighbors

        [Test]
        public void SetNeighborsAcceptsTerrainsOrTheirGameObjects()
        {
            var east = Terrain.CreateTerrainGameObject(new TerrainData { heightmapResolution = 33, size = new Vector3(100f, 50f, 100f) });
            east.name = "East";
            east.transform.position = new Vector3(100f, 0f, 0f);
            var r = M(T + " | where gameObject.name == \"Ground\" | call SetNeighbors(null, null, ref(\"" + Cell(Q("objects | where name == \"East\" | select $ref")) + "\"), null)");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.AreEqual(east.GetComponent<Terrain>(), Ground.rightNeighbor);
            Assert.IsNull(Ground.leftNeighbor);
            Fails(T + " | where gameObject.name == \"Ground\" | call SetNeighbors($)", "own neighbor");
        }
    }
}
