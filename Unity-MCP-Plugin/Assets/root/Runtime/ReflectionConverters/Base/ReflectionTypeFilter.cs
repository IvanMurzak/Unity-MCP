/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;

namespace com.IvanMurzak.Unity.MCP.Common.Reflection.Convertor
{
    /// <summary>
    /// Utility class for filtering reflection types that cause circular references during serialization.
    /// </summary>
    public static class ReflectionTypeFilter
    {
        /// <summary>
        /// Determines if a type is a System.Reflection type that should be excluded from serialization.
        /// Reflection types form circular references (RuntimeType -> RuntimeModule -> RuntimeAssembly -> RuntimeModule)
        /// and cannot be meaningfully serialized to JSON or reconstructed during deserialization.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type should be excluded from serialization; otherwise, false.</returns>
        public static bool IsReflectionType(Type type)
        {
            if (type == null) return false;

            var fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName)) return false;

            // Catch System.Reflection.* namespace types
            // Note: System.RuntimeType and System.Type are in System namespace, not System.Reflection
            return fullName.StartsWith("System.Reflection.") ||
                   fullName == "System.RuntimeType" ||
                   fullName == "System.Type";
        }
    }
}
