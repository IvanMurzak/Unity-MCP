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
    public class GradientConverter : JsonSchemaConverter<Gradient>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                ["colorKeys"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Ref] = JsonSchema.RefValue + GradientColorKeyConverter.StaticId
                    }
                },
                ["alphaKeys"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Ref] = JsonSchema.RefValue + GradientAlphaKeyConverter.StaticId
                    }
                },
                ["mode"] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Enum] = new JsonArray { "Blend", "Fixed", "PerceptualBlend" }
                }
            },
            [JsonSchema.Required] = new JsonArray { "colorKeys", "alphaKeys" },
            [JsonSchema.AdditionalProperties] = false
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override Gradient Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            GradientColorKey[]? colorKeys = null;
            GradientAlphaKey[]? alphaKeys = null;
            var mode = GradientMode.Blend;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    var gradient = new Gradient();
                    gradient.mode = mode;
                    gradient.SetKeys(
                        colorKeys ?? new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                        alphaKeys ?? new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
                    );
                    return gradient;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "colorKeys":
                            colorKeys = System.Text.Json.JsonSerializer.Deserialize<GradientColorKey[]>(ref reader, options);
                            break;
                        case "alphaKeys":
                            alphaKeys = System.Text.Json.JsonSerializer.Deserialize<GradientAlphaKey[]>(ref reader, options);
                            break;
                        case "mode":
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                var modeStr = reader.GetString();
                                if (!Enum.TryParse(modeStr, out mode))
                                    throw new JsonException($"Unknown GradientMode: '{modeStr}'. Expected 'Blend', 'Fixed', or 'PerceptualBlend'.");
                            }
                            else if (reader.TokenType == JsonTokenType.Number)
                            {
                                mode = (GradientMode)reader.GetInt32();
                            }
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, Gradient value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("colorKeys");
            System.Text.Json.JsonSerializer.Serialize(writer, value.colorKeys, options);

            writer.WritePropertyName("alphaKeys");
            System.Text.Json.JsonSerializer.Serialize(writer, value.alphaKeys, options);

            writer.WriteString("mode", value.mode.ToString());

            writer.WriteEndObject();
        }
    }
}
