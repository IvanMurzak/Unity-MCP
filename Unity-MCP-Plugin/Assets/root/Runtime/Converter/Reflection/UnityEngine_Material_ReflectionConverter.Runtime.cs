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
#if !UNITY_EDITOR
using System;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Material_ReflectionConverter : UnityEngine_Object_ReflectionConverter<Material>
    {
        protected override bool TryPopulateProperty(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember propertyValue,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{StringUtils.GetPadding(depth)}PopulateProperty property='{propertyValue.name}' type='{propertyValue.typeName}'. Converter='{GetType().GetTypeShortName()}'.");

            var material = obj as Material;
            if (material == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Object is not a Material or is null. Converter: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Object is not a Material or is null. Converter: {GetType().GetTypeShortName()}");

                return false;
            }
            var propType = TypeUtils.GetType(propertyValue.typeName);
            if (propType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder?.AppendLine($"{padding}[Error] Property type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}");

                return false;
            }

            switch (propType)
            {
                case Type t when t == typeof(int):
                    if (material.HasInt(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<int>(reflector);
                        material.SetInt(propertyValue.name, value);
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}");
                        return true;
                    }
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Converter: {GetType().GetTypeShortName()}");
                    return false;
                case Type t when t == typeof(float):
                    if (material.HasFloat(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<float>(reflector);
                        material.SetFloat(propertyValue.name, value);
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}");
                        return true;
                    }
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Converter: {GetType().GetTypeShortName()}");
                    return false;
                case Type t when t == typeof(Color):
                    if (material.HasColor(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<Color>(reflector);
                        material.SetColor(propertyValue.name, value);
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}");
                        return true;
                    }
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Converter: {GetType().GetTypeShortName()}");
                    return false;
                case Type t when t == typeof(Vector4):
                    if (material.HasVector(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<Vector4>(reflector);
                        material.SetVector(propertyValue.name, value);
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}");
                        return true;
                    }
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Converter: {GetType().GetTypeShortName()}");
                    return false;
                // case Type t when t == typeof(Texture):
                //     if (material.HasTexture(property.name))
                //     {
                //         var instanceID = propertyValue.GetValue<InstanceID>()?.instanceID ?? propertyValue.GetValue<int>();
                //         var texture = instanceID == 0
                //             ? null
                //             : UnityEditor.EditorUtility.InstanceIDToObject(instanceID) as Texture;
                //         material.SetTexture(propertyValue.name, texture);
                //         return stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{texture?.name ?? "null"}'.");
                //     }
                //     return stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found.");
                default:
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Property type '{propertyValue.typeName}' is not supported. Converter: {GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property type '{propertyValue.typeName}' is not supported. Converter: {GetType().GetTypeShortName()}");

                    return false;
            }
        }
    }
}
#endif
