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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using Extensions.Unity.PlayerPrefsEx;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_PlayerPrefs
    {
        public enum PlayerPrefsValueType
        {
            Int,
            Float,
            String
        }

        [McpPluginTool
        (
            "PlayerPrefs_WriteKey",
            Title = "Write PlayerPrefs key"
        )]
        [Description("Writes a value to PlayerPrefs with the specified key and type.")]
        public string WriteKey
        (
            [Description("The key to write the value to.")]
            string key,
            [Description("The value to write. For int/float, provide a number. For string, provide text.")]
            string value,
            [Description("The type of the value to write.")]
            PlayerPrefsValueType valueType
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(key))
                return Error.KeyIsNullOrEmpty();

            if (value == null)
                return Error.ValueCannotBeNull();

            try
            {
                // Delete key from all types first to ensure clean overwrite
                // PlayerPrefsEx stores each type separately, so we need to clear all
                if (PlayerPrefsEx.HasKey<int>(key))
                    PlayerPrefsEx.DeleteKey<int>(key);
                if (PlayerPrefsEx.HasKey<float>(key))
                    PlayerPrefsEx.DeleteKey<float>(key);
                if (PlayerPrefsEx.HasKey<string>(key))
                    PlayerPrefsEx.DeleteKey<string>(key);
                if (PlayerPrefsEx.HasKey<bool>(key))
                    PlayerPrefsEx.DeleteKey<bool>(key);

                switch (valueType)
                {
                    case PlayerPrefsValueType.Int:
                        if (!int.TryParse(value, out int intValue))
                            return Error.FailedToWriteValue($"Cannot parse '{value}' as int.");
                        PlayerPrefsEx.SetInt(key, intValue);
                        return $"[Success] Wrote int value '{intValue}' to key '{key}'.";

                    case PlayerPrefsValueType.Float:
                        if (!float.TryParse(value, out float floatValue))
                            return Error.FailedToWriteValue($"Cannot parse '{value}' as float.");
                        PlayerPrefsEx.SetFloat(key, floatValue);
                        return $"[Success] Wrote float value '{floatValue}' to key '{key}'.";

                    case PlayerPrefsValueType.String:
                        PlayerPrefsEx.SetString(key, value);
                        return $"[Success] Wrote string value '{value}' to key '{key}'.";

                    default:
                        return Error.InvalidValueType(valueType.ToString());
                }
            }
            catch (Exception ex)
            {
                return Error.FailedToWriteValue(ex.Message);
            }
        });
    }
}
