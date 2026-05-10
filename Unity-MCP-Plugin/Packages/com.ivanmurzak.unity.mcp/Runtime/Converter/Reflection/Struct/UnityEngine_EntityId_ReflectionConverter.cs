/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if UNITY_6000_5_OR_NEWER
using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    /// <summary>
    /// Reflection converter for UnityEngine.EntityId.
    /// Serializes and deserializes EntityId as a primitive <see cref="ulong"/> value.
    /// </summary>
    public partial class UnityEngine_EntityId_ReflectionConverter : UnityStructReflectionConverter<EntityId>
    {
        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined)
                return true; // No value to set, keep existing

            if (value.Value.ValueKind == JsonValueKind.Null)
            {
                obj = EntityId.None;
                return true;
            }

            try
            {
                if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetUInt64(out var raw))
                {
                    obj = EntityId.FromULong(raw);
                    return true;
                }

                var entityId = value.Value.Deserialize<EntityId>(reflector.JsonSerializerOptions);
                obj = entityId;
                return true;
            }
            catch (Exception ex)
            {
                logs?.Error($"Failed to deserialize EntityId: {ex.Message}", depth);
                return false;
            }
        }
    }
}
#endif
