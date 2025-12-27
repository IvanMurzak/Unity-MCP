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
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public class UnityEngine_Object_ReflectionConverter : UnityEngine_Object_ReflectionConverter<UnityEngine.Object> { }
    public partial class UnityEngine_Object_ReflectionConverter<T> : UnityGenericReflectionConverter<T> where T : UnityEngine.Object
    {
        public override bool AllowCascadePropertiesConversion => false;
        public override bool AllowSetValue => true;

        protected virtual IEnumerable<string> RestrictedInValuePropertyNames(Reflector reflector, JsonElement valueJsonElement) => new[]
        {
            nameof(SerializedMember.fields),
            nameof(SerializedMember.props)
        };

        protected virtual IEnumerable<string> GetKnownSerializableFields(Reflector reflector, object? obj)
            => Enumerable.Empty<string>();

        protected virtual IEnumerable<string> GetKnownSerializableProperties(Reflector reflector, object? obj)
            => Enumerable.Empty<string>();

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            // UnityEngine.Debug.LogWarning($"Serialize: {name}, Type: {(obj?.GetType() ?? type).GetTypeId()}, Obj is null? {obj == null}.\nPath: {context?.BuildCurrentPath()}");

            if (obj == null)
                return SerializedMember.FromValue(reflector, type, value: null, name: name);

            var unityObject = obj as T;
            if (unityObject == null)
            {
                // UnityEngine.Object is destroyed but reference is not null
                return SerializedMember.FromValue(reflector, type, value: null, name: name);
            }

            if (!type.IsClass && !type.IsInterface)
                throw new ArgumentException($"Unsupported type: '{type.GetTypeId()}'. Converter: {GetType().GetTypeShortName()}");

            if (recursive)
            {
                return new SerializedMember()
                {
                    name = name ?? unityObject?.name,
                    typeName = type.GetTypeId(),
                    fields = SerializeFields(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth,
                        logs: logs,
                        logger: logger,
                        context: context),
                    props = SerializeProperties(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth,
                        logs: logs,
                        logger: logger,
                        context: context)
                }.SetValue(reflector, new ObjectRef(unityObject));
            }
            else
            {
                var objectRef = new ObjectRef(unityObject);
                return SerializedMember.FromValue(reflector, type, objectRef, name);
            }
        }

        public override bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            logs?.Info($"TryPopulate called for type '{obj?.GetType().Name}'.", depth);

            // Trying to fix JSON value body, if critical property is missed or detected return false
            if (!FixJsonValueBody(
                reflector: reflector,
                obj: ref obj,
                data: data,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                flags: flags,
                logger: logger))
            {
                return false;
            }
            return base.TryPopulate(
                reflector: reflector,
                obj: ref obj,
                data: data,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                flags: flags,
                logger: logger);
        }

        protected virtual bool FixJsonValueBody(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (data?.valueJsonElement == null)
                return true;

            if (data.valueJsonElement.Value.ValueKind != JsonValueKind.Object)
                return true;

            // Look for restricted properties
            var isRestricted = data.valueJsonElement.Value.EnumerateObject()
                .Any(jsonElement => RestrictedInValuePropertyNames(reflector, data.valueJsonElement.Value)
                    .Any(name => name == jsonElement.Name));

            if (!isRestricted)
                return true;

            var node = JsonNode.Parse(data.valueJsonElement.Value.GetRawText())?.AsObject();
            if (node == null)
                return true;

            foreach (var knownField in GetKnownSerializableFields(reflector, obj))
            {
                if (node.TryGetPropertyValue(knownField, out var value) && value != null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning($"{StringUtils.GetPadding(depth)}'{knownField}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.");

                    logs?.Warning($"'{knownField}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.", depth);

                    // handle known field
                    data.fields ??= new SerializedMemberList();
                    data.fields.Add(SerializedMember.FromValue(reflector, name: knownField, value: value));
                    node.Remove(knownField);
                }
            }
            foreach (var knownProperty in GetKnownSerializableProperties(reflector, obj))
            {
                if (node.TryGetPropertyValue(knownProperty, out var value) && value != null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning($"{StringUtils.GetPadding(depth)}'{knownProperty}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.");

                    logs?.Warning($"'{knownProperty}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.", depth);

                    // handle known property
                    data.props ??= new SerializedMemberList();
                    data.props.Add(SerializedMember.FromValue(reflector, name: knownProperty, value: value));
                    node.Remove(knownProperty);
                }
            }

            foreach (var restrictedPropertyName in RestrictedInValuePropertyNames(reflector, data.valueJsonElement.Value))
            {
                if (node.TryGetPropertyValue(restrictedPropertyName, out var restrictedValue) && restrictedValue != null)
                {
                    if (restrictedPropertyName == nameof(SerializedMember.fields))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{StringUtils.GetPadding(depth)}'{restrictedPropertyName}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.");

                        logs?.Warning($"'{restrictedPropertyName}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.", depth);

                        // handle 'fields' property
                        data.fields ??= new SerializedMemberList();
                        data.fields.AddRange(restrictedValue.Deserialize<SerializedMemberList>(reflector.JsonSerializerOptions));
                        node.Remove(restrictedPropertyName);
                    }
                    else if (restrictedPropertyName == nameof(SerializedMember.props))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{StringUtils.GetPadding(depth)}'{restrictedPropertyName}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.");

                        logs?.Warning($"'{restrictedPropertyName}' should be moved from '{SerializedMember.ValueName}'. Fixing the hierarchy automatically.", depth);

                        // handle 'props' property
                        data.props ??= new SerializedMemberList();
                        data.props.AddRange(restrictedValue.Deserialize<SerializedMemberList>(reflector.JsonSerializerOptions));
                        node.Remove(restrictedPropertyName);
                    }
                    else
                    {
                        // // Need to take list of serializable Fields for the specific object
                        // // if the `restrictedPropertyName` is a field, move into `fields`
                        // // if the `restrictedPropertyName` is a property, move into `props`
                        // // if none of the conditions matches
                        // data.fields ??= new SerializedMemberList();
                        // data.fields.Add(SerializedMember.FromValue(reflector, name: restrictedPropertyName, value: restrictedValue));
                        // node.Remove(restrictedPropertyName);

                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{StringUtils.GetPadding(depth)}Restricted property '{restrictedPropertyName}' found in '{SerializedMember.ValueName}'.");

                        logs?.Error($"Restricted property '{restrictedPropertyName}' found in '{SerializedMember.ValueName}'.", depth);

                        // If we found another restricted property, we need to stop processing
                        return false;
                    }
                }
            }

            // Update json value to the updated json
            data.valueJsonElement = node.ToJsonElement();
            return true;
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            logs?.Info($"SetValue called for type '{type.Name}'. Value kind: {value?.ValueKind}", depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}Set value type='{type.GetTypeId()}'. Converter='{GetType().GetTypeShortName()}'.");

            try
            {
                var assetObj = value
                    .ToAssetObjectRef(
                        reflector: reflector,
                        suppressException: false,
                        depth: depth,
                        logs: logs,
                        logger: logger)
                    .FindAssetObject(type);

                obj = assetObj;

                logs?.Info($"SetValue success. Obj is null? {obj == null}", depth);

                return true;
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(ex, $"{padding}[Error] Failed to deserialize value for type '{type.GetTypeId()}'. Converter: {GetType().GetTypeShortName()}. Exception: {ex.Message}");

                logs?.Error($"Failed to set value for type '{type.GetTypeId()}'. Converter: {GetType().GetTypeShortName()}. Exception: {ex.Message}", depth);

                return false;
            }
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
            var targetType = fallbackType ?? typeof(T);
            var padding = StringUtils.GetPadding(depth);
            if (logger?.IsEnabled(LogLevel.Information) == true)
                logger.LogInformation($"{padding}[UnityEngine_Object_ReflectionConverter] Deserialize called for {targetType.GetTypeId()}. Converter: {GetType().GetTypeShortName()}");

            logs?.Info($"Deserialize called for '{targetType.GetTypeId()}'. Converter: {GetType().GetTypeShortName()}", depth);

            if (!TryDeserializeValue(
                reflector,
                data: data,
                result: out var result,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                return result;
            }
            // Register the object early (before deserializing children) so child references can resolve
            if (result != null && context != null)
                context.Register(result);

            return data.valueJsonElement
                .ToAssetObjectRef(
                    reflector: reflector,
                    depth: depth,
                    logs: logs,
                    logger: logger)
                .FindAssetObject(targetType);
        }

        protected override object? DeserializeValueAsJsonElement(
            Reflector reflector,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            return data.valueJsonElement
                .ToAssetObjectRef(
                    reflector: reflector,
                    depth: depth,
                    logs: logs,
                    logger: logger)
                .FindAssetObject(type);
        }
    }
}
