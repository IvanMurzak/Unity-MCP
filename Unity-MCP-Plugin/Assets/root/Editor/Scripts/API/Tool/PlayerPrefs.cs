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
using Extensions.Unity.PlayerPrefsEx;
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
        /// Determines the value and type of a PlayerPrefsEx key.
        /// PlayerPrefsEx maintains type information, so we can check each type directly.
        /// </summary>
        private static (object value, string type) GetKeyValueAndType(string key)
        {
            // PlayerPrefsEx maintains type information, so we check each type
            // Try int first
            if (PlayerPrefsEx.HasKey<int>(key))
            {
                int intValue = PlayerPrefsEx.GetInt(key, 0);
                return (intValue, "int");
            }

            // Try float
            if (PlayerPrefsEx.HasKey<float>(key))
            {
                float floatValue = PlayerPrefsEx.GetFloat(key, 0f);
                return (floatValue, "float");
            }

            // Try string
            if (PlayerPrefsEx.HasKey<string>(key))
            {
                string stringValue = PlayerPrefsEx.GetString(key, string.Empty);
                return (stringValue, "string");
            }

            // Try bool (PlayerPrefsEx supports bool)
            if (PlayerPrefsEx.HasKey<bool>(key))
            {
                bool boolValue = PlayerPrefsEx.GetBool(key, false);
                return (boolValue, "bool");
            }

            // Default to empty string if key doesn't exist
            return (string.Empty, "string");
        }
    }
}
