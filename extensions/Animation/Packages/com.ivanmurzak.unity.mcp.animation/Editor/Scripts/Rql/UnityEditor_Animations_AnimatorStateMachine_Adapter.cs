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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorStateMachine = UnityEditor.Animations.AnimatorStateMachine;

    /// <summary>
    /// AnimatorStateMachine: states / stateMachines are arrays of ChildAnimatorState(Machine) structs returned as copies, so element
    /// writes (<c>set states[0].position = "(300, 100, 0)"</c>) are assigned back; the default state and parent links are references.
    /// </summary>
    public sealed class UnityEditor_Animations_AnimatorStateMachine_Adapter : RqlTypeAdapter<AnimatorStateMachine>
    {
        public override string Description =>
            "AnimatorStateMachine: states (state + position), stateMachines (sub state machines), defaultState, anyStateTransitions, " +
            "entryTransitions; prefer the controller methods AddState / AddTransition … and the layer views";

        protected override void Members(MemberTable<AnimatorStateMachine> m) =>
            m.CopyOnRead("states", "stateMachines", "anyStateTransitions", "entryTransitions", "behaviours")
             .Reference("defaultState");
    }
}
