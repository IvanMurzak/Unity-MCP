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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Tilemaps;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Tilemaps
{
    public class TilemapModuleTests : TilemapTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(TilemapModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + TilemapModule.GuideTopic, skill);
            StringAssert.Contains("call SetTile(", skill);
            StringAssert.Contains("call BoxFill(", skill);
            StringAssert.Contains("tilemap | call CreateTilemap(", skill);
            StringAssert.Contains("tilemap.tileAt(", skill);
        }

        [Test]
        public void TheSchemaListsTheSourceFunctionsAndMethods()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("tilemap", overview);
            foreach (var fn in new[] { "tilemap.tileAt", "tilemap.cell", "tilemap.tiles", "tilemap.count", "tilemap.cells", "tilemap.worldToCell", "tilemap.cellToWorld" })
                StringAssert.Contains(fn, overview);
            var tilemap = RqlUnitySchema.Render(RqlUnity.Engine, "Tilemap");
            foreach (var method in new[] { "SetTile(", "SetTiles(", "BoxFill(", "ClearBox(", "FloodFill(", "ClearAllTiles(", "SetColor(",
                                           "SetTransform(", "SetColliderType(", "CompressBounds(", "RefreshAllTiles(" })
                StringAssert.Contains(method, tilemap);
        }

        [Test]
        public void ReadingATilemapWholeDoesNotFail()
        {
            TileAsset();
            Ok(T + $" | call BoxFill(\"{TilePath("Grass")}\", 0, 0, 2, 2)");
            Assert.IsTrue(Q(T).Ok, Table(Q(T)));
            var r = Read(T + " | select $ref gameObject.name tileCount usedTiles cellBounds orientation");
            StringAssert.Contains("9", Cell(r, 2));
            Assert.IsTrue(Q("objects | get components | where $ is Grid").Ok);
            Assert.IsTrue(Q("objects | get components | where $ is TilemapRenderer").Ok);
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            var tile = TileAsset();
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var query = T + $" | call SetTile(\"(2, 3, 0)\", \"{TilePath("Grass")}\")";
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(query)).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.AreEqual(tile, Ground.GetTile(new Vector3Int(2, 3, 0)));
        }
    }
}
