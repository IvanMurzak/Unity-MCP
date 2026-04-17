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
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    // Unity 6.5 added `internal LoadableScene loadableScene` to Scene. Its native
    // getter crashes the Editor when invoked on certain handles (e.g. the scene
    // returned by Camera.scene during recursive reflection serialization).
    // Restrict reflection to a known-safe set of public properties.
    public partial class UnityEngine_Scene_ReflectionConverter : UnityGenericReflectionConverter<UnityEngine.SceneManagement.Scene>
    {
        static readonly HashSet<string> SafePropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(UnityEngine.SceneManagement.Scene.handle),
            nameof(UnityEngine.SceneManagement.Scene.path),
            nameof(UnityEngine.SceneManagement.Scene.name),
            nameof(UnityEngine.SceneManagement.Scene.isLoaded),
            nameof(UnityEngine.SceneManagement.Scene.buildIndex),
            nameof(UnityEngine.SceneManagement.Scene.isDirty),
            nameof(UnityEngine.SceneManagement.Scene.rootCount),
            nameof(UnityEngine.SceneManagement.Scene.isSubScene),
        };

        protected override IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return objType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => SafePropertyNames.Contains(p.Name));
        }
    }
}
