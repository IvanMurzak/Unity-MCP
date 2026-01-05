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
                            center = ReadVector3(ref reader, options);
                            break;
                        case "size":
                            size = ReadVector3(ref reader, options);
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
            WriteVector3(writer, value.center, options);

            writer.WritePropertyName("size");
            WriteVector3(writer, value.size, options);

            writer.WriteEndObject();
        }

        private Vector3 ReadVector3(ref Utf8JsonReader reader, JsonSerializerOptions options)
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

        private void WriteVector3(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            WriteFloat(writer, "x", value.x, options);
            WriteFloat(writer, "y", value.y, options);
            WriteFloat(writer, "z", value.z, options);
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

