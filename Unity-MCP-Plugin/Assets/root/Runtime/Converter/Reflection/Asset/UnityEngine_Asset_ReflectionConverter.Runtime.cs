/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/


#if !UNITY_EDITOR
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
            Type type,
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

            // If we reached here, we failed to find the asset.
            // Should we set obj to null? The Sprite Converter does.
            obj = null;

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}[Warning] Failed to find asset. Cleared the reference. Converter: {GetType().GetTypeShortName()}");

            logs?.Warning($"Failed to find asset. Cleared the reference. Converter: {GetType().GetTypeShortName()}", depth);

            return true;
        }
    }
}
#endif