/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Tilemap)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{

    public class UnityEngine_Tilemaps_TileBase_AdapterTests : TilemapTestBase
    {
        [Test]
        public void ATileIsWrittenAsItsAssetPath()
        {
            var tile = TileAsset("Grass");
            Ok(T + $" | call SetTile(\"(0, 0, 0)\", \"{TilePath("Grass")}\")");
            Assert.AreEqual(tile, Ground.GetTile(Vector3Int.zero));
        }

        [Test]
        public void MissingOrMismatchedAssetsFail()
        {
            Fails(T + $" | call SetTile(\"(0, 0, 0)\", \"{Folder}/Nope.asset\")", "no asset at");
            var plain = TileAsset("Plain");
            Ok(T + $" | call SetTile(\"(0, 0, 0)\", \"{TilePath("Plain")}\")");
            // A tile path where a sprite is expected is rejected by the Sprite adapter, not taken for a tile.
            Fails($"unity.asset(\"{TilePath("Plain")}\") | set sprite = \"{TilePath("Plain")}\"", "has no sprites");
            Assert.AreEqual(plain, Ground.GetTile(Vector3Int.zero));
        }

        [Test]
        public void NonPathStringsAreNotTreatedAsTiles()
        {
            Fails(T + " | call SetTile(\"(0, 0, 0)\", \"Grass\")", "SetTile");
            Assert.IsNull(Ground.GetTile(Vector3Int.zero));
        }
    }
}
