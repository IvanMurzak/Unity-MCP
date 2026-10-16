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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class UnityEditor_Animations_AnimatorState_AdapterTests : AnimationTestBase
    {
        private const string S = P + " | get layers | get states";

        [Test]
        public void StateMembersAreReadable()
        {
            var r = Table(Ok(Q(S + " | select name speed motion.name writeDefaultValues")));
            StringAssert.Contains("Walk", r);
            StringAssert.Contains("Idle", r);
        }

        [Test]
        public void MotionAndSettingsAreWritable()
        {
            Ok(S + " | where name == \"Idle\" | set motion = " + W + ", speed = 0.5, mirror = true, cycleOffset = 0.1");
            var idle = StateNamed("Idle");
            Assert.AreEqual(Clip, idle.motion);
            Assert.AreEqual(0.5f, idle.speed, 1e-5f);
            Assert.IsTrue(idle.mirror);
        }

        [Test]
        public void SpeedParameter()
        {
            Ok(S + " | where name == \"Walk\" | set speedParameter = \"Speed\", speedParameterActive = true");
            Assert.AreEqual("Speed", StateNamed("Walk").speedParameter);
            Assert.IsTrue(StateNamed("Walk").speedParameterActive);
        }

        [Test]
        public void StateWritesAreUndoableAndDirty()
        {
            Undo.ClearAll();
            Ok(S + " | where name == \"Walk\" | set speed = 3");
            Assert.IsTrue(EditorUtility.IsDirty(StateNamed("Walk")));
            Undo.PerformUndo();
            Assert.AreEqual(1f, StateNamed("Walk").speed, 1e-5f);
        }
    }
}
