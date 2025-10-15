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
    public class ComponentRefConverter : JsonConverter<ComponentRef>
    {
        public override ComponentRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var result = new ComponentRef();

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
                        case ComponentRef.ComponentRefProperty.Index:
                            result.Index = reader.GetInt32();
                            break;
                        case ComponentRef.ComponentRefProperty.TypeName:
                            result.TypeName = reader.GetString();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected {ComponentRef.ComponentRefProperty.All.JoinEnclose()}.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, ComponentRef value, JsonSerializerOptions options)
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

            if (value.Index != -1)
                writer.WriteNumber(ComponentRef.ComponentRefProperty.Index, value.Index);

            if (!string.IsNullOrEmpty(value.TypeName))
                writer.WriteString(ComponentRef.ComponentRefProperty.TypeName, value.TypeName);

            writer.WriteEndObject();
        }
    }
}

