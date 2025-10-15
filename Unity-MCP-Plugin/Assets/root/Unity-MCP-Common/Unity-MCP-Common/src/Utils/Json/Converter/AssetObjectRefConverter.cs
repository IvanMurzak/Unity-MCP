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
using System.Text.Json.Serialization;
using com.IvanMurzak.Unity.MCP.Common.Model.Unity;

namespace com.IvanMurzak.Unity.MCP.Common.Json
{
    public class AssetObjectRefConverter : JsonConverter<AssetObjectRef>
    {
        public override AssetObjectRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var assetObjectRef = new AssetObjectRef();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return assetObjectRef;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case ObjectRef.ObjectRefProperty.InstanceID:
                            assetObjectRef.InstanceID = reader.GetInt32();
                            break;
                        case AssetObjectRef.AssetObjectRefProperty.AssetPath:
                            assetObjectRef.AssetPath = reader.GetString();
                            break;
                        case AssetObjectRef.AssetObjectRefProperty.AssetGuid:
                            assetObjectRef.AssetGuid = reader.GetString();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected {AssetObjectRef.AssetObjectRefProperty.All.JoinEnclose()}.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, AssetObjectRef value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStartObject();

                // Write the "instanceID" property
                writer.WriteNumber(ObjectRef.ObjectRefProperty.InstanceID, 0);

                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject();

            // Write the "instanceID" property
            writer.WriteNumber(ObjectRef.ObjectRefProperty.InstanceID, value.InstanceID);

            // Write the "assetPath" property
            if (!string.IsNullOrEmpty(value.AssetPath))
                writer.WriteString(AssetObjectRef.AssetObjectRefProperty.AssetPath, value.AssetPath);

            // Write the "assetGuid" property
            if (!string.IsNullOrEmpty(value.AssetGuid))
                writer.WriteString(AssetObjectRef.AssetObjectRefProperty.AssetGuid, value.AssetGuid);

            writer.WriteEndObject();
        }
    }
}
