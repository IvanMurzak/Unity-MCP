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
    public class BoundsIntConverter : JsonSchemaConverter<BoundsInt>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["position"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Properties] = new JsonObject
                    {
                        ["x"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer },
                        ["y"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer },
                        ["z"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer }
                    },
                    [JsonSchema.Required] = new JsonArray { "x", "y", "z" }
                },
                ["size"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Properties] = new JsonObject
                    {
                        ["x"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer },
                        ["y"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer },
                        ["z"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Integer }
                    },
                    [JsonSchema.Required] = new JsonArray { "x", "y", "z" }
                }
            },
            [JsonSchema.Required] = new JsonArray { "position", "size" },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override BoundsInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var position = Vector3Int.zero;
            var size = Vector3Int.zero;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new BoundsInt(position, size);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "position":
                            position = ReadVector3Int(ref reader);
                            break;
                        case "size":
                            size = ReadVector3Int(ref reader);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + "Expected 'position' or 'size'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, BoundsInt value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("position");
            WriteVector3Int(writer, value.position);

            writer.WritePropertyName("size");
            WriteVector3Int(writer, value.size);

            writer.WriteEndObject();
        }

        private Vector3Int ReadVector3Int(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token for Vector3Int.");

            int x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector3Int(x, y, z);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = reader.GetInt32();
                            break;
                        case "y":
                            y = reader.GetInt32();
                            break;
                        case "z":
                            z = reader.GetInt32();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + "Expected 'x', 'y', or 'z'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token for Vector3Int.");
        }

        private void WriteVector3Int(Utf8JsonWriter writer, Vector3Int value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}

