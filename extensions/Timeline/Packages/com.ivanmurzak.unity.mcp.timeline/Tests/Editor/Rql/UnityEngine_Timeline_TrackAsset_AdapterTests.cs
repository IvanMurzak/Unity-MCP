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
    public class UnityEngine_Timeline_TrackAsset_AdapterTests : TimelineTestBase
    {
        private const string Hero = A + " | get tracks | where name == \"Hero\"";
        private const string Door = A + " | get tracks | where name == \"Door\"";

        [Test]
        public void TrackMembersAreReadable()
        {
            var hero = AddTrack<AnimationTrack>("Hero");
            hero.CreateDefaultClip();
            AddTrack<ActivationTrack>("Door");
            var r = Read(A + " | get tracks | select name bindingType clips.count() markers.count()");
            Assert.AreEqual("Hero", Cell(r, 0, 0));
            Assert.AreEqual("Animator", Cell(r, 1, 0));
            Assert.AreEqual("1", Cell(r, 2, 0));
            Assert.AreEqual("GameObject", Cell(r, 1, 1));
        }

        [Test]
        public void MutedLockedAndNameAreWritableAndUndoable()
        {
            var hero = AddTrack<AnimationTrack>("Hero");
            Ok(Hero + " | set muted = true, locked = true");
            Assert.IsTrue(hero.muted);
            Assert.IsTrue(hero.locked);
            Assert.IsTrue(EditorUtility.IsDirty(hero));
            Undo.PerformUndo();
            Assert.IsFalse(hero.muted);
            Assert.IsFalse(hero.locked);
        }

        [Test]
        public void AddDefaultClip()
        {
            var door = AddTrack<ActivationTrack>("Door");
            var r = Ok(Door + " | call AddClip(null, 2, 1.5, \"Open\")");
            var clip = door.GetClips().Single();
            Assert.AreEqual(2.0, clip.start, 1e-9);
            Assert.AreEqual(1.5, clip.duration, 1e-9);
            Assert.AreEqual("Open", clip.displayName);
            Assert.AreEqual("ActivationPlayableAsset", clip.asset.GetType().Name);
            Assert.IsTrue(EditorUtility.IsDirty(Timeline));
            StringAssert.Contains("Open", Table(r));
        }

        [Test]
        public void AddAnimationClip()
        {
            var hero = AddTrack<AnimationTrack>("Hero");
            var walk = AnimationAsset("Walk", 2f);
            Ok(Hero + $" | call AddClip(unity.asset(\"{Folder}/Walk.anim\"), 1)");
            var clip = hero.GetClips().Single();
            Assert.AreSame(walk, clip.animationClip);
            Assert.AreEqual(1.0, clip.start, 1e-9);
            Assert.AreEqual(2.0, clip.duration, 1e-3, "duration 0 keeps the source length");
        }

        [Test]
        public void AddClipRejectsMismatchesAndClipLessTracks()
        {
            var door = AddTrack<ActivationTrack>("Door");
            AnimationAsset("Walk");
            Fails(Door + $" | call AddClip(unity.asset(\"{Folder}/Walk.anim\"))", "does not fit ActivationTrack");
            Fails(Door + " | call AddClip(null, -1)", "start -1 must be >= 0");
            Assert.AreEqual(0, door.GetClips().Count());
            AddTrack<GroupTrack>("Crowd");
            Fails(A + " | get tracks | where name == \"Crowd\" | call AddClip()", "has no clips");
        }

        [Test]
        public void AddMarkerOnASignalTrack()
        {
            var signals = AddTrack<SignalTrack>("Signals");
            var r = Ok(A + " | get tracks | where $type == \"SignalTrack\" | call AddMarker(1.25, \"Signal\")");
            StringAssert.Contains("SignalEmitter#", Result(r));
            Assert.AreEqual(1.25, signals.GetMarkers().Single().time, 1e-9);
            Assert.AreEqual("1", Cell(Read(A + " | get tracks | select markers.count()")));
        }

        [Test]
        public void DeleteRemovesTheTrackAndIsUndoable()
        {
            var group = AddTrack<GroupTrack>("Crowd");
            AddTrack<ActivationTrack>("Door", group).CreateDefaultClip();
            AddTrack<AnimationTrack>("Hero");
            Ok(A + " | get tracks | where name == \"Crowd\" | call Delete()");
            Assert.AreEqual(1, Tracks.Length);
            Assert.AreEqual("Hero", Tracks[0].name);
            Undo.PerformUndo();
            Assert.AreEqual(2, Tracks.Length);
            Assert.AreEqual("Door", Tracks[0].GetChildTracks().Single().name);
        }
    }
}
