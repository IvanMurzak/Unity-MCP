/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                     │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2026 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;
    using ProBuilderModule = global::com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder.ProBuilderModule;

    public class ProBuilderModuleTests : ProBuilderTestBase
    {
        private const string GuideFolder = "Assets/Materials";

        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(ProBuilderModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + ProBuilderModule.GuideTopic, skill);
            StringAssert.Contains("probuilder | call CreateShape(", skill);
            StringAssert.Contains("call Extrude(", skill);
            StringAssert.Contains("probuilder.facing(", skill);
        }

        [Test]
        public void TheSchemaListsTheSourceFunctionsAndMethods()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("probuilder", overview);
            foreach (var fn in new[] { "probuilder.facing", "probuilder.edges", "probuilder.openEdges", "probuilder.vertices" })
                StringAssert.Contains(fn, overview);
            var source = RqlUnitySchema.Render(RqlUnity.Engine, "ProBuilderEditorApi");
            foreach (var method in new[] { "CreateShape(", "CreatePolyShape(", "Merge(" })
                StringAssert.Contains(method, source);
            var mesh = RqlUnitySchema.Render(RqlUnity.Engine, "ProBuilderMesh");
            foreach (var method in new[] { "Extrude(", "DeleteFaces(", "SetFaceMaterial(", "FlipNormals(", "Bevel(", "Bridge(", "ConnectEdges(",
                                           "SubdivideEdges(", "SetPivot(", "SetPivotAt(" })
                StringAssert.Contains(method, mesh);
        }

        /// <summary>Every example query of the guide runs, in order, in a scene without the guide's objects and assets.</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(GuideFolder), GuideFolder + " exists in the test project: the guide test would change it");
            try
            {
                var queries = Regex.Matches(Guide(), "```\\n(.*?)```", RegexOptions.Singleline)
                    .Cast<Match>()
                    .SelectMany(b => b.Groups[1].Value.Split('\n'))
                    .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())
                    .Where(l => l.Length > 0)
                    .ToList();
                Assert.GreaterOrEqual(queries.Count, 24);
                foreach (var q in queries)
                {
                    var r = Mut(q);
                    TestContext.Out.WriteLine(q + "\n" + Table(r));
                    Assert.IsTrue(r.Ok, q + "\n" + Table(r));
                }

                ProBuilderMesh? Mesh(string name)
                {
                    var go = RqlUnity.AllGameObjects().FirstOrDefault(g => g.name == name);
                    return go != null ? go.GetComponent<ProBuilderMesh>() : null;
                }

                // Crate: extruded up (10 faces) then the forward faces individually, one face flipped; merged with Box.
                var crate = Mesh("Crate")!;
                Assert.IsNull(Mesh("Box"), "Merge deleted the source");
                var grass = AssetDatabase.LoadAssetAtPath<Material>(GuideFolder + "/Grass.mat");
                CollectionAssert.Contains(crate.GetComponent<MeshRenderer>().sharedMaterials, grass);
                Assert.Greater(crate.faceCount, 16);
                Assert.AreEqual(crate.vertexCount, SharedVertexCount(crate));

                var block = Mesh("Block")!;
                Assert.Greater(block.faceCount, 34);

                var floor = Mesh("Floor")!;
                Assert.AreEqual(new Vector3(-6f, 0f, 0f), floor.transform.position);
            }
            finally
            {
                if (AssetDatabase.IsValidFolder(GuideFolder)) AssetDatabase.DeleteAsset(GuideFolder);
            }
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var query = M + " | call Extrude(probuilder.facing($, \"Up\"), 1)";
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(query)).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.AreEqual(10, Cube.faceCount);
            Assert.AreEqual(1.5f, Cube.positions.Max(p => p.y), 1e-4f);
        }

        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## " + ProBuilderModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            var end = skill.IndexOf("\n## ", start + 3, StringComparison.Ordinal);
            return end < 0 ? skill.Substring(start) : skill.Substring(start, end - start);
        }
    }
}
