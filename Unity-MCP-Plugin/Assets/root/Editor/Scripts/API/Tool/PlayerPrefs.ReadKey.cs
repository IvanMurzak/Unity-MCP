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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_PlayerPrefs
    {
        [McpPluginTool
        (
            "PlayerPrefs_ReadKey",
            Title = "Read PlayerPrefs key"
        )]
        [Description("Reads the value of a key from PlayerPrefs. Returns the value and its inferred type.")]
        public string ReadKey
        (
            [Description("The key to read the value from.")]
            string key
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(key))
                return Error.KeyIsNullOrEmpty();

            if (!PlayerPrefs.HasKey(key))
                return Error.KeyDoesNotExist(key);

            var (value, type) = GetKeyValueAndType(key);
            return $"[Success] Key '{key}' value: {value} (type: {type})";
        });
    }
}
