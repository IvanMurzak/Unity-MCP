/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.YOUR_FEATURE_NAME
{
    using GameObject = UnityEngine.GameObject;
    using Light = UnityEngine.Light;
    using LightShadows = UnityEngine.LightShadows;
    using LightType = UnityEngine.LightType;

    public class YOUR_FEATURE_NAMEModuleTests : YOUR_FEATURE_NAMETestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(YOUR_FEATURE_NAMEModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var guide = Guide();
            StringAssert.Contains(YOUR_FEATURE_NAMEModule.SourceName + " | call CreateLight(", guide);
            StringAssert.Contains("call ScaleIntensity(", guide);
        }

        /// <summary>Every example query of the guide runs, in order, against the base scene (one light "Bulb").</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            var queries = Regex.Matches(Guide(), "```\\n(.*?)```", RegexOptions.Singleline)
                .Cast<Match>()
                .SelectMany(b => b.Groups[1].Value.Split('\n'))
                .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())   // drop the trailing comment (3+ spaces)
                .Where(l => l.Length > 0)
                .ToList();
            Assert.GreaterOrEqual(queries.Count, 5);
            foreach (var q in queries)
            {
                var r = M(q);
                Assert.IsTrue(r.Ok, q + "\n" + Table(r));
            }

            var lamp = GameObject.Find("Lamp");
            Assert.IsNotNull(lamp);
            var light = lamp!.GetComponent<Light>();
            Assert.AreEqual(LightType.Spot, light.type);
            Assert.AreEqual(1f, light.intensity, 1e-5f);   // set 2, then ScaleIntensity(0.5)
            Assert.AreEqual(LightShadows.Soft, light.shadows);
            Assert.AreEqual(1f, Bulb.intensity, 1e-5f, "the guide's where filter must leave other lights alone");
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var query = L + " | call ScaleIntensity(3)";
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(query)).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.AreEqual(3f, Bulb.intensity, 1e-5f);
        }

        /// <summary>This module's section of the rendered unity-rql skill.</summary>
        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## " + YOUR_FEATURE_NAMEModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            var end = skill.IndexOf("\n## ", start + 3, StringComparison.Ordinal);
            return end < 0 ? skill.Substring(start) : skill.Substring(start, end - start);
        }
    }
}
