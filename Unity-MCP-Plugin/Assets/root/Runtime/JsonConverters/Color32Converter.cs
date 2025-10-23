/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Common.Json.Converters
{
    public class Color32Converter : JsonSchemaConverter<Color32>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["r"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Integer,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 255
                },
                ["g"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Integer,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 255
                },
                ["b"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Integer,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 255
                },
                ["a"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Integer,
                    [JsonSchema.Minimum] = 0,
                    [JsonSchema.Maximum] = 255
                }
            },
            [JsonSchema.Required] = new JsonArray { "r", "g", "b", "a" },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override Color32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            byte r = 0, g = 0, b = 0, a = 255;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Color32(r, g, b, a);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "r":
                            r = reader.GetByte();
                            break;
                        case "g":
                            g = reader.GetByte();
                            break;
                        case "b":
                            b = reader.GetByte();
                            break;
                        case "a":
                            a = reader.GetByte();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected 'r', 'g', 'b', or 'a'.");
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Color32 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.r);
            writer.WriteNumber("g", value.g);
            writer.WriteNumber("b", value.b);
            writer.WriteNumber("a", value.a);
            writer.WriteEndObject();
        }
    }
}

