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
using com.IvanMurzak.McpPlugin.Common.Reflection.Convertor;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_GameObject_ReflectionConvertor : UnityGenericReflectionConvertor<UnityEngine.GameObject>
    {
        const string ComponentNamePrefix = "component_";
        static string GetComponentName(int index) => $"{ComponentNamePrefix}{index}";
        static bool TryParseComponentIndex(string? name, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(ComponentNamePrefix))
                return false;

            var indexStr = name.Substring(ComponentNamePrefix.Length).Trim('[', ']');
            return int.TryParse(indexStr, out index);
        }

        public override bool AllowSetValue => false;

        protected override IEnumerable<string> GetIgnoredProperties()
        {
            foreach (var property in base.GetIgnoredProperties())
                yield return property;

            yield return nameof(UnityEngine.GameObject.gameObject);
            yield return nameof(UnityEngine.GameObject.transform);
            yield return nameof(UnityEngine.GameObject.scene);
        }
        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0, StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (obj == null)
                return SerializedMember.FromValue(reflector, type, value: null, name: name);

            var unityObject = obj as UnityEngine.GameObject;
            if (recursive)
            {
                return new SerializedMember()
                {
                    name = name,
                    typeName = type.FullName,
                    fields = SerializeFields(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        logger: logger),
                    props = SerializeProperties(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        logger: logger)
                }.SetValue(reflector, new GameObjectRef(unityObject?.GetInstanceID() ?? 0));
            }
            else
            {
                var objectRef = new GameObjectRef(unityObject?.GetInstanceID() ?? 0);
                return SerializedMember.FromValue(reflector, type, objectRef, name);
            }
        }

        protected override SerializedMemberList SerializeFields(
            Reflector reflector,
            object obj,
            BindingFlags flags,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var serializedFields = base.SerializeFields(
                reflector: reflector,
                obj: obj,
                flags: flags,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger) ?? new();

            var go = obj as UnityEngine.GameObject;
            if (go == null)
                throw new ArgumentException("Object is not a GameObject.", nameof(obj));
            var components = go.GetComponents<UnityEngine.Component>();

            serializedFields.Capacity += components.Length;

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var componentSerialized = reflector.Serialize(
                    obj: component,
                    fallbackType: component?.GetType() ?? typeof(UnityEngine.Component),
                    name: GetComponentName(i),
                    recursive: true,
                    flags: flags,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    logger: logger
                );
                serializedFields.Add(componentSerialized);
            }
            return serializedFields;
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}Set value type='{type.GetTypeName(pretty: true)}'. Convertor='{GetType().GetTypeShortName()}'.");

            try
            {
                obj = value
                    .ToGameObjectRef(
                        reflector: reflector,
                        suppressException: false,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        logger: logger)
                    .FindGameObject();
                return true;
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(ex, $"{padding}[Error] Failed to deserialize value for type '{type.GetTypeName(pretty: false)}'. Convertor: {GetType().GetTypeShortName()}. Exception: {ex.Message}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to set value for type '{type.GetTypeName(pretty: false)}'. Convertor: {GetType().GetTypeShortName()}. Exception: {ex.Message}");

                return false;
            }
        }

        protected override bool TryPopulateField(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember fieldValue,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (logger?.IsEnabled(LogLevel.Information) == true)
                logger.LogInformation($"[UnityEngine_GameObject_ReflectionConvertor] TryPopulateField called for obj type: {obj?.GetType().FullName}, field: {fieldValue.name}");

            var padding = StringUtils.GetPadding(depth);
            var go = obj as UnityEngine.GameObject;
            if (go == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}[Error] Object is not a GameObject.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Object is not a GameObject.");

                return false;
            }

            // it is fine if type is unknown here, we will try to find the component by name or index
            var type = TypeUtils.GetType(fieldValue.typeName);

            int? instanceID = fieldValue.TryGetGameObjectInstanceID(out var tempInstanceID)
                ? tempInstanceID
                : null;

            int? index = TryParseComponentIndex(fieldValue.name, out var tempIndex)
                ? tempIndex
                : null;

            var component = GetComponent(
                go: go,
                instanceID: instanceID,
                index: index,
                typeName: type,
                error: out var error);
            if (component == null)
            {
                if (type == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}[Error] Type not found for field '{fieldValue.name}' with type name '{fieldValue.typeName}'.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Type not found: '{fieldValue.typeName}'");

                    return false;
                }

                // If not a component, use base method
                if (!typeof(UnityEngine.Component).IsAssignableFrom(type))
                {
                    return base.TryPopulateField(
                        reflector,
                        obj: ref obj,
                        objType: objType,
                        fieldValue: fieldValue,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        flags: flags,
                        logger: logger);
                }

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}[Error] {error}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] {error}");

                return false;
            }

            var componentObject = (object)component;
            return reflector.TryPopulate(
                obj: ref componentObject,
                data: fieldValue,
                fallbackObjType: type,
                depth: depth,
                flags: flags,
                stringBuilder: stringBuilder,
                logger: logger);
        }

        protected virtual UnityEngine.Component? GetComponent(
            UnityEngine.GameObject go,
            int? instanceID,
            int? index,
            Type? typeName,
            out string? error)
        {
            var allComponents = go.GetComponents<UnityEngine.Component>();
            if (instanceID.HasValue && instanceID.Value != 0)
            {
                var component = allComponents.FirstOrDefault(c => c.GetInstanceID() == instanceID.Value);
                if (component != null)
                {
                    error = null;
                    return component;
                }
                error = $"Component with {ObjectRef.ObjectRefProperty.InstanceID}='{instanceID.Value}' not found.";
                return null;
            }
            if (index.HasValue)
            {
                if (index < 0 || index >= allComponents.Length)
                {
                    error = $"Component with {ComponentRef.ComponentRefProperty.Index}='{index.Value}' not found. Index is out of range.";
                    return null;
                }
                error = null;
                return allComponents[index.Value];
            }
            if (typeName != null)
            {
                var component = allComponents.FirstOrDefault(c => c.GetType() == typeName);
                if (component != null)
                {
                    error = null;
                    return component;
                }
                error = $"Component of type '{typeName.GetTypeName(pretty: false)}' not found.";
                return null;
            }
            error = $"No valid criteria provided to find the component. Use '{ObjectRef.ObjectRefProperty.InstanceID}', '{ComponentRef.ComponentRefProperty.Index}', or '{ComponentRef.ComponentRefProperty.TypeName}'.";
            return null;
        }

        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            return data.valueJsonElement
                .ToGameObjectRef(
                    reflector: reflector,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger)
                .FindGameObject();
        }

        protected override object? DeserializeValueAsJsonElement(
            Reflector reflector,
            SerializedMember data,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            return data.valueJsonElement
                .ToGameObjectRef(
                    reflector: reflector,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger)
                .FindGameObject();
        }

        public override object? CreateInstance(Reflector reflector, Type type)
        {
            return new GameObject("New GameObject");
        }
    }
}
