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
    using Tile = UnityEngine.Tilemaps.Tile;

    public class TilemapFunctionsTests : TilemapTestBase
    {
        private Tile _grass = null!;
        private Tile _sand = null!;

        [SetUp]
        public void Paint()
        {
            _grass = TileAsset("Grass");
            _sand = TileAsset("Sand");
            Ground.SetTile(new Vector3Int(0, 0, 0), _grass);
            Ground.SetTile(new Vector3Int(1, 0, 0), _grass);
            Ground.SetTile(new Vector3Int(0, 1, 0), _sand);
        }

        [Test]
        public void TileAtReturnsTheAssetOrNull()
        {
            StringAssert.Contains("Grass", Cell(Read(T + " | select t=(tilemap.tileAt($, \"(1, 0, 0)\"))")));
            StringAssert.Contains("Sand", Cell(Read(T + " | select t=(tilemap.tileAt(gameObject, [0, 1]))")));
            Assert.AreEqual("null", Cell(Read(T + " | select t=(tilemap.tileAt($, \"(7, 7)\"))")));
        }

        [Test]
        public void CellDescribesOneCell()
        {
            Ground.SetColor(new Vector3Int(0, 0, 0), Color.white); // tiles lock their color by default: stays white
            var c = Cell(Read(T + " | select c=(tilemap.cell($, \"(0, 0, 0)\"))"));
            StringAssert.Contains("Grass", c);
            StringAssert.Contains(TilePath("Grass"), c);
            StringAssert.Contains("Sprite", c); // colliderType
            StringAssert.Contains("(0, 0, 0)", c);
            var empty = Cell(Read(T + " | select c=(tilemap.cell($, \"(5, 5, 0)\"))"));
            StringAssert.Contains("\"tile\":null", empty.Replace(" ", "").Replace("\\", ""));
        }

        [Test]
        public void TilesReturnsRowsTopFirst()
        {
            var rows = Cell(Read(T + " | select rows=(tilemap.tiles($, 0, 0, 1, 1))")).Replace("\\", "").Replace(" ", "");
            Assert.AreEqual("[[\"Sand\",null],[\"Grass\",\"Grass\"]]", rows);
            var r = Q(T + " | select rows=(tilemap.tiles($, 0, 0, 30, 30))");
            StringAssert.Contains("at most 20", Table(r));
        }

        [Test]
        public void CountAndCellsFilterByTile()
        {
            Assert.AreEqual("3", Cell(Read(T + " | select n=(tilemap.count($))")));
            Assert.AreEqual("2", Cell(Read(T + " | select n=(tilemap.count($, \"Grass\"))")));
            Assert.AreEqual("1", Cell(Read(T + $" | select n=(tilemap.count($, \"{TilePath("Sand")}\"))")));
            Assert.AreEqual("1", Cell(Read(T + " | select n=(tilemap.count($, null, 1, 0, 5, 5))")));
            var cells = Cell(Read(T + " | select c=(tilemap.cells($, \"Grass\"))"));
            StringAssert.Contains("(0, 0, 0)", cells);
            StringAssert.Contains("(1, 0, 0)", cells);
            StringAssert.DoesNotContain("(0, 1, 0)", cells);
        }

        [Test]
        public void WorldAndCellPositionsConvert()
        {
            World.cellSize = new Vector3(2f, 2f, 0f);
            Assert.AreEqual("(1, 0, 0)", Cell(Read(T + " | select c=(tilemap.worldToCell($, \"(2.5, 1, 0)\"))")));
            Assert.AreEqual("(3, 1, 0)", Cell(Read(T + " | select w=(tilemap.cellToWorld($, \"(1, 0, 0)\"))")));
        }

        [Test]
        public void BadArgumentsAreReported()
        {
            StringAssert.Contains("Tilemap or its GameObject", Table(Q(T + " | select t=(tilemap.tileAt(42, \"(0, 0)\"))")));
            StringAssert.Contains("whole numbers", Table(Q(T + " | select t=(tilemap.tileAt($, \"(0.5, 0)\"))")));
        }
    }
}
