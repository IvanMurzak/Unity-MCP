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
using System;
using System.Linq;
using Rql.Execution;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorCondition = UnityEditor.Animations.AnimatorCondition;
    using AnimatorConditionMode = UnityEditor.Animations.AnimatorConditionMode;
    using AnimatorTransitionBase = UnityEditor.Animations.AnimatorTransitionBase;

    /// <summary>
    /// Every animator transition (state, Any State, entry, exit): conditions are AnimatorCondition structs returned as a copy, so
    /// element writes (<c>set conditions[0].threshold = 0.5</c>) are assigned back; destinations are references; AddCondition /
    /// RemoveCondition / ClearConditions are methods (the transition is the call owner, recorded by the engine).
    /// </summary>
    public sealed class UnityEditor_Animations_AnimatorTransitionBase_Adapter : RqlTypeAdapter<AnimatorTransitionBase>
    {
        public override string Description =>
            "Animator transition: destinationState destinationStateMachine isExit solo mute conditions (parameter mode threshold); state " +
            "transitions also hasExitTime exitTime duration hasFixedDuration offset interruptionSource; call AddCondition(parameter, mode, " +
            "threshold), RemoveCondition(index), ClearConditions()";

        protected override void Members(MemberTable<AnimatorTransitionBase> m)
        {
            m.CopyOnRead("conditions").Reference("destinationState", "destinationStateMachine");
            var t = m.Untyped;
            t.AddMethod("AddCondition",
                new[] { AnimationEdit.Req("parameter", typeof(string)), AnimationEdit.Opt("mode", typeof(AnimatorConditionMode?), null), AnimationEdit.Opt("threshold", typeof(float), 0f) },
                typeof(int), (o, a) => AddCondition((AnimatorTransitionBase)o, (string)a[0]!, (AnimatorConditionMode?)a[1], (float)a[2]!),
                "add a condition on a parameter of the transition's controller (mode If, IfNot, Greater, Less, Equals, NotEqual; default by parameter type); returns the condition count");
            t.AddMethod("RemoveCondition", new[] { AnimationEdit.Req("index", typeof(int)) }, typeof(int),
                (o, a) => RemoveCondition((AnimatorTransitionBase)o, (int)a[0]!), "remove the condition at index; returns the remaining condition count");
            t.AddMethod("ClearConditions", Array.Empty<CallableParameter>(), typeof(int),
                (o, a) => ClearConditions((AnimatorTransitionBase)o), "remove every condition; returns how many were removed");
        }

        internal static int AddCondition(AnimatorTransitionBase transition, string parameter, AnimatorConditionMode? mode, float threshold)
        {
            if (string.IsNullOrWhiteSpace(parameter)) throw new ArgumentException("parameter is empty");
            var controller = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(transition)) as UnityEditor.Animations.AnimatorController;
            var m = mode;
            if (controller != null)
            {
                var type = controller.parameters[AnimationEdit.ParameterIndex(controller, parameter)].type;
                m ??= AnimationEdit.DefaultMode(type);
                AnimationEdit.CheckCondition(type, m.Value, parameter);
            }
            transition.AddCondition(m ?? AnimatorConditionMode.If, threshold, parameter);
            return transition.conditions.Length;
        }

        internal static int RemoveCondition(AnimatorTransitionBase transition, int index)
        {
            var conditions = transition.conditions;
            AnimationEdit.CheckIndex(index, conditions.Length, "condition", "the transition");
            transition.conditions = conditions.Where((c, i) => i != index).ToArray();
            return conditions.Length - 1;
        }

        internal static int ClearConditions(AnimatorTransitionBase transition)
        {
            var count = transition.conditions.Length;
            transition.conditions = Array.Empty<AnimatorCondition>();
            return count;
        }
    }
}
