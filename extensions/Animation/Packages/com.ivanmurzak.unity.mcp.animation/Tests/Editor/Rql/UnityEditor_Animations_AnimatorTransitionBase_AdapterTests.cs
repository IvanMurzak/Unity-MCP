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
    using AnimatorConditionMode = UnityEditor.Animations.AnimatorConditionMode;
    using AnimatorStateTransition = UnityEditor.Animations.AnimatorStateTransition;

    public class UnityEditor_Animations_AnimatorTransitionBase_AdapterTests : AnimationTestBase
    {
        private const string T = P + " | get layers | get states | where name == \"Idle\" | get transitions";

        private AnimatorStateTransition Transition = null!;

        [SetUp]
        public void AddTransition()
        {
            Controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            Transition = StateNamed("Idle").AddTransition(StateNamed("Walk"));
        }

        [Test]
        public void TransitionMembersAreWritable()
        {
            Ok(T + " | set hasExitTime = true, exitTime = 0.8, duration = 0.2, hasFixedDuration = false, interruptionSource = \"Destination\"");
            Assert.IsTrue(Transition.hasExitTime);
            Assert.AreEqual(0.8f, Transition.exitTime, 1e-5f);
            Assert.AreEqual(0.2f, Transition.duration, 1e-5f);
            Assert.IsFalse(Transition.hasFixedDuration);
            Assert.AreEqual(UnityEditor.Animations.TransitionInterruptionSource.Destination, Transition.interruptionSource);
        }

        [Test]
        public void TransitionsAreReadable()
        {
            Transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            var r = Table(Ok(Q(T + " | select destinationState.name hasExitTime conditions")));
            StringAssert.Contains("Walk", r);
            StringAssert.Contains("Speed", r);
        }

        [Test]
        public void AddConditions()
        {
            Assert.AreEqual("1", Result(Ok(T + " | call AddCondition(\"Speed\", \"Less\", 0.5)")));
            Assert.AreEqual("2", Result(Ok(T + " | call AddCondition(\"Grounded\")")));
            Assert.AreEqual(AnimatorConditionMode.Less, Transition.conditions[0].mode);
            Assert.AreEqual(AnimatorConditionMode.If, Transition.conditions[1].mode);
            Fails(T + " | call AddCondition(\"Grounded\", \"Greater\")", "does not apply to Bool parameter");
            Fails(T + " | call AddCondition(\"Nope\")", "no parameter named 'Nope'");
        }

        [Test]
        public void ConditionElementsAreWritable()
        {
            Transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            Ok(T + " | set conditions[0].threshold = 0.75, conditions[0].mode = \"Less\"");
            Assert.AreEqual(0.75f, Transition.conditions[0].threshold, 1e-5f);
            Assert.AreEqual(AnimatorConditionMode.Less, Transition.conditions[0].mode);
        }

        [Test]
        public void RemoveAndClearConditions()
        {
            Transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            Transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            Assert.AreEqual("1", Result(Ok(T + " | call RemoveCondition(0)")));
            Assert.AreEqual("Grounded", Transition.conditions.Single().parameter);
            Fails(T + " | call RemoveCondition(3)", "condition index 3 is out of range");
            Assert.AreEqual("1", Result(Ok(T + " | call ClearConditions()")));
            Assert.AreEqual(0, Transition.conditions.Length);
        }

        [Test]
        public void AnyStateTransitionsToo()
        {
            var any = Base.AddAnyStateTransition(StateNamed("Walk"));
            Ok(P + " | get layers | get anyStateTransitions | call AddCondition(\"Grounded\", \"IfNot\")");
            Assert.AreEqual(AnimatorConditionMode.IfNot, any.conditions.Single().mode);
        }

        [Test]
        public void TransitionWritesAreUndoable()
        {
            Undo.ClearAll();
            Ok(T + " | set duration = 0.5");
            Undo.PerformUndo();
            Assert.AreNotEqual(0.5f, Transition.duration);
        }
    }
}
