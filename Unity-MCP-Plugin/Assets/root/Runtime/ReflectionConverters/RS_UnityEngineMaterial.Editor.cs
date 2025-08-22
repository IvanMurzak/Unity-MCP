/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Model.Unity;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEngine;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class RS_UnityEngineMaterial : RS_UnityEngineObject<Material>
    {
        protected override bool TryPopulateProperty(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember propertyValue,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{StringUtils.GetPadding(depth)}PopulateProperty property='{propertyValue.name}' type='{propertyValue.typeName}'. Convertor='{GetType().GetTypeShortName()}'.");

            var material = obj as Material;
            var propType = TypeUtils.GetType(propertyValue.typeName);
            if (propType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property type '{propertyValue.typeName}' not found. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property type '{propertyValue.typeName}' not found. Convertor: {GetType().GetTypeShortName()}");

                return false;
            }

            switch (propType)
            {
                case Type t when t == typeof(int):
                    if (material.HasInt(propertyValue.name))
                    {
                        material.SetInt(propertyValue.name, propertyValue.GetValue<int>(reflector));
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{propertyValue.GetValue<int>(reflector)}'. Convertor: {GetType().GetTypeShortName()}");
                        return true;
                    }

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Convertor: {GetType().GetTypeShortName()}");

                    return false;

                case Type t when t == typeof(float):
                    if (material.HasFloat(propertyValue.name))
                    {
                        material.SetFloat(propertyValue.name, propertyValue.GetValue<float>(reflector));
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{propertyValue.GetValue<float>(reflector)}'. Convertor: {GetType().GetTypeShortName()}");
                        return true;
                    }

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Convertor: {GetType().GetTypeShortName()}");

                    return false;

                case Type t when t == typeof(Color):
                    if (material.HasColor(propertyValue.name))
                    {
                        material.SetColor(propertyValue.name, propertyValue.GetValue<Color>(reflector));
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{propertyValue.GetValue<Color>(reflector)}'. Convertor: {GetType().GetTypeShortName()}");
                        return true;
                    }

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Convertor: {GetType().GetTypeShortName()}");

                    return false;

                case Type t when t == typeof(Vector4):
                    if (material.HasVector(propertyValue.name))
                    {
                        material.SetVector(propertyValue.name, propertyValue.GetValue<Vector4>(reflector));
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{propertyValue.GetValue<Vector4>(reflector)}'. Convertor: {GetType().GetTypeShortName()}");
                        return true;
                    }

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Convertor: {GetType().GetTypeShortName()}");

                    return false;

                case Type t when t == typeof(Texture):
                    if (material.HasTexture(propertyValue.name))
                    {
                        var objTexture = propertyValue.GetValue<AssetObjectRef>(reflector).FindAssetObject();
                        var texture = objTexture as Texture;
                        material.SetTexture(propertyValue.name, texture);
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Property '{propertyValue.name}' modified to '{texture?.name ?? "null"}'. Convertor: {GetType().GetTypeShortName()}");
                        return true;
                    }

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name}' not found. Convertor: {GetType().GetTypeShortName()}");

                    return false;

                default:
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Property type '{propertyValue.typeName}' is not supported. Supported types are: int, float, Color, Vector4, Texture. Convertor: {GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property type '{propertyValue.typeName}' is not supported. Supported types are: int, float, Color, Vector4, Texture. Convertor: {GetType().GetTypeShortName()}");
                    return false;
            }
        }
    }
}
#endif
