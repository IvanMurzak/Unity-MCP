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

    public class UnityEditor_Animations_AnimatorController_AdapterTests : AnimationTestBase
    {
        [Test]
        public void AddParameterOfEveryType()
        {
            Assert.AreEqual("1", Result(Ok(P + " | call AddParameter(\"Grounded\", \"Bool\", true)")));
            Ok(P + " | call AddParameter(\"Lives\", \"Int\", 3)");
            Ok(P + " | call AddParameter(\"Gravity\", \"Float\", 9.5)");
            Ok(P + " | call AddParameter(\"Jump\", \"Trigger\")");
            var ps = Controller.parameters;
            CollectionAssert.AreEqual(new[] { "Speed", "Grounded", "Lives", "Gravity", "Jump" }, ps.Select(p => p.name).ToArray());
            Assert.IsTrue(ps[1].defaultBool);
            Assert.AreEqual(3, ps[2].defaultInt);
            Assert.AreEqual(9.5f, ps[3].defaultFloat, 1e-5f);
            Assert.AreEqual(AnimatorControllerParameterType.Trigger, ps[4].type);
        }

        [Test]
        public void AddParameterRejectsBadInput()
        {
            Fails(P + " | call AddParameter(\"Speed\", \"Float\")", "already has a parameter named 'Speed'");
            Fails(P + " | call AddParameter(\"X\", \"Vector\")", "Float");
            Fails(P + " | call AddParameter(\"X\", \"Bool\", 3)", "must be true or false");
            Fails(P + " | call AddParameter(\"X\", \"Int\", 1.5)", "must be an integer");
            Assert.AreEqual(1, Controller.parameters.Length);
        }

        [Test]
        public void RemoveParameterDropsItsConditions()
        {
            var idle = StateNamed("Idle");
            var t = idle.AddTransition(StateNamed("Walk"));
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Assert.AreEqual("0", Result(Ok(P + " | call RemoveParameter(\"Speed\")")));
            Assert.AreEqual(0, t.conditions.Length);
            Fails(P + " | call RemoveParameter(\"Speed\")", "no parameter named 'Speed'");
        }

        [Test]
        public void ParametersAreUndoable()
        {
            Undo.ClearAll();
            Ok(P + " | call AddParameter(\"Jump\", \"Trigger\")");
            Assert.AreEqual(2, Controller.parameters.Length);
            Undo.PerformUndo();
            Assert.AreEqual(1, Controller.parameters.Length);
        }

        [Test]
        public void AddAndRemoveLayers()
        {
            Assert.AreEqual("1", Result(Ok(P + " | call AddLayer(\"Upper Body\")")));
            Assert.AreEqual("Upper Body", Controller.layers[1].name);
            Assert.IsNotNull(Controller.layers[1].stateMachine);
            Fails(P + " | call AddLayer(\"Upper Body\")", "already has a layer");
            Assert.AreEqual("1", Result(Ok(P + " | call RemoveLayer(\"Upper Body\")")));
            Assert.AreEqual(1, Controller.layers.Length);
            Fails(P + " | call RemoveLayer(0)", "at least one layer");
            Fails(P + " | call RemoveLayer(\"Nope\")", "no layer named 'Nope'");
        }

        [Test]
        public void AddStateWithMotionOnALayer()
        {
            Ok(P + " | call AddLayer(\"Arms\")");
            Ok(P + " | call AddState(\"Wave\", " + W + ", \"Arms\")");
            Ok(P + " | call AddState(\"Point\", null, 1)");
            var sm = Controller.layers[1].stateMachine;
            Assert.AreEqual(2, sm.states.Length);
            Assert.AreEqual("Wave", sm.defaultState.name);
            Assert.AreEqual(Clip, sm.states.First(s => s.state.name == "Wave").state.motion);
            Assert.AreEqual(Controller, AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(sm.defaultState)));
        }

        [Test]
        public void AddStateReturnsTheState()
        {
            StringAssert.Contains("AnimatorState#", Result(Ok(P + " | call AddState(\"Run\")")));
            Fails(P + " | call AddState(\"Run\")", "already has a state named 'Run'");
            Fails(P + " | call AddState(\"Jog\", null, 3)", "layer index 3 is out of range");
        }

        [Test]
        public void RemoveState()
        {
            StateNamed("Idle").AddTransition(StateNamed("Walk"));
            Assert.AreEqual("1", Result(Ok(P + " | call RemoveState(\"Walk\")")));
            Assert.AreEqual(0, StateNamed("Idle").transitions.Length);
            Fails(P + " | call RemoveState(\"Walk\")", "no state named 'Walk'");
        }

        [Test]
        public void SetDefaultState()
        {
            Ok(P + " | call SetDefaultState(\"Walk\")");
            Assert.AreEqual("Walk", Base.defaultState.name);
        }

        [Test]
        public void StatesAreUndoable()
        {
            Undo.ClearAll();
            Ok(P + " | call AddState(\"Run\")");
            Assert.AreEqual(3, Base.states.Length);
            Undo.PerformUndo();
            Assert.AreEqual(2, Base.states.Length);
        }

        [Test]
        public void AddTransitionWithACondition()
        {
            Ok(P + " | call AddTransition(\"Idle\", \"Walk\", \"Speed\", \"Greater\", 0.1)");
            var t = StateNamed("Idle").transitions.Single();
            Assert.AreEqual("Walk", t.destinationState.name);
            Assert.IsFalse(t.hasExitTime);
            Assert.AreEqual(0f, t.duration);
            var c = t.conditions.Single();
            Assert.AreEqual(("Speed", AnimatorConditionMode.Greater, 0.1f), (c.parameter, c.mode, c.threshold));
        }

        [Test]
        public void AddTransitionDefaultsTheModeByParameterType()
        {
            Ok(P + " | call AddParameter(\"Jump\", \"Trigger\")");
            Ok(P + " | call AddTransition(\"Idle\", \"Walk\", \"Jump\")");
            Ok(P + " | call AddTransition(\"Walk\", \"Idle\", \"Speed\")");
            Assert.AreEqual(AnimatorConditionMode.If, StateNamed("Idle").transitions[0].conditions[0].mode);
            Assert.AreEqual(AnimatorConditionMode.Greater, StateNamed("Walk").transitions[0].conditions[0].mode);
        }

        [Test]
        public void AddTransitionRejectsBadConditions()
        {
            Fails(P + " | call AddTransition(\"Idle\", \"Walk\", \"Speed\", \"If\")", "does not apply to Float parameter");
            Fails(P + " | call AddTransition(\"Idle\", \"Walk\", \"Nope\")", "no parameter named 'Nope'");
            Fails(P + " | call AddTransition(\"Idle\", \"Run\")", "no state named 'Run'");
            Assert.AreEqual(0, StateNamed("Idle").transitions.Length);
        }

        [Test]
        public void ExitAndAnyStateTransitions()
        {
            Ok(P + " | call AddTransition(\"Walk\", \"Exit\")");
            Assert.IsTrue(StateNamed("Walk").transitions.Single().isExit);
            Ok(P + " | call AddAnyStateTransition(\"Idle\", \"Speed\", \"Less\", 0.05)");
            var any = Base.anyStateTransitions.Single();
            Assert.AreEqual("Idle", any.destinationState.name);
            Assert.AreEqual(AnimatorConditionMode.Less, any.conditions.Single().mode);
        }

        [Test]
        public void RemoveTransitions()
        {
            Ok(P + " | call AddTransition(\"Idle\", \"Walk\")");
            Ok(P + " | call AddTransition(\"Walk\", \"Exit\")");
            Ok(P + " | call AddAnyStateTransition(\"Walk\")");
            Assert.AreEqual("1", Result(Ok(P + " | call RemoveTransition(\"Idle\", \"Walk\")")));
            Assert.AreEqual("1", Result(Ok(P + " | call RemoveTransition(\"Walk\", \"Exit\")")));
            Assert.AreEqual("1", Result(Ok(P + " | call RemoveTransition(\"AnyState\", \"Walk\")")));
            Assert.AreEqual(0, StateNamed("Idle").transitions.Length + StateNamed("Walk").transitions.Length + Base.anyStateTransitions.Length);
            Fails(P + " | call RemoveTransition(\"Idle\", \"Walk\")", "no transition from 'Idle' to 'Walk'");
        }

        [Test]
        public void TransitionsAreUndoable()
        {
            Undo.ClearAll();
            Ok(P + " | call AddTransition(\"Idle\", \"Walk\", \"Speed\")");
            Undo.PerformUndo();
            Assert.AreEqual(0, StateNamed("Idle").transitions.Length);
        }
    }
}
