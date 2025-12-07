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
            "PlayerPrefs_ExistsKey",
            Title = "Check if PlayerPrefs key exists"
        )]
        [Description("Checks whether a specified key exists in Unity's PlayerPrefs.")]
        public string ExistsKey
        (
            [Description("The key to check for existence.")]
            string key
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(key))
                return Error.KeyIsNullOrEmpty();

            bool exists = PlayerPrefs.HasKey(key);
            return $"[Success] Key '{key}' existence check completed. Exists: {exists}";
        });
    }
}
