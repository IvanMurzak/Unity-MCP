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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Terrains
{
    public class TerrainModuleTests : TerrainTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(TerrainModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + TerrainModule.GuideTopic, skill);
            StringAssert.Contains("call SetHeights(", skill);
            StringAssert.Contains("terrain | call Create(", skill);
        }

        [Test]
        public void TheSchemaListsTheSourceFunctionsAndMethods()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("terrain", overview);
            StringAssert.Contains("terrain.heightStats", overview);
            var terrain = RqlUnitySchema.Render(RqlUnity.Engine, "Terrain");
            foreach (var method in new[] { "SetHeights(", "SetHeightGrid(", "PaintLayer(", "AddLayer(", "RemoveLayer(", "AddTreePrototype(",
                                           "AddDetailPrototype(", "PlaceTrees(", "PlaceTreesAt(", "ClearTrees(", "SetNeighbors(" })
                StringAssert.Contains(method, terrain);
        }

        [Test]
        public void ReadingATerrainWholeDoesNotFail()
        {
            var r = Q(T + " | select $ref gameObject.name terrainData");
            Assert.IsTrue(r.Ok, Table(r));
            var all = Q(T);
            Assert.IsTrue(all.Ok, Table(all));
            var data = Q(T + " | get terrainData");
            Assert.IsTrue(data.Ok, Table(data));
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse("\"" + T + " | call SetHeights(0.5)\"").RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.AreEqual(0.5f, Data.GetHeights(16, 16, 1, 1)[0, 0], 1e-3f);
            Assert.AreEqual(25f, Ground.SampleHeight(new Vector3(50f, 0f, 50f)), 0.1f);
        }
    }
}
