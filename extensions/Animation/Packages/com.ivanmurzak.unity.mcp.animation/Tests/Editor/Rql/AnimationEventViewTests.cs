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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class AnimationEventViewTests : AnimationTestBase
    {
        private void TwoEvents()
        {
            AnimationUtility.SetAnimationEvents(Clip, new[]
            {
                new AnimationEvent { time = 0.2f, functionName = "Left" },
                new AnimationEvent { time = 0.7f, functionName = "Right", stringParameter = "r" },
            });
        }

        [Test]
        public void EventsAreReadable()
        {
            TwoEvents();
            var r = Table(Ok(Q(W + " | get events | select index time functionName stringParameter")));
            StringAssert.Contains("Left", r);
            StringAssert.Contains("Right", r);
            Assert.AreEqual(1L, Count(W + " | get events | where functionName == \"Right\" | count"));
        }

        [Test]
        public void EventMembersAreWritable()
        {
            TwoEvents();
            Ok(W + " | set events[1].functionName = \"Step\", events[1].floatParameter = 2, events[1].intParameter = 4, events[0].stringParameter = \"l\"");
            var events = AnimationUtility.GetAnimationEvents(Clip);
            Assert.AreEqual("Step", events[1].functionName);
            Assert.AreEqual(2f, events[1].floatParameter, 1e-5f);
            Assert.AreEqual(4, events[1].intParameter);
            Assert.AreEqual("l", events[0].stringParameter);
        }

        [Test]
        public void EventObjectParameter()
        {
            TwoEvents();
            Ok(W + " | set events[0].objectReferenceParameter = " + W);
            Assert.AreEqual(Clip, AnimationUtility.GetAnimationEvents(Clip)[0].objectReferenceParameter);
        }

        [Test]
        public void EveryEventAtOnce()
        {
            TwoEvents();
            Ok(W + " | get events | set time = time / 2");
            CollectionAssert.AreEqual(new[] { 0.1f, 0.35f }, AnimationUtility.GetAnimationEvents(Clip).Select(e => e.time).ToArray());
        }

        [Test]
        public void RemoveAnEvent()
        {
            TwoEvents();
            Ok(W + " | get events | where functionName == \"Left\" | call Remove()");
            Assert.AreEqual("Right", AnimationUtility.GetAnimationEvents(Clip).Single().functionName);
        }

        [Test]
        public void EventWritesAreUndoable()
        {
            TwoEvents();
            Undo.ClearAll();
            Ok(W + " | set events[0].time = 0.5");
            Undo.PerformUndo();
            Assert.AreEqual(0.2f, AnimationUtility.GetAnimationEvents(Clip)[0].time, 1e-5f);
        }
    }
}
