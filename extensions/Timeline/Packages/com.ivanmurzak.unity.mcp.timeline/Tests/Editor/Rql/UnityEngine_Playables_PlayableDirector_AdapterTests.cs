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
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Timelines
{
    public class UnityEngine_Playables_PlayableDirector_AdapterTests : TimelineTestBase
    {
        private PlayableDirector Director = null!;
        private AnimationTrack HeroTrack = null!;
        private ActivationTrack DoorTrack = null!;
        private GameObject Hero = null!;
        private GameObject Door = null!;

        [SetUp]
        public void CreateScene()
        {
            HeroTrack = AddTrack<AnimationTrack>("Hero");
            DoorTrack = AddTrack<ActivationTrack>("Door");
            Director = new GameObject("Cutscene").AddComponent<PlayableDirector>();
            Hero = new GameObject("HeroModel");
            Hero.AddComponent<Animator>();
            Door = new GameObject("DoorModel");
        }

        private static string Ref(Object o) => $"ref(\"{com.IvanMurzak.Unity.MCP.Editor.Rql.RqlUnity.Engine.RefOf(o)}\")";

        [Test]
        public void TheDirectorPlaysATimelineAssignedByRql()
        {
            Ok(D + $" | set playableAsset = {A}, playOnAwake = false");
            Assert.AreSame(Timeline, Director.playableAsset);
            Assert.IsFalse(Director.playOnAwake);
        }

        [Test]
        public void BindingsNarrowAGameObjectToTheTrackComponent()
        {
            Director.playableAsset = Timeline;
            Ok(D + $" | set bindings[\"Hero\"] = {Ref(Hero)}, bindings[\"Door\"] = {Ref(Door)}");
            Assert.AreSame(Hero.GetComponent<Animator>(), Director.GetGenericBinding(HeroTrack));
            Assert.AreSame(Door, Director.GetGenericBinding(DoorTrack));
            var r = Read(D + " | select bindings[\"Hero\"].name");
            Assert.AreEqual("HeroModel", Cell(r));
            StringAssert.Contains("DoorModel", Table(Read(D + " | select gameObject.name playableAsset.name bindings")));
            Undo.PerformUndo();
            Assert.IsNull(Director.GetGenericBinding(HeroTrack));
        }

        [Test]
        public void ABindingCanBeCleared()
        {
            Director.playableAsset = Timeline;
            Director.SetGenericBinding(DoorTrack, Door);
            Ok(D + " | set bindings[\"Door\"] = null");
            Assert.IsNull(Director.GetGenericBinding(DoorTrack));
        }

        [Test]
        public void InvalidBindingsFailWithoutChanges()
        {
            Fails(D + $" | set bindings[\"Hero\"] = {Ref(Hero)}", "plays no TimelineAsset");
            Director.playableAsset = Timeline;
            Fails(D + $" | set bindings[\"Hero\"] = {Ref(Door)}", "has no Animator");
            Fails(D + $" | set bindings[\"Nope\"] = {Ref(Door)}", "no entry 'Nope'");
            Assert.IsNull(Director.GetGenericBinding(HeroTrack));
        }

        [Test]
        public void ThePlayableGraphIsHidden()
        {
            Director.playableAsset = Timeline;
            Read(D);
            StringAssert.DoesNotContain("playableGraph", Table(Read(D + " | select $")));
        }
    }
}
