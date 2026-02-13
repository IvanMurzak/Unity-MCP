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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;

namespace com.IvanMurzak.Unity.MCP.JsonConverters
{
    public class GameObjectRefConverter : JsonConverter<GameObjectRef>
    {
        public override GameObjectRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                return new GameObjectRef
                {
                    InstanceID = reader.GetInt32()
                };
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token or a number (InstanceID).");

            var result = new GameObjectRef();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return result;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case ObjectRef.ObjectRefProperty.InstanceID:
                            result.InstanceID = reader.GetInt32();
                            break;
                        case GameObjectRef.GameObjectRefProperty.Path:
                            result.Path = reader.GetString();
                            break;
                        case GameObjectRef.GameObjectRefProperty.Name:
                            result.Name = reader.GetString();
                            break;
                        case AssetObjectRef.AssetObjectRefProperty.AssetType:
                            result.AssetType = TypeUtils.GetType(reader.GetString());
                            break;
                        case AssetObjectRef.AssetObjectRefProperty.AssetPath:
                            result.AssetPath = reader.GetString();
                            break;
                        case AssetObjectRef.AssetObjectRefProperty.AssetGuid:
                            result.AssetGuid = reader.GetString();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected {GameObjectRef.GameObjectRefProperty.All.JoinEnclose()}.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, GameObjectRef value, JsonSerializerOptions options)
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

            writer.WriteNumber(ObjectRef.ObjectRefProperty.InstanceID, value.InstanceID);

            if (!string.IsNullOrEmpty(value.Path))
                writer.WriteString(GameObjectRef.GameObjectRefProperty.Path, value.Path);

            if (!string.IsNullOrEmpty(value.Name))
                writer.WriteString(GameObjectRef.GameObjectRefProperty.Name, value.Name);

            if (value.AssetType != null)
                writer.WriteString(AssetObjectRef.AssetObjectRefProperty.AssetType, value.AssetType.GetTypeId());

            if (!string.IsNullOrEmpty(value.AssetPath))
                writer.WriteString(AssetObjectRef.AssetObjectRefProperty.AssetPath, value.AssetPath);

            if (!string.IsNullOrEmpty(value.AssetGuid))
                writer.WriteString(AssetObjectRef.AssetObjectRefProperty.AssetGuid, value.AssetGuid);

            writer.WriteEndObject();
        }
    }
}

