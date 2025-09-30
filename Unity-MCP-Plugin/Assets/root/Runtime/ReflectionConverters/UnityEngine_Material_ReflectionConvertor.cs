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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common.Model.Unity;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_Material_ReflectionConvertor : UnityEngine_Object_ReflectionConvertor<Material>
    {
        const string FieldShader = "shader";
        const string FieldName = "name";

        // public override bool AllowCascadeSerialization => false;
        public override bool AllowSetValue => false;

        protected override IEnumerable<string> RestrictedInValuePropertyNames(Reflector reflector, JsonElement valueJsonElement)
        {
            var result = base.RestrictedInValuePropertyNames(reflector, valueJsonElement).Concat(new[]
            {
                FieldShader,
                FieldName
            });
            var assetObjectRef = valueJsonElement.ToAssetObjectRef(
                reflector: null, // parse it without custom json serializer, that is why reflector is not needed
                suppressException: true
            );
            if (assetObjectRef == null)
                return result;

            var assetObject = assetObjectRef.FindAssetObject();
            if (assetObject is not Material material)
                return result;

            var shader = material.shader;
            int propertyCount = shader.GetPropertyCount();

            var restrictedProperties = Enumerable.Range(0, propertyCount)
                .Select(i => shader.GetPropertyName(i))
                .Where(propName => propName != FieldShader && propName != FieldName);

            var test = restrictedProperties.ToList();

            return result.Concat(restrictedProperties);
        }

        protected override IEnumerable<string> GetKnownSerializableFields(Reflector reflector, object? obj)
        {
            var result = base.GetKnownSerializableFields(reflector, obj);
            return result.Concat(new[]
            {
                FieldShader,
                FieldName
            });
        }
        protected override IEnumerable<string> GetKnownSerializableProperties(Reflector reflector, object? obj)
        {
            var result = base.GetKnownSerializableProperties(reflector, obj);

            if (obj is not Material material)
                return result;

            return result.Concat(GetMaterialProperties(material));
        }

        IEnumerable<string> GetMaterialProperties(Material material)
        {
            var shader = material.shader;
            int propertyCount = shader.GetPropertyCount();

            return Enumerable.Range(0, propertyCount)
                .Select(i => shader.GetPropertyName(i));
        }

        public override IEnumerable<string> GetAdditionalSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
        {
            return base.GetAdditionalSerializableFields(reflector, objType, flags, logger)
                .Concat(new[]
                {
                    FieldShader,
                    FieldName,
                });
        }

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (obj == null)
                return SerializedMember.FromValue(reflector, type, value: null, name: name);

            var padding = StringUtils.GetPadding(depth);

            var material = obj as Material;
            if (material == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}[Error] Object is not a Material. The type is {obj.GetType().GetTypeShortName()}. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Object is not a Material. The type is {obj.GetType().GetTypeShortName()}. Convertor: {GetType().GetTypeShortName()}");

                return SerializedMember.FromValue(reflector, type, value: null, name: name);
            }
            var shader = material.shader;
            int propertyCount = shader.GetPropertyCount();

            var properties = new SerializedMemberList(propertyCount);

            for (int i = 0; i < propertyCount; i++)
            {
                var propName = shader.GetPropertyName(i);
                var propType = shader.GetPropertyType(i) switch
                {
                    UnityEngine.Rendering.ShaderPropertyType.Int => typeof(int),
                    UnityEngine.Rendering.ShaderPropertyType.Float => typeof(float),
                    UnityEngine.Rendering.ShaderPropertyType.Range => typeof(float),
                    UnityEngine.Rendering.ShaderPropertyType.Color => typeof(Color),
                    UnityEngine.Rendering.ShaderPropertyType.Vector => typeof(Vector4),
                    UnityEngine.Rendering.ShaderPropertyType.Texture => typeof(Texture),
                    _ => throw new NotSupportedException($"Unsupported shader property type: '{shader.GetPropertyType(i)}'."
                        + " Supported types are: Int, Float, Range, Color, Vector, Texture.")
                };
                var propValue = shader.GetPropertyType(i) switch
                {
                    UnityEngine.Rendering.ShaderPropertyType.Int => material.GetInt(propName) as object,
                    UnityEngine.Rendering.ShaderPropertyType.Float => material.GetFloat(propName),
                    UnityEngine.Rendering.ShaderPropertyType.Range => material.GetFloat(propName),
                    UnityEngine.Rendering.ShaderPropertyType.Color => material.GetColor(propName),
                    UnityEngine.Rendering.ShaderPropertyType.Vector => material.GetVector(propName),
                    UnityEngine.Rendering.ShaderPropertyType.Texture => material.GetTexture(propName)?.GetInstanceID() != null
                        ? new ObjectRef(material.GetTexture(propName).GetInstanceID())
                        : null,
                    _ => throw new NotSupportedException($"Unsupported shader property type: '{shader.GetPropertyType(i)}'."
                        + " Supported types are: Int, Float, Range, Color, Vector, Texture.")
                };
                if (propType == null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning($"{padding}Material property '{propName}' has unsupported type '{shader.GetPropertyType(i)}'.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Warning] Material property '{propName}' has unsupported type '{shader.GetPropertyType(i)}'.");

                    continue;
                }
                properties.Add(SerializedMember.FromValue(reflector, propType, propValue, name: propName));
            }

            return new SerializedMember()
            {
                name = name,
                typeName = type.FullName,
                fields = new SerializedMemberList()
                {
                    SerializedMember.FromValue(reflector, name: FieldName, value: material.name),
                    SerializedMember.FromValue(reflector, name: FieldShader, value: shader.name)
                },
                props = properties,
            }.SetValue(reflector, new ObjectRef(material.GetInstanceID()));
        }

        public override bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? fallbackType = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            // Trying to fix JSON value body, if critical property is missed or detected return false
            if (!FixJsonValueBody(
                reflector: reflector,
                obj: ref obj,
                data: data,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                flags: flags,
                logger: logger))
            {
                return false;
            }

            if (obj is Material material)
            {
                var unityObject = data.valueJsonElement
                    .ToAssetObjectRef(
                        reflector: reflector,
                        suppressException: true,
                        stringBuilder: stringBuilder,
                        logger: logger)
                    ?.FindAssetObject();

                if (unityObject == null)
                {
                    // Recognized as a command to remove material
                    obj = null;
                    return true;
                }
                if (material.GetInstanceID() == unityObject.GetInstanceID())
                {
                    // Recognized as a command to update material
                    return base.TryPopulate(reflector, ref obj, data, fallbackType, depth, stringBuilder, flags, logger);
                }
                // Need to set new material after and maybe to populate the new material.
                var newMaterial = reflector.Deserialize(
                    data,
                    fallbackType: obj?.GetType() ?? typeof(Material),
                    fallbackName: null,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger);

                var success = base.TryPopulate(reflector, ref newMaterial, data, fallbackType, depth, stringBuilder, flags, logger);
                if (success)
                    obj = newMaterial;

                return success;
            }
            return base.TryPopulate(reflector, ref obj, data, fallbackType, depth, stringBuilder, flags, logger);
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
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{StringUtils.GetPadding(depth)}Populate field for type='{objType.GetTypeName(pretty: true)}'. Convertor='{GetType().GetTypeShortName()}'.");

            var material = obj as Material;
            if (material == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}[Error] Object is not a Material. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Object is not a Material. Convertor: {GetType().GetTypeShortName()}");

                return false;
            }

            if (fieldValue.name == FieldName)
            {
                material.name = fieldValue.GetValue<string>(reflector);

                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{padding}[Success] Material name set to '{material.name}'. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] Material name set to '{material.name}'.");

                return true;
            }
            if (fieldValue.name == FieldShader)
            {
                var shaderName = fieldValue.GetValue<string>(reflector);

                // Check if the shader is already set
                if (string.IsNullOrEmpty(shaderName) || material.shader.name == shaderName)
                {
                    if (logger?.IsEnabled(LogLevel.Information) == true)
                        logger.LogInformation($"{padding}Material '{material.name}' shader is already set to '{shaderName}'. Convertor: {GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Info] Material '{material.name}' shader is already set to '{shaderName}'.");

                    return true;
                }

                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}[Error] Shader '{shaderName}' not found. Convertor: {GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Shader '{shaderName}' not found.");

                    return false;
                }

                material.shader = shader;

                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{padding}[Success] Material '{material.name}' shader set to '{shaderName}'. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] Material '{material.name}' shader set to '{shaderName}'.");

                return true;
            }

            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}[Error] Field '{fieldValue.name}' is not supported for setting values in runtime. Convertor: {GetType().GetTypeShortName()}");

            if (stringBuilder != null)
                stringBuilder.AppendLine($"{padding}[Error] Field '{fieldValue.name}' is not supported for setting values in runtime. Convertor: {GetType().GetTypeShortName()}");

            return false;
        }

        public override object CreateInstance(Reflector reflector, Type type)
        {
            return new Material(Shader.Find("Standard"));
        }
    }
}
