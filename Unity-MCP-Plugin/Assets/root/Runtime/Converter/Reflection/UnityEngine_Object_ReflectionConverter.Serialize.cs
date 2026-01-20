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
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Object_ReflectionConverter<T>
    {
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
                return SerializedMember.Null(type, name);

            var unityObject = obj as T;
            if (unityObject == null)
            {
                // UnityEngine.Object is destroyed but reference is not null
                return SerializedMember.Null(type, name);
            }

            if (!type.IsClass && !type.IsInterface)
                throw new ArgumentException($"Unsupported type: '{type.GetTypeId()}'. Converter: {GetType().GetTypeShortName()}");

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
                }.SetValue(reflector, new ObjectRef(unityObject));
            }
            else
            {
                var objectRef = new ObjectRef(unityObject);
                return SerializedMember.FromValue(
                    reflector: reflector,
                    type: type,
                    value: objectRef,
                    name: name ?? unityObject?.name);
            }
        }
    }
}
