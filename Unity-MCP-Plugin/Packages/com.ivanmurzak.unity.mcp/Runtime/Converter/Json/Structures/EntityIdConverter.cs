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
#if UNITY_6000_5_OR_NEWER
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.JsonConverters
{
    public class EntityIdConverter : JsonSchemaConverter<EntityId>, IJsonSchemaConverter
    {
        public override JsonNode GetSchema() => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Integer
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return EntityId.None;

            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"Expected number token for {nameof(EntityId)}, got {reader.TokenType}.");

            return ReadEntityIdValue(ref reader);
        }

        // Accept both JSON representations so handwritten JSON (legacy int form
        // from EntityId.ToString) and machine-serialized JSON (full raw ulong
        // from EntityId.ToULong) both round-trip correctly.
        internal static EntityId ReadEntityIdValue(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt32(out var intValue))
                return EntityIdUtils.FromLegacyInstanceId(intValue);

            if (reader.TryGetUInt64(out var unsignedValue))
                return EntityIdUtils.FromRawValue(unsignedValue);

            return EntityIdUtils.FromNumber(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(EntityId.ToULong(value));
        }
    }
}
#endif
