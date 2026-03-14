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
using com.IvanMurzak.Unity.MCP.Runtime.Data;

namespace com.IvanMurzak.Unity.MCP.JsonConverters
{
    public class SceneRefConverter : JsonConverter<SceneRef>
    {
        public override SceneRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var sceneRef = new SceneRef();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return sceneRef;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case ObjectRef.ObjectRefProperty.InstanceID:
                            sceneRef.InstanceID = reader.GetInt32();
                            break;
                        case SceneRef.SceneRefProperty.Path:
                            sceneRef.Path = reader.GetString() ?? string.Empty;
                            break;
                        case SceneRef.SceneRefProperty.BuildIndex:
                            sceneRef.BuildIndex = reader.GetInt32();
                            break;
                        default:
                            throw new JsonException($"[SceneRefConverter] Unexpected property name: {propertyName}. "
                                + $"Expected {SceneRef.SceneRefProperty.All.JoinEnclose()}.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, SceneRef value, JsonSerializerOptions options)
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

            // Write the "path" property
            if (!string.IsNullOrEmpty(value.Path))
                writer.WriteString(SceneRef.SceneRefProperty.Path, value.Path);

            // Write the "buildIndex" property
            writer.WriteNumber(SceneRef.SceneRefProperty.BuildIndex, value.BuildIndex);

            writer.WriteEndObject();
        }
    }
}
