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
                            r = JsonFloatHelper.ReadFloat(ref reader, options);
                            break;
                        case "g":
                            g = JsonFloatHelper.ReadFloat(ref reader, options);
                            break;
                        case "b":
                            b = JsonFloatHelper.ReadFloat(ref reader, options);
                            break;
                        case "a":
                            a = JsonFloatHelper.ReadFloat(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected 'r', 'g', 'b', or 'a'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            JsonFloatHelper.WriteFloat(writer, "r", value.r, options);
            JsonFloatHelper.WriteFloat(writer, "g", value.g, options);
            JsonFloatHelper.WriteFloat(writer, "b", value.b, options);
            JsonFloatHelper.WriteFloat(writer, "a", value.a, options);
            writer.WriteEndObject();
        }
    }
}
