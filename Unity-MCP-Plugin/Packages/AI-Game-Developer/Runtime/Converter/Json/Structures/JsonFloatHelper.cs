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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.JsonConverters
{
    /// <summary>
    /// Provides shared helper methods for reading and writing floating-point values and Vector3 types
    /// with support for special floating-point literals (NaN, Infinity, -Infinity).
    /// </summary>
    public static class JsonFloatHelper
    {
        /// <summary>
        /// Reads a float value from the JSON reader, supporting named floating-point literals
        /// (NaN, Infinity, -Infinity) when enabled in options.
        /// </summary>
        public static float ReadFloat(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if ((options.NumberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
                {
                    var s = reader.GetString();
                    if (s == "NaN") return float.NaN;
                    if (s == "Infinity") return float.PositiveInfinity;
                    if (s == "-Infinity") return float.NegativeInfinity;
                    throw new JsonException($"Unrecognized floating-point literal: '{s}'. Expected 'NaN', 'Infinity', or '-Infinity'.");
                }
                throw new JsonException($"String token encountered but AllowNamedFloatingPointLiterals is not enabled. Value: '{reader.GetString()}'.");
            }
            return reader.GetSingle();
        }

        /// <summary>
        /// Writes a float value with the given property name, using string representation
        /// for special values (NaN, Infinity) when enabled in options.
        /// </summary>
        public static void WriteFloat(Utf8JsonWriter writer, string propertyName, float value, JsonSerializerOptions options)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if ((options.NumberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
                {
                    writer.WriteString(propertyName, value.ToString(CultureInfo.InvariantCulture));
                    return;
                }
            }
            writer.WriteNumber(propertyName, value);
        }

        /// <summary>
        /// Reads a Vector3 from the JSON reader.
        /// </summary>
        public static Vector3 ReadVector3(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token for Vector3.");

            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector3(x, y, z);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = ReadFloat(ref reader, options);
                            break;
                        case "y":
                            y = ReadFloat(ref reader, options);
                            break;
                        case "z":
                            z = ReadFloat(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + "Expected 'x', 'y', or 'z'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token for Vector3.");
        }

        /// <summary>
        /// Writes a Vector3 to the JSON writer.
        /// </summary>
        public static void WriteVector3(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteFloat(writer, "x", value.x, options);
            WriteFloat(writer, "y", value.y, options);
            WriteFloat(writer, "z", value.z, options);
            writer.WriteEndObject();
        }
    }
}
