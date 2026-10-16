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
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    using TimelineAsset = UnityEngine.Timeline.TimelineAsset;

    public class TimelineEditorApiTests : TimelineTestBase
    {
        [Test]
        public void CreateMakesAnEmptyTimelineAsset()
        {
            var path = Folder + "/Sub/Outro.playable";
            var r = Ok($"timeline | call Create(\"{path}\", 30)");
            StringAssert.Contains("TimelineAsset#", Result(r));
            var created = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            Assert.IsNotNull(created);
            Assert.AreEqual(30.0, created.editorSettings.frameRate, 1e-9);
            Assert.AreEqual(TimelineAsset.DurationMode.BasedOnClips, created.durationMode);
            Assert.AreEqual(0, created.rootTrackCount);
        }

        [Test]
        public void CreateWithAFixedDuration()
        {
            var path = Folder + "/Fixed.playable";
            Ok($"timeline | call Create(\"{path}\", 60, 12.5)");
            var created = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            Assert.AreEqual(TimelineAsset.DurationMode.FixedLength, created.durationMode);
            Assert.AreEqual(12.5, created.fixedDuration, 1e-9);
            Assert.AreEqual(12.5, created.duration, 1e-9);
        }

        [Test]
        public void CreateRejectsBadPathsAndValues()
        {
            Fails("timeline | call Create(\"Timelines/Intro.playable\")", "must start with Assets/");
            Fails($"timeline | call Create(\"{Folder}/Intro.asset\")", "must end with .playable");
            Fails($"timeline | call Create(\"{TimelinePath}\")", "already exists");
            Fails($"timeline | call Create(\"{Folder}/Zero.playable\", 0)", "frameRate");
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(Folder + "/Zero.playable"));
        }

        [Test]
        public void CreateSignalMakesASignalAsset()
        {
            var path = Folder + "/Boom.signal";
            var r = Ok($"timeline | call CreateSignal(\"{path}\")");
            StringAssert.Contains("SignalAsset#", Result(r));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SignalAsset>(path));
        }
    }
}
