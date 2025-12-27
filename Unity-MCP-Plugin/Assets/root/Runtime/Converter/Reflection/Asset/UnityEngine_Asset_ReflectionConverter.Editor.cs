/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if UNITY_EDITOR
#nullable enable
using System;
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Asset_ReflectionConverter<T> : UnityEngine_Object_ReflectionConverter<T> where T : UnityEngine.Object
    {
        public override bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? dataType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}Populate asset from data. Converter='{GetType().GetTypeShortName()}'.");

            var objectRef = data.valueJsonElement.ToAssetObjectRef(
                reflector: reflector,
                depth: depth,
                logs: logs,
                logger: logger);

            if (objectRef == null)
            {
                // If no object ref, maybe we should fall back to base behavior?
                // But for assets, usually we expect an object ref.
                // Let's return false to indicate we couldn't populate it as an asset.
                return false;
            }

            var instanceID = objectRef.InstanceID;
            if (instanceID != 0)
            {
                var loadedObj = LoadFromInstanceID(instanceID);
                if (loadedObj != null)
                {
                    obj = loadedObj;
                    logs?.Success($"Assigned asset from InstanceID: '{instanceID}'. Converter: {GetType().GetTypeShortName()}", depth);
                    return true;
                }

                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}InstanceID '{instanceID}' found but failed to load asset. Converter: {GetType().GetTypeShortName()}");
            }

            if (!string.IsNullOrEmpty(objectRef.AssetPath))
            {
                var loadedObj = LoadFromAssetPath(objectRef.AssetPath);
                if (loadedObj != null)
                {
                    obj = loadedObj;
                    logs?.Success($"Set asset from AssetPath: '{objectRef.AssetPath}'. Converter: {GetType().GetTypeShortName()}", depth);
                    return true;
                }

                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}AssetPath '{objectRef.AssetPath}' found but failed to load asset. Converter: {GetType().GetTypeShortName()}");
            }

            if (!string.IsNullOrEmpty(objectRef.AssetGuid))
            {
                var loadedObj = LoadFromAssetGuid(objectRef.AssetGuid);
                if (loadedObj != null)
                {
                    obj = loadedObj;
                    logs?.Success($"Set asset from AssetGuid: '{objectRef.AssetGuid}'. Converter: {GetType().GetTypeShortName()}", depth);
                    return true;
                }

                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}AssetGuid '{objectRef.AssetGuid}' found but failed to load asset. Converter: {GetType().GetTypeShortName()}");
            }

            // If we reached here, we failed to find the asset.
            // Should we set obj to null? The Sprite Converter does.
            obj = null;

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}[Warning] Failed to find asset. Cleared the reference. Converter: {GetType().GetTypeShortName()}");

            logs?.Warning($"Failed to find asset. Cleared the reference. Converter: {GetType().GetTypeShortName()}", depth);

            return true;
        }

        protected virtual T? LoadFromInstanceID(int instanceID)
        {
#if UNITY_6000_3_OR_NEWER
            var obj = UnityEditor.EditorUtility.EntityIdToObject(instanceID);
#else
            var obj =  UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
#endif
            return obj as T;
        }

        protected virtual T? LoadFromAssetPath(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        protected virtual T? LoadFromAssetGuid(string guid)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;
            return LoadFromAssetPath(path);
        }
    }
}
#endif
