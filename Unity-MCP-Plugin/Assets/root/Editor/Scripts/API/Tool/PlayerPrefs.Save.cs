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
            "PlayerPrefs_Save",
            Title = "Save PlayerPrefs to disk"
        )]
        [Description("Writes all modified PlayerPrefs to disk. On some platforms, PlayerPrefs are not saved until the application quits. Use this to force save.")]
        public string Save()
        => MainThread.Instance.Run(() =>
        {
            PlayerPrefsEx.Save();
            return "[Success] PlayerPrefs saved to disk.";
        });
    }
}
