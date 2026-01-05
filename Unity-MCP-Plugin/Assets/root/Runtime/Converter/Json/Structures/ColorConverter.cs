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
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.JsonConverters
{
    public class ColorConverter : JsonSchemaConverter<Color>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["r"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Number,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 1
                },
                ["g"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Number,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 1
                },
                ["b"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Number,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 1
                },
                ["a"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Number,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 1
                }
            },
            [JsonSchema.Required] = new JsonArray { "r", "g", "b", "a" },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            float r = 0, g = 0, b = 0, a = 1;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Color(r, g, b, a);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "r":
                            r = ReadFloat(ref reader, options);
                            break;
                        case "g":
                            g = ReadFloat(ref reader, options);
                            break;
                        case "b":
                            b = ReadFloat(ref reader, options);
                            break;
                        case "a":
                            a = ReadFloat(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected 'r', 'g', 'b', or 'a'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        private float ReadFloat(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if ((options.NumberHandling & System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
                {
                    var s = reader.GetString();
                    if (s == "NaN") return float.NaN;
                    if (s == "Infinity") return float.PositiveInfinity;
                    if (s == "-Infinity") return float.NegativeInfinity;
                }
            }
            return reader.GetSingle();
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteFloat(writer, "r", value.r, options);
            WriteFloat(writer, "g", value.g, options);
            WriteFloat(writer, "b", value.b, options);
            WriteFloat(writer, "a", value.a, options);
            writer.WriteEndObject();
        }

        private void WriteFloat(Utf8JsonWriter writer, string propertyName, float value, JsonSerializerOptions options)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if ((options.NumberHandling & System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
                {
                    writer.WriteString(propertyName, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }
            }
            writer.WriteNumber(propertyName, value);
        }
    }
}

