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
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_GameObject_ReflectionConverter : UnityGenericReflectionConverter<UnityEngine.GameObject>
    {
        public override bool AllowSetValue => false;

        protected override IEnumerable<string> GetIgnoredProperties()
        {
            foreach (var property in base.GetIgnoredProperties())
                yield return property;

            yield return nameof(UnityEngine.GameObject.gameObject);
            yield return nameof(UnityEngine.GameObject.transform);
            yield return nameof(UnityEngine.GameObject.scene);
#if UNITY_6000_3_OR_NEWER
            yield return nameof(UnityEngine.GameObject.transformHandle);
#endif
        }
        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0, Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            if (obj == null)
                return SerializedMember.Null(type, name);

            var unityObject = obj as UnityEngine.GameObject;
            if (unityObject == null)
            {
                // UnityEngine.Object is destroyed but reference is not null
                return SerializedMember.Null(type, name);
            }

            var objectRef = new GameObjectRef(unityObject?.GetInstanceID() ?? 0);

            if (recursive && depth < 1)
            {
                return new SerializedMember()
                {
                    name = name ?? unityObject?.name,
                    typeName = type.GetTypeId(),
                    fields = SerializeFields(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context),
                    props = SerializeProperties(
                        reflector,
                        obj: obj,
                        flags: flags,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context)
                }.SetValue(reflector, objectRef);
            }
            else
            {
                return SerializedMember.FromValue(
                    reflector: reflector,
                    type: type,
                    value: objectRef,
                    name: name ?? unityObject?.name);
            }
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

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}Set value type='{type.GetTypeId()}'. Converter='{GetType().GetTypeShortName()}'.");

            try
            {
                obj = value
                    .ToGameObjectRef(
                        reflector: reflector,
                        suppressException: false,
                        depth: depth,
                        logs: logs,
                        logger: logger)
                    .FindGameObject();
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
            return data.valueJsonElement
                .ToGameObjectRef(
                    reflector: reflector,
                    depth: depth,
                    logs: logs,
                    logger: logger)
                .FindGameObject();
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
                .ToGameObjectRef(
                    reflector: reflector,
                    depth: depth,
                    logs: logs,
                    logger: logger)
                .FindGameObject();
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
            try
            {
                if (obj is UnityEngine.GameObject go)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (member.name == nameof(GameObject.active))
                    {
                        go.SetActive(member.GetValue<bool>(reflector));
                        return true;
                    }
#pragma warning restore CS0618 // Type or member is obsolete
                    if (member.name == nameof(GameObject.activeSelf))
                    {
                        go.SetActive(member.GetValue<bool>(reflector));
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(e, "{padding}Failed to set property '{property}'",
                        StringUtils.GetPadding(depth), member.name);
                logs?.Error($"Failed to set property '{member.name}': {e.Message}", depth);
                return false;
            }
            return base.TryPopulateProperty(
                reflector,
                ref obj,
                objType,
                member,
                depth,
                logs,
                flags,
                logger);
        }

        public override object? CreateInstance(Reflector reflector, Type type)
        {
            return new GameObject("New GameObject");
        }
    }
}
