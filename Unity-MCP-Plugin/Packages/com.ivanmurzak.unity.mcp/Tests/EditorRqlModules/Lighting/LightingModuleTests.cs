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
using System.Linq;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary>Registration, schema and skill: nothing here touches a scene, so no per-test scene (seconds each on CI).</summary>
    public class LightingModuleRegistrationTests
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            CollectionAssert.Contains(RqlUnity.Engine.Modules.Select(m => m.Module).ToList(), typeof(LightingModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        /// <summary>
        /// The unity-rql skill pays one line for lighting: the source row pointing at rql-schema. Members, calls and adapter
        /// methods are read on demand.
        /// </summary>
        [Test]
        public void TheSkillListsTheSourceInOneLineAndAddsNoGuide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render().Replace("\r\n", "\n");
            var rows = skill.Split('\n').Where(l => l.Contains("`" + LightingModule.SourceName + "`")).ToList();
            Assert.AreEqual(1, rows.Count, string.Join("\n", rows));
            StringAssert.Contains("rql-schema \"lighting\"", rows[0]);
            StringAssert.DoesNotContain("## Lighting", skill);
            foreach (var member in new[] { "CreateSettings", "PlaceGrid", "BakeReflectionProbes" })
                StringAssert.DoesNotContain(member, skill);
        }

        [Test]
        public void TheSchemaListsTheSourceAndItsCalls()
        {
            StringAssert.Contains(LightingModule.SourceName, RqlUnitySchema.Render(RqlUnity.Engine, null));
            var api = RqlUnitySchema.Render(RqlUnity.Engine, LightingModule.SourceName);
            foreach (var member in new[] { "environment", "settings", "isBaking", "bakeProgress", "hasBakedData", "lightmapCount",
                                           "CreateSettings(", "UseSettings(", "Bake(", "CancelBake(", "ClearBakedData(", "BakeReflectionProbes(" })
                StringAssert.Contains(member, api);
            StringAssert.Contains("PlaceGrid(", RqlUnitySchema.Render(RqlUnity.Engine, "LightProbeGroup"));
            StringAssert.Contains("Bake(", RqlUnitySchema.Render(RqlUnity.Engine, "ReflectionProbe"));
        }
    }

    public class LightingModuleTests : LightingTestBase
    {
        [Test]
        public void ReadingEverythingWholeDoesNotFail()
        {
            CreateLight("Sun", LightType.Directional);
            EnsureTestFolder();
            AssertOk(M($"lighting | call CreateSettings(\"{TestFolder}/Read.lighting\")"));
            foreach (var q in new[] { "lighting", "lighting | get environment", "lighting | get settings" })
                AssertOk(Q(q));
        }

        /// <summary>
        /// A whole lighting setup through RQL, in a saved scene with one directional light "Sun": lights, environment, settings,
        /// a background bake that is cancelled, light probes and reflection probes.
        /// </summary>
        [Test]
        public void AWholeLightingSetupRunsThroughRql()
        {
            // The setup bakes; without a graphics device only the CPU lightmapper can (Bake refuses the GPU one).
            if (!HasGraphicsDevice && !CpuLightmapperSupported()) Assert.Ignore("the CPU lightmapper is not supported here and there is no graphics device");
            var sun = CreateLight("Sun", LightType.Directional);
            SaveScene();
            var settingsPath = TestFolder + "/Outdoor.lighting";
            var queries = new[]
            {
                "editor | call CreateGameObject(\"Lamp\")",
                "objects | where name == \"Lamp\" | call AddComponent(\"Light\")",
                "objects | get components | where $ is Light | where gameObject.name == \"Lamp\" | set type = \"Spot\", intensity = 2, range = 15, spotAngle = 60, color = \"#ffd8a8\", shadows = \"Soft\", lightmapBakeType = \"Baked\"",
                "lighting | get environment | select ambientMode ambientIntensity fog fogMode skybox sun",
                "lighting | get environment | set ambientMode = \"Trilight\", ambientSkyColor = \"#9ab8e0\", ambientEquatorColor = \"#6b7a8a\", ambientGroundColor = \"#3a3228\"",
                "lighting | get environment | set fog = true, fogMode = \"ExponentialSquared\", fogColor = \"#8090a0\", fogDensity = 0.02",
                "lighting | get environment | set sun = unity.find(\"Sun\"), reflectionIntensity = 0.8",
                $"lighting | call CreateSettings(\"{settingsPath}\")",
                $"lighting | call UseSettings(\"{settingsPath}\")",
                "lighting | get settings | set lightmapper = \"ProgressiveCPU\", lightmapResolution = 10, lightmapMaxSize = 512, ao = true",
                "lighting | call Bake()",
                "lighting | select isBaking bakeProgress hasBakedData lightmapCount",
                "lighting | call CancelBake()",
                "lighting | call ClearBakedData()",
                "editor | call CreateGameObject(\"Probes\")",
                "objects | where name == \"Probes\" | call AddComponent(\"LightProbeGroup\")",
                "objects | get components | where $ is LightProbeGroup | call PlaceGrid(\"(10, 4, 10)\", 2)",
                "editor | call CreateGameObject(\"Mirror\")",
                "objects | where name == \"Mirror\" | call AddComponent(\"ReflectionProbe\")",
                "objects | get components | where $ is ReflectionProbe | set mode = \"Baked\", size = \"(10, 5, 10)\", resolution = 16",
                "lighting | call BakeReflectionProbes()",
            };
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
                var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
                Assert.IsNotNull(settings);
                Assert.AreEqual(settings, Lightmapping.lightingSettings);
                Assert.AreEqual(10f, settings.lightmapResolution, 1e-4f);
                Assert.AreEqual(108, GameObject.Find("Probes").GetComponent<LightProbeGroup>().probePositions.Length);
                Assert.IsFalse(Lightmapping.isRunning, "CancelBake() must stop the bake started above");
            }
            finally
            {
                if (Lightmapping.isRunning) Lightmapping.Cancel();
            }
        }

        [Test]
        public void RqlMutateReachesTheModuleThroughTheMcpPipeline()
        {
            var response = CallThroughMcpPlugin("rql-mutate", "{\"query\": \"lighting | get environment | set fog = true, fogDensity = 0.04\"}");
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(0.04f, RenderSettings.fogDensity, 1e-5f);
        }
    }
}
