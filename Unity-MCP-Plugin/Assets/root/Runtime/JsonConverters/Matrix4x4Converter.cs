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
                throw new JsonException();

            float[] elements = new float[16];
            int index = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Matrix4x4(
                        new Vector4(elements[0], elements[1], elements[2], elements[3]),
                        new Vector4(elements[4], elements[5], elements[6], elements[7]),
                        new Vector4(elements[8], elements[9], elements[10], elements[11]),
                        new Vector4(elements[12], elements[13], elements[14], elements[15])
                    );

                if (reader.TokenType == JsonTokenType.Number)
                {
                    elements[index++] = reader.GetSingle();
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    writer.WriteNumber($"m{i}{j}", value[i, j]);
                }
            }
            writer.WriteEndObject();
        }
    }
}

