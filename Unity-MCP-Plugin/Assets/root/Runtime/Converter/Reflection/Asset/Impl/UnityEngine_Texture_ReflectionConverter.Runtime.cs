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
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Texture_ReflectionConverter : UnityEngine_Asset_ReflectionConverter<UnityEngine.Texture>
    {
        // public override bool TryPopulate(
        //     Reflector reflector,
        //     ref object? obj,
        //     SerializedMember data,
        //     Type type,
        //     int depth = 0,
        //     Logs? logs = null,
        //     BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        //     ILogger? logger = null)
        // {
        //     var padding = StringUtils.GetPadding(depth);

        //     if (logger?.IsEnabled(LogLevel.Trace) == true)
        //         logger.LogTrace($"{StringUtils.GetPadding(depth)}Populate sprite from data. Converter='{GetType().GetTypeShortName()}'.");

        //     if (logger?.IsEnabled(LogLevel.Error) == true)
        //         logger.LogError($"{padding}Operation is not supported in runtime. Converter: {GetType().GetTypeShortName()}");

        //     if (stringBuilder != null)
        //         stringBuilder.AppendLine($"{padding}[Error] Operation is not supported in runtime. Converter: {GetType().GetTypeShortName()}");

        //     return false;
        // }
    }
}
#endif
