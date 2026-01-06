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
    public class Matrix4x4Converter : JsonSchemaConverter<Matrix4x4>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["m00"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m01"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m02"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m03"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m10"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m11"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m12"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m13"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m20"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m21"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m22"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m23"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m30"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m31"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m32"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                ["m33"] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number }
            },
            [JsonSchema.Required] = new JsonArray
            {
                "m00", "m01", "m02", "m03",
                "m10", "m11", "m12", "m13",
                "m20", "m21", "m22", "m23",
                "m30", "m31", "m32", "m33"
            },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override Matrix4x4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            float m00 = 0, m01 = 0, m02 = 0, m03 = 0;
            float m10 = 0, m11 = 0, m12 = 0, m13 = 0;
            float m20 = 0, m21 = 0, m22 = 0, m23 = 0;
            float m30 = 0, m31 = 0, m32 = 0, m33 = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    var m = new Matrix4x4();
                    m.m00 = m00; m.m01 = m01; m.m02 = m02; m.m03 = m03;
                    m.m10 = m10; m.m11 = m11; m.m12 = m12; m.m13 = m13;
                    m.m20 = m20; m.m21 = m21; m.m22 = m22; m.m23 = m23;
                    m.m30 = m30; m.m31 = m31; m.m32 = m32; m.m33 = m33;
                    return m;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "m00": m00 = ReadFloat(ref reader, options); break;
                        case "m01": m01 = ReadFloat(ref reader, options); break;
                        case "m02": m02 = ReadFloat(ref reader, options); break;
                        case "m03": m03 = ReadFloat(ref reader, options); break;
                        case "m10": m10 = ReadFloat(ref reader, options); break;
                        case "m11": m11 = ReadFloat(ref reader, options); break;
                        case "m12": m12 = ReadFloat(ref reader, options); break;
                        case "m13": m13 = ReadFloat(ref reader, options); break;
                        case "m20": m20 = ReadFloat(ref reader, options); break;
                        case "m21": m21 = ReadFloat(ref reader, options); break;
                        case "m22": m22 = ReadFloat(ref reader, options); break;
                        case "m23": m23 = ReadFloat(ref reader, options); break;
                        case "m30": m30 = ReadFloat(ref reader, options); break;
                        case "m31": m31 = ReadFloat(ref reader, options); break;
                        case "m32": m32 = ReadFloat(ref reader, options); break;
                        case "m33": m33 = ReadFloat(ref reader, options); break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}.");
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

        public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    WriteFloat(writer, $"m{i}{j}", value[i, j], options);
                }
            }
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

