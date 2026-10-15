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
    /// LayerMask (cameras, lights, physics queries, NavMeshSurface …): its only state is a private field, so it needs an adapter
    /// to be writable. Reads as the int mask; writes take an int, a layer name, a list of layer names, "Everything" or "Nothing".
    /// </summary>
    public sealed class UnityEngine_LayerMask_Adapter : RqlTypeAdapter<LayerMask>
    {
        public override string Description => "LayerMask: an int mask (-1 = Everything), a layer name or a list of layer names";

        protected override JsonNode? Encode(LayerMask value, EncodeContext context) => JsonValue.Create(value.value);

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            switch (value)
            {
                case string name:
                    if (!TryMask(new[] { name }, out var single, out reason)) return AdapterOutcome.Failed;
                    result = (LayerMask)single;
                    return AdapterOutcome.Ok;
                case IList<object?> list:
                    var names = new List<string>();
                    foreach (var item in list)
                    {
                        if (!(item is string s)) { reason = "expected layer names"; return AdapterOutcome.Failed; }
                        names.Add(s);
                    }
                    if (!TryMask(names, out var mask, out reason)) return AdapterOutcome.Failed;
                    result = (LayerMask)mask;
                    return AdapterOutcome.Ok;
                default:
                    if (!kernel.TryCoerce(value, typeof(int), out var number, out _))
                    {
                        reason = "expected an int mask, a layer name or a list of layer names";
                        return AdapterOutcome.Failed;
                    }
                    result = (LayerMask)(int)number!;
                    return AdapterOutcome.Ok;
            }
        }

        protected override bool? Equal(LayerMask value, object? other, RqlKernel kernel) =>
            kernel.TryCoerce(other, typeof(LayerMask), out var coerced, out _) && coerced is LayerMask o ? value.value == o.value : (bool?)null;

        private static bool TryMask(IEnumerable<string> names, out int mask, out string? reason)
        {
            mask = 0;
            reason = null;
            foreach (var name in names)
            {
                if (string.Equals(name, "Everything", StringComparison.OrdinalIgnoreCase)) { mask = ~0; continue; }
                if (string.Equals(name, "Nothing", StringComparison.OrdinalIgnoreCase)) continue;
                int layer = LayerMask.NameToLayer(name);
                if (layer < 0) { reason = $"no layer named '{name}'"; return false; }
                mask |= 1 << layer;
            }
            return true;
        }
    }
}
