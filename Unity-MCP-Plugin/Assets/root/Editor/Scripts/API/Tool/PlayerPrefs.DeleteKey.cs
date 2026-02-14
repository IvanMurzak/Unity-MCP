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
using Extensions.Unity.PlayerPrefsEx;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_PlayerPrefs
    {
        [McpPluginTool
        (
            "PlayerPrefs_DeleteKey",
            Title = "Delete PlayerPrefs key"
        )]
        [Description("Deletes a key and its value from PlayerPrefs.")]
        public string DeleteKey
        (
            [Description("The key to delete.")]
            string key
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(key))
                return Error.KeyIsNullOrEmpty();

            // Check if key exists in any type and delete it
            bool deleted = false;
            
            if (PlayerPrefsEx.HasKey<int>(key))
            {
                PlayerPrefsEx.DeleteKey<int>(key);
                deleted = true;
            }
            
            if (PlayerPrefsEx.HasKey<float>(key))
            {
                PlayerPrefsEx.DeleteKey<float>(key);
                deleted = true;
            }
            
            if (PlayerPrefsEx.HasKey<string>(key))
            {
                PlayerPrefsEx.DeleteKey<string>(key);
                deleted = true;
            }
            
            if (PlayerPrefsEx.HasKey<bool>(key))
            {
                PlayerPrefsEx.DeleteKey<bool>(key);
                deleted = true;
            }
            
            if (!deleted)
                return Error.KeyDoesNotExist(key);

            return $"[Success] Deleted key '{key}' from PlayerPrefs.";
        });
    }
}
