/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Navigation
{
    public class NavigationModuleTests : NavigationTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(NavigationModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## Navigation", skill);
            StringAssert.Contains("call Bake()", skill);
            StringAssert.Contains("navigation | get agentTypes", skill);
        }

        [Test]
        public void TheNavigationSourceIsInTheSchema()
        {
            StringAssert.Contains("navigation", RqlUnitySchema.Render(RqlUnity.Engine, null));
            var api = RqlUnitySchema.Render(RqlUnity.Engine, "navigation");
            StringAssert.Contains("agentTypes", api);
            StringAssert.Contains("BakeAll()", api);
        }

        [Test]
        public void TheOldNavigationToolsAreGone()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var tools = UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Select(t => t.Name).ToList();
            Assert.IsFalse(tools.Any(n => n.StartsWith("navigation-")), string.Join(", ", tools.Where(n => n.StartsWith("navigation-"))));
        }

        [Test]
        public void RqlMutateReachesTheExtensionThroughTheMcpPipeline()
        {
            var surface = CreateGround();
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse("\"objects | get components | where $ is NavMeshSurface | call Bake()\"").RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Message);
            Assert.IsNotNull(surface.navMeshData);
        }
    }
}
