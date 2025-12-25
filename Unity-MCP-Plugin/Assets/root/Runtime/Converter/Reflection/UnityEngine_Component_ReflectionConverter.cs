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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Component_ReflectionConverter : UnityEngine_Object_ReflectionConverter<UnityEngine.Component>
    {
        public override bool AllowSetValue => false;

        protected override IEnumerable<string> GetIgnoredProperties()
        {
            foreach (var property in base.GetIgnoredProperties())
                yield return property;

            yield return nameof(UnityEngine.Component.gameObject);
            yield return nameof(UnityEngine.Component.transform);
#if UNITY_6000_3_OR_NEWER
            yield return nameof(UnityEngine.Component.transformHandle);
#endif
        }
        protected override object? DeserializeValueAsJsonElement(
            Reflector reflector,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            return data.valueJsonElement
                .ToObjectRef(
                    reflector: reflector,
                    depth: depth,
                    logs: logs,
                    logger: logger)
                .FindObject() as UnityEngine.Component;
        }
    }
}
