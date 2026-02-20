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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public class UnityEngine_Object_ReflectionConverter : UnityEngine_Object_ReflectionConverter<UnityEngine.Object> { }
    public partial class UnityEngine_Object_ReflectionConverter<T> : UnityGenericReflectionConverter<T> where T : UnityEngine.Object
    {
        public override bool AllowCascadePropertiesConversion => true;
        public override bool AllowSetValue => true;

        protected virtual IEnumerable<string> RestrictedInValuePropertyNames(Reflector reflector, JsonElement valueJsonElement) => new[]
        {
            nameof(SerializedMember.fields),
            nameof(SerializedMember.props)
        };

        protected virtual IEnumerable<string> GetKnownSerializableFields(Reflector reflector, object? obj)
            => Enumerable.Empty<string>();

        protected virtual IEnumerable<string> GetKnownSerializableProperties(Reflector reflector, object? obj)
            => Enumerable.Empty<string>();
    }
}
