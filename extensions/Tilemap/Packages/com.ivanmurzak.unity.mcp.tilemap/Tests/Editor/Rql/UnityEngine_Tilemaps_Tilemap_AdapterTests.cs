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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using Tile = UnityEngine.Tilemaps.Tile;
    using TileFlags = UnityEngine.Tilemaps.TileFlags;

    public class UnityEngine_Tilemaps_Tilemap_AdapterTests : TilemapTestBase
    {
        private Tile _grass = null!;
        private string Grass => TilePath("Grass");

        [SetUp]
        public void CreateTile() => _grass = TileAsset("Grass");

        private static Vector3Int C(int x, int y, int z = 0) => new Vector3Int(x, y, z);

        // ------------------------------------------------------------ painting

        [Test]
        public void SetTilePaintsAndErasesACell()
        {
            Ok(T + $" | call SetTile(\"(1, 2, 0)\", \"{Grass}\")");
            Assert.AreEqual(_grass, Ground.GetTile(C(1, 2)));
            Ok(T + $" | call SetTile([3, 4, 0], unity.asset(\"{Grass}\"))");
            Assert.AreEqual(_grass, Ground.GetTile(C(3, 4)));
            Ok(T + " | call SetTile(\"(1, 2, 0)\", null)");
            Assert.IsNull(Ground.GetTile(C(1, 2)));
        }

        [Test]
        public void SetTileRejectsBadArgumentsWithoutWriting()
        {
            Fails(T + $" | call SetTile(\"(1, 2.5, 0)\", \"{Grass}\")", "whole numbers");
            Fails(T + $" | call SetTile(\"(1, 2, 0)\", \"{Folder}/Missing.asset\")", "no asset at");
            Assert.AreEqual(0, Ground.GetUsedTilesCount());
        }

        [Test]
        public void SetTilesPaintsAList()
        {
            Assert.AreEqual("3", Result(Ok(T + $" | call SetTiles([\"(0, 0, 0)\", \"(1, 0, 0)\", \"(5, 5, 0)\"], \"{Grass}\")")));
            Assert.AreEqual(_grass, Ground.GetTile(C(5, 5)));
            Assert.AreEqual("3", Cell(Read(T + " | select tileCount")));
        }

        [Test]
        public void BoxFillCoversBothCornersInAnyOrder()
        {
            Assert.AreEqual("12", Result(Ok(T + $" | call BoxFill(\"{Grass}\", 2, 1, -1, -1)")));
            Assert.AreEqual(_grass, Ground.GetTile(C(-1, -1)));
            Assert.AreEqual(_grass, Ground.GetTile(C(2, 1)));
            Assert.IsNull(Ground.GetTile(C(3, 1)));
            Assert.IsNull(Ground.GetTile(C(0, 2)));
            Assert.AreEqual("12", Cell(Read(T + " | select tileCount")));
        }

        [Test]
        public void BoxFillOnAnotherLayerAndTooLargeRegions()
        {
            Ok(T + $" | call BoxFill(\"{Grass}\", 0, 0, 1, 1, 3)");
            Assert.AreEqual(_grass, Ground.GetTile(C(1, 1, 3)));
            Assert.IsNull(Ground.GetTile(C(1, 1, 0)));
            Fails(T + $" | call BoxFill(\"{Grass}\", 0, 0, 2000, 2000)", "at most");
        }

        [Test]
        public void ClearBoxAndClearAllTilesReportWhatTheyRemoved()
        {
            Ok(T + $" | call BoxFill(\"{Grass}\", 0, 0, 3, 3)");
            Assert.AreEqual("4", Result(Ok(T + " | call ClearBox(0, 0, 1, 1)")));
            Assert.IsNull(Ground.GetTile(C(0, 0)));
            Assert.AreEqual(_grass, Ground.GetTile(C(2, 2)));
            Assert.AreEqual("5", Result(Ok(T + " | call ClearBox(1, 1, 5, 2)")));
            Assert.AreEqual("7", Result(Ok(T + " | call ClearAllTiles()")));
            Assert.AreEqual(0, Ground.GetUsedTilesCount());
        }

        [Test]
        public void FloodFillFillsAnEnclosedArea()
        {
            var water = TileAsset("Water");
            Ok(T + $" | call BoxFill(\"{Grass}\", 0, 0, 4, 4)");
            Ok(T + " | call ClearBox(1, 1, 3, 3)");
            Ok(T + $" | call FloodFill(\"(2, 2, 0)\", \"{TilePath("Water")}\")");
            Assert.AreEqual(water, Ground.GetTile(C(1, 1)));
            Assert.AreEqual(water, Ground.GetTile(C(3, 3)));
            Assert.AreEqual(_grass, Ground.GetTile(C(0, 0)));
            Fails(T + $" | call FloodFill(\"(50, 50, 0)\", \"{TilePath("Water")}\")", "outside the painted area");
        }

        [Test]
        public void SetColorTintsAPaintedCell()
        {
            Ok(T + $" | call SetTile(\"(0, 0, 0)\", \"{Grass}\")");
            Ok(T + " | call SetColor(\"(0, 0, 0)\", \"#FF0000\")");
            Assert.AreEqual(Color.red, Ground.GetColor(C(0, 0)));
            Assert.AreEqual(TileFlags.None, Ground.GetTileFlags(C(0, 0)) & TileFlags.LockColor);
            Fails(T + " | call SetColor(\"(9, 9, 0)\", \"#FF0000\")", "no tile at (9, 9, 0)");
        }

        [Test]
        public void SetTransformRotatesScalesAndFlips()
        {
            Ok(T + $" | call SetTile(\"(0, 0, 0)\", \"{Grass}\")");
            Ok(T + " | call SetTransform(\"(0, 0, 0)\", 90, \"(2, 2, 1)\", true)");
            var m = Ground.GetTransformMatrix(C(0, 0));
            var expected = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), new Vector3(-2, 2, 1));
            for (int i = 0; i < 16; i++) Assert.AreEqual(expected[i], m[i], 1e-4f);
            Fails(T + " | call SetTransform(\"(0, 0, 0)\", 0, \"(0, 1, 1)\")", "must not be 0");
            Fails(T + " | call SetTransform(\"(4, 4, 0)\", 45)", "no tile at");
        }

        [Test]
        public void SetColliderTypeChangesOneCell()
        {
            Ok(T + $" | call SetTile(\"(0, 0, 0)\", \"{Grass}\")");
            Ok(T + " | call SetColliderType(\"(0, 0, 0)\", \"Grid\")");
            Assert.AreEqual(Tile.ColliderType.Grid, Ground.GetColliderType(C(0, 0)));
            Fails(T + " | call SetColliderType(\"(0, 0, 0)\", \"Box\")", "None, Sprite, Grid");
        }

        [Test]
        public void PaintingIsUndoable()
        {
            Ok(T + $" | call BoxFill(\"{Grass}\", 0, 0, 2, 2)");
            Assert.AreEqual(_grass, Ground.GetTile(C(1, 1)));
            Undo.PerformUndo();
            Assert.IsNull(Ground.GetTile(C(1, 1)));
            Undo.PerformRedo();
            Assert.AreEqual(_grass, Ground.GetTile(C(1, 1)));
        }

        // ------------------------------------------------------------ members

        [Test]
        public void UsedTilesAndCellBoundsDescribeThePaintedArea()
        {
            TileAsset("Sand");
            Ok(T + $" | call BoxFill(\"{Grass}\", 0, 0, 3, 1)");
            Ok(T + $" | call SetTile(\"(5, 2, 0)\", \"{TilePath("Sand")}\")");
            Ok(T + " | call CompressBounds()");
            var used = Cell(Read(T + " | select usedTiles.name"));
            StringAssert.Contains("Grass", used);
            StringAssert.Contains("Sand", used);
            Assert.AreEqual("(0, 0, 0)", Cell(Read(T + " | select cellBounds.min")));
            Assert.AreEqual("(6, 3, 1)", Cell(Read(T + " | select cellBounds.max")));
            Assert.AreEqual("9", Cell(Read(T + " | select tileCount")));
            Assert.AreEqual("Ground", Cell(Read(T + " | where tileCount > 8 | select gameObject.name")));
        }

        [Test]
        public void SettingsAreOrdinaryMembers()
        {
            Ok(T + " | set orientation = \"XZ\", tileAnchor = \"(0, 0, 0)\", color = \"(1, 1, 1, 0.5)\"");
            Assert.AreEqual(Tilemap.Orientation.XZ, Ground.orientation);
            Assert.AreEqual(Vector3.zero, Ground.tileAnchor);
            Assert.AreEqual(0.5f, Ground.color.a, 1e-4f);
            Ok("objects | get components | where $ is Grid | set cellSize = \"(1, 0.5, 0)\", cellLayout = \"Isometric\"");
            Assert.AreEqual(GridLayout.CellLayout.Isometric, World.cellLayout);
            Assert.AreEqual(0.5f, World.cellSize.y, 1e-4f);
        }
    }
}
