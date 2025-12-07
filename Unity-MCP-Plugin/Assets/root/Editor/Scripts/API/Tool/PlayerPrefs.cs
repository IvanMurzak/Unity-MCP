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
using com.IvanMurzak.McpPlugin;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_PlayerPrefs
    {
        public static class Error
        {
            public static string KeyIsNullOrEmpty()
                => "[Error] Key cannot be null or empty.";

            public static string KeyDoesNotExist(string key)
                => $"[Error] Key '{key}' does not exist.";

            public static string ValueCannotBeNull()
                => "[Error] Value cannot be null.";

            public static string ValueTypeMustBeSpecified()
                => "[Error] Value type must be specified ('int', 'float', or 'string').";

            public static string InvalidValueType(string valueType)
                => $"[Error] Invalid value type '{valueType}'. Must be 'int', 'float', or 'string'.";

            public static string FailedToWriteValue(string message)
                => $"[Error] Failed to write value: {message}";
        }

        /// <summary>
        /// Determines the value and type of a PlayerPrefs key.
        /// Since PlayerPrefs doesn't expose the type, we try to infer it.
        /// </summary>
        private static (object value, string type) GetKeyValueAndType(string key)
        {
            // First, try to get as string
            string stringValue = PlayerPrefs.GetString(key, string.Empty);

            if (!string.IsNullOrEmpty(stringValue))
            {
                // Try to parse as int
                if (int.TryParse(stringValue, out int intResult))
                {
                    int intValue = PlayerPrefs.GetInt(key, int.MaxValue);
                    if (intValue != int.MaxValue && intValue == intResult)
                        return (intValue, "int");
                }

                // Try to parse as float
                if (float.TryParse(stringValue, out float floatResult))
                {
                    float floatValue = PlayerPrefs.GetFloat(key, float.NaN);
                    if (!float.IsNaN(floatValue) && Math.Abs(floatValue - floatResult) < 0.0001f)
                        return (floatValue, "float");
                }

                return (stringValue, "string");
            }

            // If string is empty, try int
            int intVal = PlayerPrefs.GetInt(key, int.MinValue + 1);
            if (intVal != int.MinValue + 1)
                return (intVal, "int");

            // Try float
            float floatVal = PlayerPrefs.GetFloat(key, float.NaN);
            if (!float.IsNaN(floatVal))
                return (floatVal, "float");

            // Default to empty string
            return (PlayerPrefs.GetString(key, string.Empty), "string");
        }
    }
}
