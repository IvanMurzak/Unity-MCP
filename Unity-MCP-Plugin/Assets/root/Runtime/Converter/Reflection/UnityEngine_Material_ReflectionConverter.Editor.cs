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
#if UNITY_EDITOR
using System;
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Material_ReflectionConverter : UnityEngine_Object_ReflectionConverter<Material>
    {
        protected override bool TryPopulateProperty(
            Reflector reflector,
            ref object obj,
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
                    logger.LogError($"{padding}Object is not a 'UnityEngine.Material' or is null. Converter: {GetType().GetTypeShortName()}");

                logs?.Error($"Object is not a 'UnityEngine.Material' or is null. Converter: {GetType().GetTypeShortName()}", depth);

                return false;
            }

            var propType = TypeUtils.GetType(propertyValue.typeName);
            if (propType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}");

                logs?.Error($"Property type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                return false;
            }

            switch (propType)
            {
                case Type t when t == typeof(int):
                    if (material.HasInt(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<int>(reflector);
                        material.SetInt(propertyValue.name, value);
                        logs?.Success($"Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}", depth);
                        EditorUtility.SetDirty(material);
                        RepaintAllEditorWindows();
                        return true;
                    }

                    logs?.Error($"Property '{propertyValue.name}' with type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                    return false;

                case Type t when t == typeof(float):
                    if (material.HasFloat(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<float>(reflector);
                        material.SetFloat(propertyValue.name, value);
                        logs?.Success($"Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}", depth);
                        EditorUtility.SetDirty(material);
                        RepaintAllEditorWindows();
                        return true;
                    }

                    logs?.Error($"{padding}[Error] Property '{propertyValue.name}' with type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                    return false;

                case Type t when t == typeof(Color):
                    if (material.HasColor(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<Color>(reflector);
                        material.SetColor(propertyValue.name, value);
                        logs?.Success($"Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}", depth);
                        EditorUtility.SetDirty(material);
                        RepaintAllEditorWindows();
                        return true;
                    }

                    logs?.Error($"{padding}[Error] Property '{propertyValue.name}' with type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                    return false;

                case Type t when t == typeof(Vector4):
                    if (material.HasVector(propertyValue.name))
                    {
                        var value = propertyValue.GetValue<Vector4>(reflector);
                        material.SetVector(propertyValue.name, value);
                        logs?.Success($"Property '{propertyValue.name}' modified to '{value}'. Converter: {GetType().GetTypeShortName()}", depth);
                        EditorUtility.SetDirty(material);
                        RepaintAllEditorWindows();
                        return true;
                    }

                    logs?.Error($"{padding}[Error] Property '{propertyValue.name}' with type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                    return false;

                case Type t when t == typeof(Texture):
                    if (material.HasTexture(propertyValue.name))
                    {
                        var objTexture = propertyValue.GetValue<AssetObjectRef>(reflector).FindAssetObject();
                        var texture = objTexture as Texture;
                        material.SetTexture(propertyValue.name, texture);
                        logs?.Success($"Property '{propertyValue.name}' modified to '{texture?.name ?? "null"}'. Converter: {GetType().GetTypeShortName()}", depth);
                        EditorUtility.SetDirty(material);
                        RepaintAllEditorWindows();
                        return true;
                    }

                    logs?.Error($"Property '{propertyValue.name}' with type '{propertyValue.typeName}' not found. Converter: {GetType().GetTypeShortName()}", depth);

                    return false;

                default:
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Property type '{propertyValue.typeName}' is not supported. Supported types are: int, float, Color, Vector4, Texture. Converter: {GetType().GetTypeShortName()}");

                    logs?.Error($"Property type '{propertyValue.typeName}' is not supported. Supported types are: int, float, Color, Vector4, Texture. Converter: {GetType().GetTypeShortName()}", depth);
                    return false;
            }
        }
        private static void RepaintAllEditorWindows()
        {
            UnityEditor.EditorApplication.RepaintProjectWindow();
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            UnityEditor.EditorApplication.RepaintAnimationWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
#endif
