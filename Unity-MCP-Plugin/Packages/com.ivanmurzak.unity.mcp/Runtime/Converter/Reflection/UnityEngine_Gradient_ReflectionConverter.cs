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
using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    /// <summary>
    /// Reflection converter for UnityEngine.Gradient. Gradient is a class (not a struct)
    /// that is NOT a UnityEngine.Object, so it cannot go through the asset-reference path.
    /// This converter overrides SetValue to deserialize directly via the registered JSON converter.
    /// </summary>
    public partial class UnityEngine_Gradient_ReflectionConverter : UnityGenericReflectionConverter<Gradient>
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
                obj = null;
                return true;
            }

            try
            {
                var gradient = value.Value.Deserialize<Gradient>(reflector.JsonSerializerOptions);
                if (gradient != null)
                {
                    obj = gradient;
                    return true;
                }

                logs?.Error("Failed to deserialize Gradient from JSON.", depth);
                return false;
            }
            catch (Exception ex)
            {
                logs?.Error($"Failed to deserialize Gradient: {ex.Message}", depth);
                return false;
            }
        }
    }
}
