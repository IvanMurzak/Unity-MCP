/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    public class CinemachineModuleTests : CinemachineTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(CinemachineModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + CinemachineModule.GuideTopic, skill);
            foreach (var text in new[] { "cinemachine | call CreateCamera(", "call SetBody(", "call SetAim(", "call SetNoise(", "call SetTargets(",
                         "set Lens.FieldOfView", "set Priority.Value", "cinemachine | call EnsureBrain()", "set DefaultBlend.Style" })
                StringAssert.Contains(text, skill);
        }

        [Test]
        public void TheSchemaListsTheSourceAndMethods()
        {
            StringAssert.Contains("cinemachine", RqlUnitySchema.Render(RqlUnity.Engine, null));
            var api = RqlUnitySchema.Render(RqlUnity.Engine, "cinemachine");
            foreach (var member in new[] { "cameras", "brains", "noiseProfiles", "bodyTypes", "aimTypes", "extensionTypes", "CreateCamera(", "EnsureBrain(" })
                StringAssert.Contains(member, api);
            var camera = RqlUnitySchema.Render(RqlUnity.Engine, "CinemachineCamera");
            foreach (var member in new[] { "SetBody(", "SetAim(", "SetNoise(", "SetTargets(", "body ", "aim ", "noise ", "extensions ", "Lens ", "Priority ", "Follow ", "LookAt " })
                StringAssert.Contains(member, camera);
        }

        /// <summary>Every example query of the guide runs, in order, against an empty scene (no Camera, no targets).</summary>
        [Test]
        public void EveryGuideExampleRuns()
        {
            var block = Regex.Match(Guide(), "```\\n(.*?)```", RegexOptions.Singleline).Groups[1].Value;
            var queries = block.Split('\n')
                .Select(l => Regex.Split(l, "\\s{3,}")[0].Trim())
                .Where(l => l.Length > 0)
                .ToList();
            Assert.GreaterOrEqual(queries.Count, 20);
            foreach (var q in queries)
            {
                var r = M(q);
                Assert.IsTrue(r.Ok, q + "\n" + Table(r));
            }

            var player = GameObject.Find("Player");
            var follow = GameObject.Find("FollowCam").GetComponent<CinemachineCamera>();
            Assert.AreEqual(player.transform, follow.Follow);
            Assert.AreEqual(20, follow.Priority.Value);
            Assert.AreEqual(50f, follow.Lens.FieldOfView, 1e-4f);
            Assert.AreEqual(5f, follow.Lens.Dutch, 1e-4f);
            var orbit = follow.GetComponent<CinemachineOrbitalFollow>();
            Assert.IsNotNull(orbit);
            Assert.AreEqual(6f, orbit.Radius, 1e-4f);
            Assert.IsNull(follow.GetComponent<CinemachineFollow>(), "SetBody replaced the Follow body");
            Assert.AreEqual(new Vector2(0.1f, 0f), follow.GetComponent<CinemachineRotationComposer>().Composition.ScreenPosition);
            var noise = follow.GetComponent<CinemachineBasicMultiChannelPerlin>();
            Assert.AreEqual("Handheld_normal_mild", noise.NoiseProfile.name);
            Assert.AreEqual(0.5f, noise.AmplitudeGain, 1e-4f);
            Assert.IsNotNull(follow.GetComponent<CinemachineDeoccluder>());

            var overview = GameObject.Find("Overview");
            Assert.IsNull(overview.GetComponent<CinemachineCamera>(), "the last example removes the component");
            Assert.IsNull(overview.GetComponent<CinemachineBasicMultiChannelPerlin>());

            var brain = Camera.main.GetComponent<CinemachineBrain>();
            Assert.AreEqual(CinemachineBlendDefinition.Styles.EaseInOut, brain.DefaultBlend.Style);
            Assert.AreEqual(1.5f, brain.DefaultBlend.Time, 1e-4f);
        }

        [Test]
        public void TheOldCinemachineToolsAreGone()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var tools = UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Select(t => t.Name).ToList();
            Assert.IsFalse(tools.Any(n => n.StartsWith("cinemachine-")), string.Join(", ", tools.Where(n => n.StartsWith("cinemachine-"))));
        }

        [Test]
        public void RqlMutateReachesTheExtensionThroughTheMcpPipeline()
        {
            CreateTarget("Hero");
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize("cinemachine | call CreateCamera(\"ViaMcp\", \"Hero\")")).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            var camera = GameObject.Find("ViaMcp").GetComponent<CinemachineCamera>();
            Assert.AreEqual("Hero", camera.Follow.name);
            Assert.IsNotNull(Camera.main.GetComponent<CinemachineBrain>());
        }

        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## " + CinemachineModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            return skill.Substring(start);
        }
    }
}
