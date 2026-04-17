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
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityGenericNoPropertiesReflectionConverter<T> : UnityGenericReflectionConverter<T>
    {
        protected override IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return null;
        }
    }
}
