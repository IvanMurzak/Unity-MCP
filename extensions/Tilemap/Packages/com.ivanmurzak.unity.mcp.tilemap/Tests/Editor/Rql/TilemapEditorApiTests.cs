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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    using Tilemap = UnityEngine.Tilemaps.Tilemap;
    using TilemapRenderer = UnityEngine.Tilemaps.TilemapRenderer;
    using Tile = UnityEngine.Tilemaps.Tile;
    using RuleTile = UnityEngine.RuleTile;

    public class TilemapEditorApiTests : TilemapTestBase
    {
        [Test]
        public void CreateTilemapMakesAGridAndATilemap()
        {
            var r = Ok("tilemap | call CreateTilemap(\"Decor\", null, \"(2, 2, 0)\", \"Level\")");
            StringAssert.Contains("GameObject#", Result(r));
            var go = GameObject.Find("Decor");
            Assert.IsNotNull(go);
            Assert.IsNotNull(go!.GetComponent<Tilemap>());
            Assert.IsNotNull(go.GetComponent<TilemapRenderer>());
            var grid = go.transform.parent.GetComponent<Grid>();
            Assert.IsNotNull(grid);
            Assert.AreEqual("Level", grid.name);
            Assert.IsNull(grid.transform.parent);
            Assert.AreEqual(new Vector3(2f, 2f, 0f), grid.cellSize);
        }

        [Test]
        public void CreateTilemapUnderAGridAddsALayer()
        {
            Ok($"tilemap | call CreateTilemap(\"Walls\", ref(\"{Ref(World.gameObject)}\"))");
            var walls = GameObject.Find("Walls");
            Assert.IsNotNull(walls);
            Assert.AreEqual(World.transform, walls!.transform.parent);
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length);
        }

        [Test]
        public void CreateTilemapUnderAPlainParentNestsANewGrid()
        {
            var holder = new GameObject("Holder");
            Ok($"tilemap | call CreateTilemap(\"Sky\", ref(\"{Ref(holder)}\"))");
            var sky = GameObject.Find("Sky");
            Assert.IsNotNull(sky);
            Assert.AreEqual(holder.transform, sky!.transform.parent.parent);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), sky.transform.parent.GetComponent<Grid>().cellSize);
        }

        [Test]
        public void CreateTilemapIsUndoable()
        {
            Ok("tilemap | call CreateTilemap(\"Temp\", null, null, \"TempGrid\")");
            Assert.IsNotNull(GameObject.Find("TempGrid"));
            Undo.PerformUndo();
            Assert.IsNull(GameObject.Find("TempGrid"));
            Assert.IsNull(GameObject.Find("Temp"));
        }

        [Test]
        public void CreateTilemapRejectsBadArguments()
        {
            Fails("tilemap | call CreateTilemap(\"\")", "name is empty");
            Fails("tilemap | call CreateTilemap(\"Bad\", null, \"(0, 1, 0)\")", "cellSize must be positive");
            Assert.IsNull(GameObject.Find("Bad"));
        }

        [Test]
        public void CreateTileMakesATileAssetFromATexturePath()
        {
            var png = SpriteTexture("grass");
            var r = Ok($"tilemap | call CreateTile(\"{Folder}/Tiles/Grass.asset\", \"{png}\", \"#FF0000\", \"Grid\")");
            StringAssert.Contains("Tile#", Result(r));
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{Folder}/Tiles/Grass.asset");
            Assert.IsNotNull(tile);
            Assert.IsNotNull(tile.sprite);
            Assert.AreEqual("grass", tile.sprite.name);
            Assert.AreEqual(Color.red, tile.color);
            Assert.AreEqual(Tile.ColliderType.Grid, tile.colliderType);
        }

        [Test]
        public void CreateTileAcceptsTheTextureAssetAndDefaults()
        {
            var png = SpriteTexture("stone");
            Ok($"tilemap | call CreateTile(\"{Folder}/Stone.asset\", unity.asset(\"{png}\"))");
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{Folder}/Stone.asset");
            Assert.AreEqual("stone", tile.sprite.name);
            Assert.AreEqual(Color.white, tile.color);
            Assert.AreEqual(Tile.ColliderType.Sprite, tile.colliderType);

            Ok($"tilemap | call CreateTile(\"{Folder}/Empty.asset\")");
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Tile>($"{Folder}/Empty.asset").sprite);
        }

        [Test]
        public void CreateTileRejectsBadPaths()
        {
            Fails("tilemap | call CreateTile(\"Tiles/Bad.asset\")", "must start with Assets/");
            Fails($"tilemap | call CreateTile(\"{Folder}/Bad.tile\")", "must end with .asset");
            TileAsset("Taken");
            Fails($"tilemap | call CreateTile(\"{TilePath("Taken")}\")", "already exists");
            Fails($"tilemap | call CreateTile(\"{Folder}/NoSprite.asset\", \"{Folder}/missing.png\")", "no asset at");
        }

        [Test]
        public void CreateRuleTileMakesARuleTileAsset()
        {
            var png = SpriteTexture("ground", sheet: true);
            var r = Ok($"tilemap | call CreateRuleTile(\"{Folder}/Ground.asset\", \"{png}#ground_1\", \"None\")");
            StringAssert.Contains("RuleTile#", Result(r));
            var tile = AssetDatabase.LoadAssetAtPath<RuleTile>($"{Folder}/Ground.asset");
            Assert.IsNotNull(tile);
            Assert.AreEqual("ground_1", tile.m_DefaultSprite.name);
            Assert.AreEqual(Tile.ColliderType.None, tile.m_DefaultColliderType);
            Assert.AreEqual(0, tile.m_TilingRules.Count);
        }

        internal static string Ref(Object o) => RqlUnity.Engine.RefOf(o);
    }
}
