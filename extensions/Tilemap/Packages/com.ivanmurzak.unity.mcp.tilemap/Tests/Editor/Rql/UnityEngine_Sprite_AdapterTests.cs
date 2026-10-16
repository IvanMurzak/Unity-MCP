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
    using Tile = UnityEngine.Tilemaps.Tile;

    public class UnityEngine_Sprite_AdapterTests : TilemapTestBase
    {
        private Tile _tile = null!;
        private string Q1 => $"unity.asset(\"{TilePath("Grass")}\")";

        [SetUp]
        public void CreateTile() => _tile = TileAsset("Grass");

        [Test]
        public void ASpriteIsWrittenFromItsTextureOrPath()
        {
            var png = SpriteTexture("leaf");
            Ok(Q1 + $" | set sprite = unity.asset(\"{png}\")");
            Assert.AreEqual("leaf", _tile.sprite.name);
            _tile.sprite = null;
            Ok(Q1 + $" | set sprite = \"{png}\"");
            Assert.AreEqual("leaf", _tile.sprite!.name);
        }

        [Test]
        public void ASheetSpriteIsPickedByName()
        {
            var png = SpriteTexture("sheet", sheet: true);
            Ok(Q1 + $" | set sprite = \"{png}#sheet_1\"");
            Assert.AreEqual("sheet_1", _tile.sprite.name);
            Fails(Q1 + $" | set sprite = \"{png}#sheet_9\"", "has no sprite 'sheet_9'");
        }

        [Test]
        public void ATextureThatIsNotASpriteFails()
        {
            var png = SpriteTexture("plain", asSprite: false);
            Fails(Q1 + $" | set sprite = unity.asset(\"{png}\")", "has no sprites");
            Fails(Q1 + $" | set sprite = \"{Folder}/missing.png\"", "no asset at");
            Assert.IsNull(_tile.sprite);
        }

        [Test]
        public void SpriteWritesAreUndoable()
        {
            var png = SpriteTexture("leaf");
            Ok(Q1 + $" | set sprite = \"{png}\"");
            Assert.IsNotNull(_tile.sprite);
            Undo.PerformUndo();
            Assert.IsNull(_tile.sprite);
        }
    }
}
