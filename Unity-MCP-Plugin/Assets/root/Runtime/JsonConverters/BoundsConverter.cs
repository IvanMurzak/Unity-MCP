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
    public class BoundsConverter : JsonSchemaConverter<Bounds>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["center"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Properties] = new JsonObject
                    {
                        ["x"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                        ["y"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                        ["z"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number }
                    },
                    [JsonSchema.Required] = new JsonArray { "x", "y", "z" }
                },
                ["size"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Properties] = new JsonObject
                    {
                        ["x"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                        ["y"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                        ["z"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number }
                    },
                    [JsonSchema.Required] = new JsonArray { "x", "y", "z" }
                }
            },
            [JsonSchema.Required] = new JsonArray { "center", "size" },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override Bounds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var center = Vector3.zero;
            var size = Vector3.zero;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Bounds(center, size);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "center":
                            center = ReadVector3(ref reader);
                            break;
                        case "size":
                            size = ReadVector3(ref reader);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + "Expected 'center' or 'size'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, Bounds value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("center");
            WriteVector3(writer, value.center);

            writer.WritePropertyName("size");
            WriteVector3(writer, value.size);

            writer.WriteEndObject();
        }

        private Vector3 ReadVector3(ref Utf8JsonReader reader)
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
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + "Expected 'x', 'y', or 'z'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token for Vector3.");
        }

        private void WriteVector3(Utf8JsonWriter writer, Vector3 value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}

