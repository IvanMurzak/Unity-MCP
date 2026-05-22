/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

// Wire contract for EntityId (Unity 6.5+):
//   On the wire: JSON string of decimal digits, e.g. "568105584918935294".
//                No sign, no leading zeros, no exponent.
//   Schema:      { "type": "string", "pattern": "^[0-9]+$" }.
//
//   Outbound: always emitted as a JSON string (via WriteStringValue).
//   Inbound:  accepts both a JSON string (preferred) and a JSON number
//             (back-compat — legacy clients pre-#759 wrote a number).
//
// Rationale: Unity 6.5's EntityId is a 64-bit ulong. JS-based MCP clients
// (Claude Agent SDK, etc.) parse JSON numbers as IEEE-754 doubles, so any
// value past 2^53 - 1 rounds. Serializing as a string makes the value
// opaque to every JSON parser, preserving full precision through any
// language boundary. This is the same pattern used by Twitter, YouTube,
// Discord, Stripe, and gRPC-JSON for 64-bit IDs. See #759 / #754.

#nullable enable
#if UNITY_6000_5_OR_NEWER
using System;
using System.Globalization;
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
            [JsonSchema.Type] = JsonSchema.String,
            [JsonSchema.Pattern] = "^[0-9]+$"
        };
        public override JsonNode GetSchemaRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return EntityId.None;

            if (reader.TokenType != JsonTokenType.String && reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"Expected string or number token for {nameof(EntityId)}, got {reader.TokenType}.");

            return ReadEntityIdValue(ref reader);
        }

        // Accept three JSON representations:
        //   1. String token — the new on-wire format (decimal digits, see top-of-file).
        //   2. Legacy int form from EntityId.ToString (handwritten JSON).
        //   3. Raw ulong from EntityId.ToULong (machine-serialized JSON).
        // All three round-trip correctly.
        internal static EntityId ReadEntityIdValue(ref Utf8JsonReader reader)
        {
            // String token — the new on-wire format.
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                // Reject empty string up-front: the schema regex "^[0-9]+$" requires
                // one or more digits, but EntityIdUtils.FromString("") returns
                // EntityId.None silently. Throw here so the runtime matches the
                // published schema (see #759 wire contract at top-of-file).
                if (string.IsNullOrEmpty(stringValue))
                    throw new JsonException($"{nameof(EntityId)} string must be one or more decimal digits, got empty string.");
                return EntityIdUtils.FromString(stringValue);
            }

            if (reader.TryGetInt64(out var signedValue))
                return EntityIdUtils.FromNumber(signedValue);

            if (reader.TryGetUInt64(out var unsignedValue))
                return EntityIdUtils.FromRawValue(unsignedValue);

            // Fallback for numeric tokens STJ 8's TryGet* helpers reject
            // (e.g. numbers re-serialized through JsonElement that end up with
            // a fractional trailing zero). Parse the raw token text ourselves.
            byte[] rawBytes;
            if (reader.HasValueSequence)
            {
                var seq = reader.ValueSequence;
                rawBytes = new byte[seq.Length];
                var offset = 0;
                foreach (var mem in seq)
                {
                    mem.Span.CopyTo(new Span<byte>(rawBytes, offset, mem.Length));
                    offset += mem.Length;
                }
            }
            else
            {
                rawBytes = reader.ValueSpan.ToArray();
            }
            return EntityIdUtils.FromString(System.Text.Encoding.UTF8.GetString(rawBytes));
        }

        public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
        {
            // Outbound: always a JSON string of decimal digits so JS clients
            // can JSON.parse the payload without IEEE-754 precision loss
            // (see top-of-file wire contract).
            writer.WriteStringValue(EntityId.ToULong(value).ToString(CultureInfo.InvariantCulture));
        }
    }
}
#endif
