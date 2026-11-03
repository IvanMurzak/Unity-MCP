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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    public class UnityEngine_Timeline_TimelineAsset_AdapterTests : TimelineTestBase
    {
        [Test]
        public void TracksAreTheRootTracksWithoutTheMarkerTrack()
        {
            AddTrack<AnimationTrack>("Hero");
            AddTrack<AudioTrack>("Music");
            Timeline.CreateMarkerTrack();
            var r = Read(A + " | get tracks | select name");
            Assert.AreEqual(2, r.Rows!.Count, Table(r));
            Assert.AreEqual("Hero", Cell(r, 0, 0));
            Assert.AreEqual("Music", Cell(r, 0, 1));
        }

        [Test]
        public void FrameRateIsWritableAndValidated()
        {
            Ok(A + " | set frameRate = 24");
            Assert.AreEqual(24.0, Timeline.editorSettings.frameRate, 1e-9);
            Assert.IsTrue(EditorUtility.IsDirty(Timeline));
            Fails(A + " | set frameRate = 0", "frameRate");
            Assert.AreEqual(24.0, Timeline.editorSettings.frameRate, 1e-9);
        }

        [Test]
        public void DurationModeAndFixedDurationAreWritable()
        {
            Ok(A + " | set durationMode = \"FixedLength\", fixedDuration = 8");
            Assert.AreEqual(TimelineAsset.DurationMode.FixedLength, Timeline.durationMode);
            Assert.AreEqual("8", Cell(Read(A + " | select duration")));
        }

        [Test]
        public void InternalListsAreNotMembers()
        {
            AddTrack<AnimationTrack>("Hero");
            Fails(A + " | set m_Tracks = []", "m_Tracks");
            Assert.AreEqual(1, Tracks.Length);
            Fails(A + " | set editorSettings.frameRate = 10", "editorSettings");
        }

        [Test]
        public void AddTrackByKindAndName()
        {
            var r = Ok(A + " | call AddTrack(\"Animation\", \"Hero\")");
            StringAssert.Contains("AnimationTrack#", Result(r));
            Ok(A + " | call AddTrack(\"Activation\")");
            Ok(A + " | call AddTrack(\"UnityEngine.Timeline.AudioTrack\", \"Music\")");
            Ok(A + " | call AddTrack(\"ControlTrack\", \"Fx\")");
            var tracks = Tracks;
            Assert.AreEqual(4, tracks.Length);
            Assert.IsInstanceOf<AnimationTrack>(tracks[0]);
            Assert.AreEqual("Hero", tracks[0].name);
            Assert.IsInstanceOf<ActivationTrack>(tracks[1]);
            Assert.IsInstanceOf<AudioTrack>(tracks[2]);
            Assert.IsInstanceOf<ControlTrack>(tracks[3]);
            Assert.AreEqual(TimelinePath, AssetDatabase.GetAssetPath(tracks[0]), "tracks are sub-assets of the .playable");
            Assert.IsTrue(EditorUtility.IsDirty(Timeline));
        }

        [Test]
        public void AddTrackIntoAGroup()
        {
            Ok(A + " | call AddTrack(\"Group\", \"Crowd\")");
            Ok(A + " | call AddTrack(\"Activation\", \"Door\", \"Crowd\")");
            var group = Tracks.Single();
            Assert.IsInstanceOf<GroupTrack>(group);
            var child = group.GetChildTracks().Single();
            Assert.AreEqual("Door", child.name);
            var r = Read(A + " | get tracks | recurse children | select $depth name");
            StringAssert.Contains("Door", Table(r));
            Fails(A + " | call AddTrack(\"Activation\", \"X\", \"Nope\")", "no track \"Nope\"");
        }

        [Test]
        public void AddTrackRejectsUnknownTypes()
        {
            Fails(A + " | call AddTrack(\"Banana\")", "unknown track type");
            Assert.AreEqual(0, Tracks.Length);
        }

        [Test]
        public void AddTrackIsUndoable()
        {
            Ok(A + " | call AddTrack(\"Animation\", \"Hero\")");
            Assert.AreEqual(1, Tracks.Length);
            Undo.PerformUndo();
            Assert.AreEqual(0, Tracks.Length);
        }

        [Test]
        public void AddMarkerCreatesTheMarkerTrack()
        {
            var r = Ok(A + " | call AddMarker(3.5)");
            StringAssert.Contains("SignalEmitter#", Result(r));
            Assert.IsNotNull(Timeline.markerTrack);
            var marker = Timeline.markerTrack.GetMarkers().Single();
            Assert.IsInstanceOf<SignalEmitter>(marker);
            Assert.AreEqual(3.5, marker.time, 1e-9);
            Assert.AreEqual("3.5", Cell(Read(A + " | get markers | select time")));
            Fails(A + " | call AddMarker(-1)", "must be >= 0");
            Fails(A + " | call AddMarker(1, \"Banana\")", "unknown marker type");
        }
    }
}
