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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    public class UnityEngine_TerrainPrototype_AdapterTests : TerrainTestBase
    {
        [Test]
        public void MembersAreReadOnlyBecauseTerrainDataReturnsCopies()
        {
            var prefab = PrefabAsset("Oak");
            Data.treePrototypes = new[] { new TreePrototype { prefab = prefab, bendFactor = 0.1f } };
            Fails(T + " | set terrainData.treePrototypes[0].bendFactor = 0.3", "read-only");
            Assert.AreEqual(0.1f, Data.treePrototypes[0].bendFactor, 1e-5f);
            var read = double.Parse(Cell(Q(T + " | select terrainData.treePrototypes[0].bendFactor")), System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(0.1, read, 1e-6);
        }

        [Test]
        public void ATreePrototypeIsReplacedFromAnObjectLiteral()
        {
            var prefab = PrefabAsset("Oak");
            Data.treePrototypes = new[] { new TreePrototype { prefab = prefab } };
            Ok(T + $" | set terrainData.treePrototypes[0] = {{prefab: unity.asset(\"{Folder}/Oak.prefab\"), bendFactor: 0.3}}");
            Assert.AreEqual(0.3f, Data.treePrototypes[0].bendFactor, 1e-5f);
            Assert.AreEqual(prefab, Data.treePrototypes[0].prefab);
            Fails(T + " | set terrainData.treePrototypes[0] = {nope: 1}", "no writable member 'nope'");
            Fails(T + " | set terrainData.treePrototypes[0] = {bendFactor: \"x\"}", "bendFactor");
        }

        [Test]
        public void ADetailPrototypeIsReplacedFromAnObjectLiteral()
        {
            var texture = TextureAsset("Blade");
            Data.detailPrototypes = new[] { new DetailPrototype { prototypeTexture = texture, renderMode = DetailRenderMode.GrassBillboard } };
            Ok(T + $" | set terrainData.detailPrototypes[0] = {{prototypeTexture: unity.asset(\"{Folder}/Blade.asset\"), renderMode: \"GrassBillboard\", minHeight: 0.5, maxHeight: 2}}");
            Assert.AreEqual(0.5f, Data.detailPrototypes[0].minHeight, 1e-5f);
            Assert.AreEqual(2f, Data.detailPrototypes[0].maxHeight, 1e-5f);
            Assert.AreEqual(texture, Data.detailPrototypes[0].prototypeTexture);
        }
    }
}
