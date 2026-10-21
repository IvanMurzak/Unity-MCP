/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
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
using com.IvanMurzak.Unity.MCP.Editor.Rql.Timelines;
using NUnit.Framework;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    public class TimelineModuleTests : TimelineTestBase
    {
        [Test]
        public void TheModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(TimelineModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheGuideIsPartOfTheRqlSkill()
        {
            var skill = global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render();
            StringAssert.Contains("## " + TimelineModule.GuideTopic, skill);
            StringAssert.Contains("timeline | call Create(", skill);
            StringAssert.Contains("call AddTrack(", skill);
            StringAssert.Contains("call AddClip(", skill);
            StringAssert.Contains("set bindings[", skill);
        }

        [Test]
        public void TheSchemaListsTheSourceAndMethods()
        {
            var overview = RqlUnitySchema.Render(RqlUnity.Engine, null);
            StringAssert.Contains("timeline", overview);
            var timeline = RqlUnitySchema.Render(RqlUnity.Engine, "TimelineAsset");
            StringAssert.Contains("AddTrack(", timeline);
            StringAssert.Contains("AddMarker(", timeline);
            StringAssert.Contains("frameRate", timeline);
            StringAssert.DoesNotContain("m_Tracks", timeline);
            var track = RqlUnitySchema.Render(RqlUnity.Engine, "AnimationTrack");
            foreach (var member in new[] { "AddClip(", "AddMarker(", "Delete(", "clips", "markers", "children", "bindingType" })
                StringAssert.Contains(member, track);
            StringAssert.DoesNotContain("m_Clips", track);
        }

        [Test]
        public void ReadingTimelineObjectsWholeDoesNotFail()
        {
            var track = AddTrack<AnimationTrack>("Hero");
            track.CreateDefaultClip();
            Timeline.CreateMarkerTrack();
            Timeline.markerTrack.CreateMarker<SignalEmitter>(1.0);
            Read(A);
            Read(A + " | get tracks");
            Read(A + " | get tracks | get clips");
            Read(A + " | get markers");
            Read("unity.assets(\"t:TimelineAsset\", \"" + Folder + "\") | select name duration tracks.name");
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new Dictionary<string, JsonElement>
            {
                ["query"] = JsonDocument.Parse(JsonSerializer.Serialize(A + " | call AddTrack(\"Animation\", \"Hero\")")).RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(ResponseStatus.Success, response.Value!.Status, response.Value.ToString());
            Assert.AreEqual(1, Tracks.Length);
            Assert.IsInstanceOf<AnimationTrack>(Tracks[0]);
            Assert.AreEqual("Hero", Tracks[0].name);
        }
    }
}
