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
    public class UnityEditor_Animations_AnimatorStateMachine_AdapterTests : AnimationTestBase
    {
        private const string SM = P + " | get layers | get stateMachine";

        [Test]
        public void StateMachineIsReadable()
        {
            var r = Table(Ok(Q(SM + " | select name defaultState.name states.state.name anyStatePosition")));
            StringAssert.Contains("Idle", r);
            StringAssert.Contains("Walk", r);
        }

        [Test]
        public void ChildStatePositionsAreAssignedBack()
        {
            Ok(SM + " | set states[1].position = \"(300, 120, 0)\"");
            Assert.AreEqual(new Vector3(300, 120, 0), Base.states[1].position);
        }

        [Test]
        public void PositionWritesAreUndoable()
        {
            var before = Base.states[0].position;
            Undo.ClearAll();
            Ok(SM + " | set states[0].position = \"(10, 20, 0)\"");
            Undo.PerformUndo();
            Assert.AreEqual(before, Base.states[0].position);
        }
    }
}
