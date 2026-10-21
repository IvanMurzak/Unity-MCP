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
    using AnimatorState = UnityEditor.Animations.AnimatorState;

    /// <summary>
    /// AnimatorState: its settings (speed, motion, tag, mirror, writeDefaultValues …) are ordinary members recorded by the engine;
    /// the motion is an asset reference (not traversed); the transitions array is a copy, so element replacement is assigned back.
    /// </summary>
    public sealed class UnityEditor_Animations_AnimatorState_Adapter : RqlTypeAdapter<AnimatorState>
    {
        public override string Description =>
            "AnimatorState: name speed motion (AnimationClip / BlendTree) tag mirror cycleOffset writeDefaultValues iKOnFeet *Parameter / " +
            "*ParameterActive, transitions (AnimatorStateTransition: destinationState conditions hasExitTime exitTime duration …)";

        protected override void Members(MemberTable<AnimatorState> m) =>
            m.Reference("motion").CopyOnRead("transitions", "behaviours");
    }
}
