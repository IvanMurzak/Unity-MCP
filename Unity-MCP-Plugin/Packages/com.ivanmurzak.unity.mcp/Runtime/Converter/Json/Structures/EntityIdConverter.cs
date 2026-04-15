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

        // Unity 6.5+ EntityId stores a 64-bit raw value of the form
        // (0x7E2510500000000UL | (ulong)(uint)intInstanceID). Its ToString() emits only
        // the low 32 bits as a signed int, while ToULong() emits the full raw value.
        // Accept both JSON representations so handwritten JSON (legacy int form) and
        // machine-serialized JSON (full raw ulong) both round-trip correctly.
        const ulong EntityIdMagic = 0x7E2510500000000UL;

        internal static EntityId ReadEntityIdValue(ref Utf8JsonReader reader)
        {
            // Legacy int form: EntityId.ToString() — reconstruct with the magic prefix.
            if (reader.TryGetInt32(out var intValue))
                return EntityId.FromULong(EntityIdMagic | (uint)intValue);

            // Raw ulong form: EntityId.ToULong().
            if (reader.TryGetUInt64(out var unsignedValue))
                return EntityId.FromULong(unsignedValue);

            // Signed long outside int range — unlikely, but be tolerant.
            return EntityId.FromULong(unchecked((ulong)reader.GetInt64()));
        }

        public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(EntityId.ToULong(value));
        }
    }
}
#endif
