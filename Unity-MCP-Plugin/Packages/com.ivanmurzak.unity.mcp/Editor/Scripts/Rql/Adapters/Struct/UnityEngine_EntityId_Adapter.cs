/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if UNITY_6000_5_OR_NEWER
#nullable enable
using System;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// EntityId (Unity 6.5+) from what agents and tests send, as the JSON EntityIdConverter accepts it: decimal digits as
    /// text (exact for every 64-bit id) or a number (exact up to 2^53).
    /// </summary>
    public sealed class UnityEngine_EntityId_Adapter : RqlTypeAdapter
    {
        public UnityEngine_EntityId_Adapter() : base(typeof(EntityId)) { }

        public override string? Description => "an instance id: decimal digits as text (\"123456\") or a number";

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            switch (value)
            {
                case string s when s.Length > 0 && s.Trim().Length > 0:
                    try { result = EntityIdUtils.FromString(s.Trim()); return AdapterOutcome.Ok; }
                    catch (Exception ex) { reason = ex.Message; return AdapterOutcome.Failed; }
                case double d when Math.Floor(d) == d && Math.Abs(d) <= 9007199254740992d:
                    result = EntityIdUtils.FromNumber((long)d);
                    return AdapterOutcome.Ok;
                case double _:
                    reason = "an instance id must be a whole number (give large ids as text)";
                    return AdapterOutcome.Failed;
                case long l:
                    result = EntityIdUtils.FromNumber(l);
                    return AdapterOutcome.Ok;
                case int i:
                    result = EntityIdUtils.FromNumber(i);
                    return AdapterOutcome.Ok;
                default:
                    return AdapterOutcome.NotHandled;
            }
        }
    }
}
#endif
