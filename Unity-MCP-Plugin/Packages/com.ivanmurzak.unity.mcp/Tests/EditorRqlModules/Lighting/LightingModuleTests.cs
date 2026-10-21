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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    public class LightingModuleTests : LightingTestBase
    {
        // Assets the guide examples create outside the test folder.
        private const string GuideSettingsPath = "Assets/Lighting/Outdoor.lighting";

        [Test]
        public void TheModuleIsDiscovered()
        {
            CollectionAssert.Contains(RqlUnity.Engine.Modules.Select(m => m.Module).ToList(), typeof(LightingModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var guide = Guide();
            StringAssert.Contains(LightingModule.SourceName + " | get environment", guide);
            StringAssert.Contains(LightingModule.SourceName + " | call Bake()", guide);
            StringAssert.Contains("call PlaceGrid(", guide);
        }

        [Test]
        public void TheSchemaListsTheSourceAndItsCalls()
        {
            StringAssert.Contains(LightingModule.SourceName, RqlUnitySchema.Render(RqlUnity.Engine, null));
            var api = RqlUnitySchema.Render(RqlUnity.Engine, LightingModule.SourceName);
            foreach (var member in new[] { "environment", "settings", "stats", "isBaking", "CreateSettings(", "UseSettings(", "Bake(",
                                           "CancelBake(", "ClearBakedData(", "BakeReflectionProbes(" })
                StringAssert.Contains(member, api);
            StringAssert.Contains("PlaceGrid(", RqlUnitySchema.Render(RqlUnity.Engine, "LightProbeGroup"));
            StringAssert.Contains("Bake(", RqlUnitySchema.Render(RqlUnity.Engine, "ReflectionProbe"));
        }

        [Test]
        public void ReadingEverythingWholeDoesNotFail()
        {
            CreateLight("Sun", LightType.Directional);
            new GameObject("Probes").AddComponent<LightProbeGroup>();
            new GameObject("Mirror").AddComponent<ReflectionProbe>();
            EnsureTestFolder();
            Ok(M($"lighting | call CreateSettings(\"{TestFolder}/Read.lighting\")"));
            foreach (var q in new[] { "lighting", "lighting | get environment", "lighting | get settings", "lighting | select stats",
                                      "lighting | get lights", "lighting | get probeGroups", "lighting | get reflectionProbes" })
                Ok(Q(q));
        }

        /// <summary>Every example query of the guide runs, in order, in a saved scene that has one directional light, "Sun".</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            var sun = CreateLight("Sun", LightType.Directional);
            SaveScene();
            var queries = Regex.Matches(Guide(), "```\\n(.*?)```", RegexOptions.Singleline)
                .Cast<Match>()
                .SelectMany(b => b.Groups[1].Value.Split('\n'))
                .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())   // drop the trailing comment (3+ spaces)
                .Where(l => l.Length > 0)
                .ToList();
            Assert.GreaterOrEqual(queries.Count, 20);
            try
            {
                foreach (var q in queries)
                {
                    var r = M(q);
                    Assert.IsTrue(r.Ok, q + "\n" + Table(r));
                }

                var lamp = GameObject.Find("Lamp").GetComponent<Light>();
                Assert.AreEqual(LightType.Spot, lamp.type);
                Assert.AreEqual(LightmapBakeType.Baked, lamp.lightmapBakeType);
                Assert.AreEqual(LightShadows.Soft, lamp.shadows);
                Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Trilight, RenderSettings.ambientMode);
                Assert.IsTrue(RenderSettings.fog);
                Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode);
                Assert.AreEqual(sun, RenderSettings.sun);
                var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(GuideSettingsPath);
                Assert.IsNotNull(settings);
                Assert.AreEqual(settings, Lightmapping.lightingSettings);
                Assert.AreEqual(10f, settings.lightmapResolution, 1e-4f);
                Assert.AreEqual(108, GameObject.Find("Probes").GetComponent<LightProbeGroup>().probePositions.Length);
                Assert.IsFalse(Lightmapping.isRunning, "CancelBake() must stop the bake the guide started");
            }
            finally
            {
                if (Lightmapping.isRunning) Lightmapping.Cancel();
                AssetDatabase.DeleteAsset(GuideSettingsPath);
                if (AssetDatabase.IsValidFolder("Assets/Lighting") && AssetDatabase.FindAssets("", new[] { "Assets/Lighting" }).Length == 0)
                    AssetDatabase.DeleteAsset("Assets/Lighting");
            }
        }

        [Test]
        public void RqlMutateReachesTheModuleThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize("lighting | get environment | set fog = true, fogDensity = 0.04")).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(0.04f, RenderSettings.fogDensity, 1e-5f);
        }

        /// <summary>This module's section of the rendered unity-rql skill.</summary>
        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render().Replace("\r\n", "\n");
            var start = skill.IndexOf("## " + LightingModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "the Lighting guide is missing from the unity-rql skill");
            var end = skill.IndexOf("\n## ", start + 3, StringComparison.Ordinal);
            return end < 0 ? skill.Substring(start) : skill.Substring(start, end - start);
        }
    }
}
