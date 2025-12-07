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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Convertor;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.McpPlugin.Common.Reflection.Convertor
{
    public partial class UnityGenericReflectionConvertor<T> : GenericReflectionConvertor<T>
    {
        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.IsPublic || field.IsPrivate && field.GetCustomAttribute<SerializeField>() != null);

        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var type = fallbackType ?? typeof(T);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return data.valueJsonElement
                   .ToAssetObjectRef(reflector, suppressException: true, depth: depth, stringBuilder: stringBuilder, logger: logger)
                   .FindAssetObject(type);
            }
            return base.Deserialize(reflector, data, fallbackType, fallbackName, depth, stringBuilder, logger);
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            System.Text.Json.JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var originalObj = obj;
            var result = base.SetValue(reflector, ref obj, type, value, depth, stringBuilder, logger);

            // If obj became null but we had an object, and the value didn't explicitly say null, restore it.
            // This handles cases where TryPopulate is called with an existing object but no valueJsonElement.
            if (obj == null && originalObj != null)
            {
                var isExplicitNull = value.HasValue && value.Value.ValueKind == System.Text.Json.JsonValueKind.Null;
                if (!isExplicitNull)
                {
                    obj = originalObj;
                }
            }

            return result;
        }

        protected override bool TryPopulateField(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember fieldValue,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (obj == null)
            {
                logger?.LogError("{padding}obj is null in TryPopulateField for {field}", padding, fieldValue.name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] obj is null in TryPopulateField for {fieldValue.name}");
                return false;
            }

            var field = objType.GetField(fieldValue.name, flags);
            if (field == null)
            {
                logger?.LogError("{padding}Field {field} not found on {type}", padding, fieldValue.name, objType.Name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Field {fieldValue.name} not found on {objType.Name}");
                return false;
            }

            try
            {
                var value = reflector.Deserialize(fieldValue, field.FieldType, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);
                field.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "{padding}Failed to set field {field}", padding, fieldValue.name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to set field {fieldValue.name}: {e.Message}");
                return false;
            }
        }

        protected override bool TryPopulateProperty(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember member,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (obj == null)
            {
                logger?.LogError("{padding}obj is null in TryPopulateProperty for {property}", padding, member.name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] obj is null in TryPopulateProperty for {member.name}");
                return false;
            }

            var property = objType.GetProperty(member.name, flags);
            if (property == null || !property.CanWrite)
            {
                logger?.LogError("{padding}Property {property} not found or not writable on {type}", padding, member.name, objType.Name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property {member.name} not found or not writable on {objType.Name}");
                return false;
            }

            try
            {
                var value = reflector.Deserialize(member, property.PropertyType, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);
                property.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "{padding}Failed to set property {property}", padding, member.name);
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to set property {member.name}: {e.Message}");
                return false;
            }
        }
    }
}
