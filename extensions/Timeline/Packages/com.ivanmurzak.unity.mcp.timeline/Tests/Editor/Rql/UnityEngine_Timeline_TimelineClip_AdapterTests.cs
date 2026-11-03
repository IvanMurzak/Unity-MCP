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
    using TimelineClip = UnityEngine.Timeline.TimelineClip;

    public class UnityEngine_Timeline_TimelineClip_AdapterTests : TimelineTestBase
    {
        private const string Clips = A + " | get tracks | get clips";

        private TimelineClip Walk = null!;
        private TimelineClip Door = null!;

        [SetUp]
        public void CreateClips()
        {
            var hero = AddTrack<AnimationTrack>("Hero");
            Walk = hero.CreateClip(AnimationAsset("Walk", 4f));
            Walk.displayName = "Walk";
            Walk.start = 1.0;
            Door = AddTrack<ActivationTrack>("Door").CreateDefaultClip();
            Door.displayName = "Open";
            EditorUtility.ClearDirty(Timeline);
        }

        [Test]
        public void ClipsAreReadable()
        {
            var r = Read(Clips + " | select track.name displayName start duration end asset.name");
            Assert.AreEqual(2, r.Rows!.Count, Table(r));
            Assert.AreEqual("Hero", Cell(r, 0, 0));
            Assert.AreEqual("Walk", Cell(r, 1, 0));
            Assert.AreEqual("1", Cell(r, 2, 0));
            Assert.AreEqual("5", Cell(r, 4, 0));
        }

        [Test]
        public void MovingAClipKeepsItsDurationIsDirtyAndUndoable()
        {
            Ok(Clips + " | where displayName == \"Walk\" | set start = start + 0.5");
            Assert.AreEqual(1.5, Walk.start, 1e-9);
            Assert.AreEqual(4.0, Walk.duration, 1e-3);
            Assert.IsTrue(EditorUtility.IsDirty(Walk.GetParentTrack()));
            Assert.IsTrue(EditorUtility.IsDirty(Timeline));
            Undo.PerformUndo();
            var restored = Timeline.GetRootTracks().First(t => t.name == "Hero").GetClips().Single();
            Assert.AreEqual(1.0, restored.start, 1e-9);
        }

        [Test]
        public void TimingMembersAreWritable()
        {
            Ok(Clips + " | where displayName == \"Walk\" | set duration = 3, clipIn = 0.25, timeScale = 1.5, easeInDuration = 0.5, easeOutDuration = 0.3");
            Assert.AreEqual(3.0, Walk.duration, 1e-9);
            Assert.AreEqual(0.25, Walk.clipIn, 1e-9);
            Assert.AreEqual(1.5, Walk.timeScale, 1e-9);
            Assert.AreEqual(0.5, Walk.easeInDuration, 1e-9);
            Assert.AreEqual(0.3, Walk.easeOutDuration, 1e-9);
        }

        [Test]
        public void InvalidTimingFailsWithoutChanges()
        {
            Fails(Clips + " | where displayName == \"Walk\" | set start = -1", "start -1 must be >= 0");
            Fails(Clips + " | where displayName == \"Walk\" | set duration = 0", "duration 0 must be > 0");
            Fails(Clips + " | where displayName == \"Walk\" | set start = 2, easeInDuration = 10", "longer than the clip");
            Fails(Clips + " | where displayName == \"Walk\" | set blendInDuration = 1", "blendInDuration");
            Fails(Clips + " | where displayName == \"Open\" | set timeScale = 2", "does not support a time scale");
            Assert.AreEqual(1.0, Walk.start, 1e-9);
            Assert.AreEqual(0.0, Walk.easeInDuration, 1e-9);
            Assert.AreEqual(1.0, Door.timeScale, 1e-9);
        }

        [Test]
        public void AnEaseLongerThanThePartOutsideTheBlendFailsInsteadOfBeingClamped()
        {
            Walk.blendOutDuration = 3.0; // a crossfade into a following clip leaves 1 s of the 4 s clip for the ease in
            Fails(Clips + " | where displayName == \"Walk\" | set easeInDuration = 2", "longer than the clip (1 s outside its 3 s blend)");
            Assert.AreEqual(0.0, Walk.easeInDuration, 1e-9);
            Ok(Clips + " | where displayName == \"Walk\" | set easeInDuration = 0.5");
            Assert.AreEqual(0.5, Walk.easeInDuration, 1e-9);
        }

        [Test]
        public void TheClipAssetIsReadOnlyButItsMembersAreWritable()
        {
            Fails(Clips + " | where displayName == \"Walk\" | set asset = null", "asset");
            Ok(Clips + " | where displayName == \"Walk\" | set asset.removeStartOffset = false");
            Assert.IsFalse(((AnimationPlayableAsset)Walk.asset).removeStartOffset);
        }

        [Test]
        public void DeleteRemovesTheClipAndIsUndoable()
        {
            Ok(Clips + " | where displayName == \"Open\" | call Delete()");
            var door = Timeline.GetRootTracks().First(t => t.name == "Door");
            Assert.AreEqual(0, door.GetClips().Count());
            Assert.IsTrue(EditorUtility.IsDirty(Timeline));
            Undo.PerformUndo();
            Assert.AreEqual(1, door.GetClips().Count());
        }
    }
}
