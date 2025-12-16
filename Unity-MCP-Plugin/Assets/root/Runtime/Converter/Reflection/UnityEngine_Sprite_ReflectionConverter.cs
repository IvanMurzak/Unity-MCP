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
using System.Text;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Sprite_ReflectionConverter : UnityEngine_Asset_ReflectionConverter<UnityEngine.Sprite>
    {
        public override bool AllowCascadeSerialization => false;
        public override bool AllowSetValue => false;

        // protected override SerializedMember InternalSerialize(
        //     Reflector reflector,
        //     object? obj,
        //     Type type,
        //     string? name = null,
        //     bool recursive = true,
        //     BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        //     int depth = 0,
        //     Logs? logs = null,
        //     ILogger? logger = null,
        //     SerializationContext? context = null)
        // {
        //     if (obj == null)
        //         return SerializedMember.FromValue(reflector, type, value: null, name: name);

        //     if (obj is UnityEngine.Texture texture)
        //     {
        //         var objectRef = new ObjectRef(texture);
        //         return SerializedMember.FromValue(reflector, type, objectRef, name);
        //     }

        //     return base.InternalSerialize(
        //         reflector: reflector,
        //         obj: obj,
        //         type: type,
        //         name: name,
        //         recursive: recursive,
        //         flags: flags,
        //         depth: depth,
        //         logs: logs,
        //         logger: logger,
        //         context: context);
        // }
    }
}
