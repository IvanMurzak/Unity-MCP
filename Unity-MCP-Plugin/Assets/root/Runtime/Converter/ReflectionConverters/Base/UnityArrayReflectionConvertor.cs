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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Convertor;
using com.IvanMurzak.ReflectorNet.Model;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.McpPlugin.Common.Reflection.Convertor
{
    public partial class UnityArrayReflectionConvertor : ArrayReflectionConvertor
    {
        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (fallbackType == null || !fallbackType.IsArray)
                return base.Deserialize(reflector, data, fallbackType, fallbackName, depth, stringBuilder, logger);

            var elementType = fallbackType.GetElementType();
            if (elementType == null)
                return base.Deserialize(reflector, data, fallbackType, fallbackName, depth, stringBuilder, logger);

            if (data.valueJsonElement == null || data.valueJsonElement.Value.ValueKind != JsonValueKind.Array)
                return base.Deserialize(reflector, data, fallbackType, fallbackName, depth, stringBuilder, logger);

            var jsonArray = data.valueJsonElement.Value;
            var length = jsonArray.GetArrayLength();
            var array = Array.CreateInstance(elementType, length);

            int index = 0;
            foreach (var element in jsonArray.EnumerateArray())
            {
                SerializedMember? member = null;
                if (element.ValueKind == JsonValueKind.Object &&
                    (
                        element.TryGetProperty(nameof(SerializedMember.typeName), out _) ||
                        element.TryGetProperty(nameof(SerializedMember.fields), out _) ||
                        element.TryGetProperty(nameof(SerializedMember.props), out _))
                    )
                {
                    try
                    {
                        member = JsonSerializer.Deserialize<SerializedMember>(element.GetRawText());
                        if (member != null && element.TryGetProperty(nameof(SerializedMember.valueJsonElement), out var valueProp))
                        {
                            member.valueJsonElement = valueProp;
                        }
                    }
                    catch { }
                }

                if (member == null)
                {
                    member = new SerializedMember
                    {
                        valueJsonElement = element
                    };
                }

                var value = reflector.Deserialize(member, elementType, null, depth + 1, stringBuilder, logger);
                array.SetValue(value, index);
                index++;
            }

            return array;
        }

        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.IsPublic || field.IsPrivate && field.GetCustomAttribute<SerializeField>() != null);
    }
}
