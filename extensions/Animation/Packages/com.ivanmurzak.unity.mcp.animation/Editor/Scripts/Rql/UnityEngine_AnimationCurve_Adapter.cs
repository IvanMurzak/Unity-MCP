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
using System.Collections.Generic;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// AnimationCurve fields of any component or asset: <c>keys</c> returns a copy, so element writes (<c>set curve.keys[1].value = 2</c>)
    /// are assigned back; a list of keyframes becomes a curve (<c>set curve = [[0, 0], [1, 1]]</c>). Clip curves are not AnimationCurve
    /// members — use AnimationClip.curves.
    /// </summary>
    public sealed class UnityEngine_AnimationCurve_Adapter : RqlTypeAdapter<AnimationCurve>
    {
        public override string Description =>
            "AnimationCurve: keys (Keyframe time value inTangent outTangent …), length, preWrapMode, postWrapMode; write a list of keyframes " +
            "[[time, value], …]";

        protected override void Members(MemberTable<AnimationCurve> m) => m.CopyOnRead("keys");

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is IList<object?> list)) return AdapterOutcome.NotHandled; // {keys: […], postWrapMode: …} is constructed by the kernel
            var keys = new Keyframe[list.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                if (!kernel.TryCoerce(list[i], typeof(Keyframe), out var k, out var why, $"[{i}]") || !(k is Keyframe key))
                {
                    reason = why;
                    return AdapterOutcome.Failed;
                }
                keys[i] = key;
            }
            result = new AnimationCurve(keys);
            return AdapterOutcome.Ok;
        }
    }
}
