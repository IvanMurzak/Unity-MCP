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
using com.IvanMurzak.ReflectorNet.Converter;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Collider_ReflectionConverter : LazyReflectionConverter
    {
        public UnityEngine_Collider_ReflectionConverter(UnityEngine_GenericComponent_ReflectionConverter<UnityEngine.Component> backingConverter)
            : base(
                targetTypeName: "UnityEngine.Collider",
                ignoredProperties: null,
                ignoredFields: new string[]
                {
                    "material" // nameof(UnityEngine.Collider.material)
                },
                backingConverter: backingConverter)
        {
            // empty
        }
    }
}
