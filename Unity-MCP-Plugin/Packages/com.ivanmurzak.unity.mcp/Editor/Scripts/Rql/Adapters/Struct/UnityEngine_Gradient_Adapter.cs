/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Gradient: written as a list of keys, [[color, time], …] or [{color: …, time: …}, …]; read as the same list. Colors are
    /// coerced and encoded by the kernel — whatever adapter handles Color — so this adapter never knows the Color format.
    /// The key's alpha becomes an alpha key at the same time.
    /// </summary>
    public sealed class UnityEngine_Gradient_Adapter : RqlTypeAdapter<Gradient>
    {
        public override string Description => "Gradient: write as [[color, time], …] or [{color: \"#ff0000\", time: 0}, …]";

        protected override JsonNode? Encode(Gradient value, EncodeContext context)
        {
            var keys = new JsonArray();
            foreach (var k in value.colorKeys)
                keys.Add(new JsonArray(context.EncodeValue(KeyColor(value, k)), JsonValue.Create(k.time)));
            return keys;
        }

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is IList<object?> list)) return AdapterOutcome.NotHandled;
            var colors = new List<GradientColorKey>();
            var alphas = new List<GradientAlphaKey>();
            for (int i = 0; i < list.Count; i++)
            {
                object? c, t;
                switch (list[i])
                {
                    case IList<object?> pair when pair.Count == 2: c = pair[0]; t = pair[1]; break;
                    case ObjectValue o when o.ContainsKey("color") && o.ContainsKey("time"): c = o["color"]; t = o["time"]; break;
                    default: reason = $"[{i}]: expected [color, time] or {{color, time}}"; return AdapterOutcome.Failed;
                }
                if (!kernel.TryCoerce(c, typeof(Color), out var color, out reason, path: $"[{i}].color")) return AdapterOutcome.Failed;
                if (!kernel.TryCoerce(t, typeof(float), out var time, out reason, path: $"[{i}].time")) return AdapterOutcome.Failed;
                var col = (Color)color!;
                colors.Add(new GradientColorKey(col, (float)time!));
                alphas.Add(new GradientAlphaKey(col.a, (float)time!));
            }
            if (colors.Count < 1 || colors.Count > 8) { reason = "a Gradient has 1..8 keys"; return AdapterOutcome.Failed; }
            var g = new Gradient();
            g.SetKeys(colors.ToArray(), alphas.ToArray());
            result = g;
            return AdapterOutcome.Ok;
        }

        protected override bool? Equal(Gradient value, object? other, RqlKernel kernel)
        {
            Gradient? o = other as Gradient;
            if (o == null && kernel.TryCoerce(other, typeof(Gradient), out var coerced, out _)) o = coerced as Gradient;
            if (o == null) return null;
            var a = value.colorKeys;
            var b = o.colorKeys;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (Math.Abs(a[i].time - b[i].time) >= 1e-4f || kernel.Equal(KeyColor(value, a[i]), KeyColor(o, b[i])) != true) return false;
            return true;
        }

        // A key as it is read and compared: its RGB plus the gradient's alpha at its time (alpha keys are separate in Unity and
        // the color key's own alpha is not meaningful), so equality matches the encoded form.
        private static Color KeyColor(Gradient g, GradientColorKey k) => new Color(k.color.r, k.color.g, k.color.b, g.Evaluate(k.time).a);
    }
}
