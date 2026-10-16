/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;

    public class AnimationModuleTests : AnimationTestBase
    {
        private const string GuideFolder = "Assets/Animations";

        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(AnimationModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + AnimationModule.GuideTopic, skill);
            StringAssert.Contains("animation | call CreateClip(", skill);
            StringAssert.Contains("call SetCurve(", skill);
            StringAssert.Contains("call AddTransition(", skill);
        }

        [Test]
        public void TheSchemaListsTheSourceAndMethods()
        {
            StringAssert.Contains("animation", RqlUnitySchema.Render(RqlUnity.Engine, null));
            StringAssert.Contains("CreateController", RqlUnitySchema.Render(RqlUnity.Engine, "AnimationEditorApi"));
            var clip = RqlUnitySchema.Render(RqlUnity.Engine, "AnimationClip");
            foreach (var method in new[] { "SetCurve(", "SetObjectCurve(", "RemoveCurve(", "ClearCurves(", "AddEvent(", "RemoveEvent(", "ClearEvents(" })
                StringAssert.Contains(method, clip);
            var controller = RqlUnitySchema.Render(RqlUnity.Engine, "AnimatorController");
            foreach (var method in new[] { "AddParameter(", "RemoveParameter(", "AddLayer(", "RemoveLayer(", "AddState(", "RemoveState(",
                                           "SetDefaultState(", "AddTransition(", "AddAnyStateTransition(", "RemoveTransition(" })
                StringAssert.Contains(method, controller);
        }

        [Test]
        public void ReadingTheAssetsWholeDoesNotFail()
        {
            foreach (var q in new[] { W, P, P + " | get layers", P + " | get layers | get states", P + " | get layers | get stateMachine",
                                      W + " | get curves", W + " | get settings", P + " | get parameters" })
                Ok(Q(q));
        }

        /// <summary>Every example query of the guide runs, in order, in a project without the guide's assets.</summary>
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
                Assert.GreaterOrEqual(queries.Count, 40);
                foreach (var q in queries)
                {
                    var r = M(q);
                    Assert.IsTrue(r.Ok, q + "\n" + Table(r));
                }

                var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(GuideFolder + "/Walk.anim");
                Assert.AreEqual(30f, walk.frameRate);
                Assert.IsTrue(walk.isLooping);
                Assert.AreEqual(1, AnimationUtility.GetCurveBindings(walk).Length);
                Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(walk).Length);
                var y = AnimationUtility.GetEditorCurve(walk, AnimationUtility.GetCurveBindings(walk)[0]);
                Assert.AreEqual(4, y.length);
                Assert.AreEqual(2f, y.keys[1].value, 1e-5f);

                var player = AssetDatabase.LoadAssetAtPath<AnimatorController>(GuideFolder + "/Player.controller");
                Assert.AreEqual(1, player.layers.Length);
                CollectionAssert.AreEqual(new[] { "Speed", "Grounded" }, player.parameters.Select(p => p.name).ToArray());
                Assert.AreEqual(0.5f, player.parameters[0].defaultFloat, 1e-5f);
                var sm = player.layers[0].stateMachine;
                Assert.AreEqual("Idle", sm.defaultState.name);
                var idle = sm.states.First(s => s.state.name == "Idle").state;
                var walkState = sm.states.First(s => s.state.name == "Walk").state;
                Assert.AreEqual(1.5f, walkState.speed, 1e-5f);
                Assert.AreEqual(walk, walkState.motion);
                Assert.AreEqual(0, walkState.transitions.Length);
                Assert.AreEqual(1, idle.transitions.Length);
                Assert.IsTrue(idle.transitions[0].hasExitTime);
                Assert.AreEqual(2, idle.transitions[0].conditions.Length);
                Assert.AreEqual(0, sm.anyStateTransitions.Length);

                var animator = RqlUnity.AllGameObjects().First(go => go.name == "Player").GetComponent<Animator>();
                Assert.AreEqual(player, animator.runtimeAnimatorController);
                Assert.IsFalse(animator.applyRootMotion);
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
            var query = P + " | call AddParameter(\"Jump\", \"Trigger\")";
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(query)).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.IsTrue(Controller.parameters.Any(p => p.name == "Jump" && p.type == AnimatorControllerParameterType.Trigger));
        }

        private static string Guide()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            var start = skill.IndexOf("## " + AnimationModule.GuideTopic, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            var end = skill.IndexOf("\n## ", start + 3, StringComparison.Ordinal);
            return end < 0 ? skill.Substring(start) : skill.Substring(start, end - start);
        }
    }
}
