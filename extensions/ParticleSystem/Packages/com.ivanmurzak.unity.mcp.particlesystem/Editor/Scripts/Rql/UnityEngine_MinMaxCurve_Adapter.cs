/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ParticleSystem) │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    /// <summary>MinMaxCurve: a number (constant) or [min, max] (random between two constants); curves are reported by mode.</summary>
    public sealed class UnityEngine_MinMaxCurve_Adapter : RqlTypeAdapter<ParticleSystem.MinMaxCurve>
    {
        public override string Description => "MinMaxCurve: write a number (constant) or [min, max]";

        protected override JsonNode? Encode(ParticleSystem.MinMaxCurve value, EncodeContext context) => value.mode switch
        {
            ParticleSystemCurveMode.Constant => context.EncodeValue(value.constant),
            ParticleSystemCurveMode.TwoConstants => new JsonArray(context.EncodeValue(value.constantMin), context.EncodeValue(value.constantMax)),
            _ => JsonValue.Create($"{value.mode} ×{value.curveMultiplier}"),
        };

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (value is IList<object?> list && list.Count == 2)
            {
                if (!kernel.TryCoerce(list[0], typeof(float), out var min, out reason, "[0]")) return AdapterOutcome.Failed;
                if (!kernel.TryCoerce(list[1], typeof(float), out var max, out reason, "[1]")) return AdapterOutcome.Failed;
                result = new ParticleSystem.MinMaxCurve((float)min!, (float)max!);
                return AdapterOutcome.Ok;
            }
            if (!(value is IList<object?>) && kernel.TryCoerce(value, typeof(float), out var constant, out _))
            {
                result = new ParticleSystem.MinMaxCurve((float)constant!);
                return AdapterOutcome.Ok;
            }
            reason = "expected a number or [min, max]";
            return AdapterOutcome.Failed;
        }

        // Comparisons accept what writes accept (`where main.startLifetime == 3`, `== [5, 25]`); curve modes defer.
        protected override bool? Equal(ParticleSystem.MinMaxCurve value, object? other, RqlKernel kernel)
        {
            if (!kernel.TryCoerce(other, typeof(ParticleSystem.MinMaxCurve), out var coerced, out _) || !(coerced is ParticleSystem.MinMaxCurve o)) return null;
            if (value.mode != o.mode) return false;
            return value.mode switch
            {
                ParticleSystemCurveMode.Constant => kernel.Equal(value.constant, o.constant),
                ParticleSystemCurveMode.TwoConstants => kernel.Equal(value.constantMin, o.constantMin) == true && kernel.Equal(value.constantMax, o.constantMax) == true,
                _ => null,
            };
        }
    }
}
