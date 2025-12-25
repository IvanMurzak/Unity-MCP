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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityArrayReflectionConverter : ArrayReflectionConverter
    {
        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
        {
            return objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null)
                .Where(field => field.IsPublic || field.IsPrivate && field.GetCustomAttribute<SerializeField>() != null);
        }
    }
}
