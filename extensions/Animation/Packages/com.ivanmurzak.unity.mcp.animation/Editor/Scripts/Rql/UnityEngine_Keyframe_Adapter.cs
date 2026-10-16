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
using System.Linq;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// Keyframe literals for curves: <c>[time, value]</c>, <c>[time, value, inTangent, outTangent]</c> or <c>"(time, value)"</c>;
    /// object literals <c>{time: 0, value: 1, outTangent: 2}</c> use the default construction. Only the real key data is a
    /// member (the obsolete tangentMode is not).
    /// </summary>
    public sealed class UnityEngine_Keyframe_Adapter : RqlTypeAdapter<Keyframe>
    {
        public override string Description =>
            "Keyframe: time value inTangent outTangent inWeight outWeight weightedMode; write [time, value], [time, value, inTangent, outTangent] " +
            "or {time: 0, value: 1, …}";

        protected override void Members(MemberTable<Keyframe> m) =>
            m.Only("time", "value", "inTangent", "outTangent", "inWeight", "outWeight", "weightedMode");

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (value is ObjectValue) return AdapterOutcome.NotHandled; // {time, value, …}: the kernel constructs and assigns members
            IList<object?>? parts = value switch
            {
                IList<object?> list => list,
                string s when s.Trim().StartsWith("(") && s.Trim().EndsWith(")") =>
                    s.Trim().Trim('(', ')').Split(',').Select(p => (object?)p.Trim()).ToList(),
                _ => null,
            };
            if (parts == null || (parts.Count != 2 && parts.Count != 4))
            {
                reason = "expected a keyframe [time, value], [time, value, inTangent, outTangent] or {time: 0, value: 1, …}";
                return AdapterOutcome.Failed;
            }
            var numbers = new float[parts.Count];
            for (int i = 0; i < numbers.Length; i++)
            {
                var part = parts[i] is string text && float.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : parts[i];
                if (!kernel.TryCoerce(part, typeof(float), out var f, out _) || !(f is float x))
                {
                    reason = $"keyframe element {i} is not a number";
                    return AdapterOutcome.Failed;
                }
                numbers[i] = x;
            }
            result = numbers.Length == 2 ? new Keyframe(numbers[0], numbers[1]) : new Keyframe(numbers[0], numbers[1], numbers[2], numbers[3]);
            return AdapterOutcome.Ok;
        }
    }
}
