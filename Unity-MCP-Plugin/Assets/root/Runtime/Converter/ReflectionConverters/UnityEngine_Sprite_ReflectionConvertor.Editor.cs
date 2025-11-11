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
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using UnityEditor;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_Sprite_ReflectionConvertor : UnityEngine_Object_ReflectionConvertor<UnityEngine.Sprite>
    {
        public override bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? dataType = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{StringUtils.GetPadding(depth)}Populate sprite from data. Convertor='{GetType().GetTypeShortName()}'.");

            if (!data.TryGetInstanceID(out var instanceID))
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}InstanceID not found. Set 'instanceID` as 0 if you want to set it to null. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] InstanceID not found. Set 'instanceID` as 0 if you want to set it to null. Convertor: {GetType().GetTypeShortName()}");

                return false;
            }
            if (instanceID == 0)
            {
                obj = null;

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}[Success] InstanceID is 0. Cleared the reference. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] InstanceID is 0. Cleared the reference. Convertor: {GetType().GetTypeShortName()}");

                return true;
            }
            var textureOrSprite = EditorUtility.InstanceIDToObject(instanceID);
            if (textureOrSprite == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}InstanceID {instanceID} not found. Convertor: {GetType().GetTypeShortName()}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] InstanceID {instanceID} not found. Convertor: {GetType().GetTypeShortName()}");

                return false;
            }

            if (textureOrSprite is UnityEngine.Texture2D texture)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                    .OfType<UnityEngine.Sprite>()
                    .ToArray();
                if (sprites.Length == 0)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}No sprites found for texture at path: {path}. Convertor: {GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] No sprites found for texture at path: {path}. Convertor: {GetType().GetTypeShortName()}");

                    return false;
                }

                obj = sprites[0]; // Assign the first sprite found

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] Assigned sprite from texture: {path}. Convertor: {GetType().GetTypeShortName()}");

                return true;
            }
            if (textureOrSprite is UnityEngine.Sprite sprite)
            {
                obj = sprite;

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] Assigned sprite: {sprite.name}. Convertor: {GetType().GetTypeShortName()}");

                return true;
            }

            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}InstanceID {instanceID} is not a Texture2D or Sprite. Convertor: {GetType().GetTypeShortName()}");

            if (stringBuilder != null)
                stringBuilder.AppendLine($"{padding}[Error] InstanceID {instanceID} is not a Texture2D or Sprite. Convertor: {GetType().GetTypeShortName()}");

            return false;
        }
    }
}
#endif
