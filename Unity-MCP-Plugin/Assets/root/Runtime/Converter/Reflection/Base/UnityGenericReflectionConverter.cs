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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityGenericReflectionConverter<T> : GenericReflectionConverter<T>
    {
        protected override IEnumerable<FieldInfo>? GetSerializableFieldsInternal(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
        {
            return objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null)
                .Where(field => field.IsPublic || field.IsPrivate && field.GetCustomAttribute<SerializeField>() != null);
        }

        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            var type = fallbackType ?? typeof(T);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var result = data.valueJsonElement
                   .ToAssetObjectRef(
                       reflector: reflector,
                       suppressException: true,
                       depth: depth,
                       logs: logs,
                       logger: logger)
                   .FindAssetObject(type);

                // Register the object early (before deserializing children) so child references can resolve
                if (result != null && context != null)
                    context.Register(result);

                return result;
            }
            return base.Deserialize(
                reflector: reflector,
                data: data,
                fallbackType: fallbackType,
                fallbackName: fallbackName,
                depth: depth,
                logs: logs,
                logger: logger);
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            System.Text.Json.JsonElement? value,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var originalObj = obj;

            // For value types (structs) without an explicit value, preserve the original to allow partial updates.
            // This prevents resetting structs to default values when only nested fields are specified.
            if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && originalObj != null)
            {
                var hasExplicitValue = value.HasValue &&
                    value.Value.ValueKind != System.Text.Json.JsonValueKind.Undefined;
                if (!hasExplicitValue)
                {
                    // No explicit value provided - keep the original struct value
                    // so that nested field updates can modify individual members
                    return true;
                }
            }

            var result = base.SetValue(
                reflector: reflector,
                obj: ref obj,
                type: type,
                value: value,
                depth: depth,
                logs: logs,
                logger: logger);

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
            ref object obj,
            Type objType,
            SerializedMember fieldValue,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (obj == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError("{padding}obj is null in TryPopulateField for '{field}'", padding, fieldValue.name);
                logs?.Error($"obj is null in TryPopulateField for '{fieldValue.name}'", depth);
                return false;
            }

            var field = objType.GetField(fieldValue.name, flags);
            if (field == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError("{padding}Field '{field}' not found on '{type}'", padding, fieldValue.name, objType.GetTypeId());
                logs?.Error($"Field '{fieldValue.name}' not found on '{objType.GetTypeId()}'", depth);
                return false;
            }

            try
            {
                // For value types (structs) with nested fields/props, we need to:
                // 1. Get the existing value to preserve unspecified members
                // 2. Call TryPopulate on it to modify only the specified members
                // 3. Write the modified value back to the parent object
                // This prevents losing existing values when doing partial updates on structs.
                var fieldType = field.FieldType;
                var hasNestedMembers = (fieldValue.fields != null && fieldValue.fields.Count > 0) ||
                                       (fieldValue.props != null && fieldValue.props.Count > 0);

                if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum && hasNestedMembers)
                {
                    // Get existing struct value (boxed)
                    var existingValue = field.GetValue(obj);
                    if (existingValue != null)
                    {
                        // Populate the existing struct with only the specified members
                        var success = reflector.TryPopulate(
                            ref existingValue,
                            data: fieldValue,
                            depth: depth + 1,
                            logs: logs,
                            flags: flags,
                            logger: logger);

                        if (success)
                        {
                            // Write the modified struct back to the parent object
                            field.SetValue(obj, existingValue);
                            return true;
                        }
                        return false;
                    }
                }

                // For reference types, primitives, enums, or full replacements, use standard deserialization
                var value = reflector.Deserialize(fieldValue, fieldType, depth: depth + 1, logs: logs, logger: logger);
                field.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(e, "{padding}Failed to set field {field}", padding, fieldValue.name);
                logs?.Error($"Failed to set field {fieldValue.name}: {e.Message}", depth);
                return false;
            }
        }

        protected override bool TryPopulateProperty(
            Reflector reflector,
            ref object obj,
            Type objType,
            SerializedMember member,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (obj == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError("{padding}obj is null in TryPopulateProperty for '{property}'", padding, member.name);
                logs?.Error($"obj is null in TryPopulateProperty for '{member.name}'", depth);
                return false;
            }

            var property = objType.GetProperty(member.name, flags);
            if (property == null || !property.CanWrite)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError("{padding}Property '{property}' not found or not writable on '{type}'", padding, member.name, objType.GetTypeId());
                logs?.Error($"Property '{member.name}' not found or not writable on '{objType.GetTypeId()}'", depth);
                return false;
            }

            try
            {
                // For value types (structs) with nested fields/props, we need to:
                // 1. Get the existing value to preserve unspecified members
                // 2. Call TryPopulate on it to modify only the specified members
                // 3. Write the modified value back to the parent object
                // This prevents losing existing values when doing partial updates on structs.
                var propertyType = property.PropertyType;
                var hasNestedMembers = (member.fields != null && member.fields.Count > 0) ||
                                       (member.props != null && member.props.Count > 0);

                if (propertyType.IsValueType && !propertyType.IsPrimitive && !propertyType.IsEnum && hasNestedMembers && property.CanRead)
                {
                    // Get existing struct value (boxed)
                    var existingValue = property.GetValue(obj);
                    if (existingValue != null)
                    {
                        // Populate the existing struct with only the specified members
                        var success = reflector.TryPopulate(
                            ref existingValue,
                            data: member,
                            depth: depth + 1,
                            logs: logs,
                            flags: flags,
                            logger: logger);

                        if (success)
                        {
                            // Write the modified struct back to the parent object
                            property.SetValue(obj, existingValue);
                            return true;
                        }
                        return false;
                    }
                }

                // For reference types, primitives, enums, or full replacements, use standard deserialization
                var value = reflector.Deserialize(member, propertyType, depth: depth + 1, logs: logs, logger: logger);
                property.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(e, "{padding}Failed to set property '{property}'", padding, member.name);
                logs?.Error($"Failed to set property '{member.name}': {e.Message}", depth);
                return false;
            }
        }
    }
}
