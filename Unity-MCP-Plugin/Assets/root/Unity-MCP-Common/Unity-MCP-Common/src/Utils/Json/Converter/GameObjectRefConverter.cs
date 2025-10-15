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
    public class GameObjectRefConverter : JsonConverter<GameObjectRef>
    {
        public override GameObjectRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

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

            writer.WriteEndObject();
        }
    }
}

