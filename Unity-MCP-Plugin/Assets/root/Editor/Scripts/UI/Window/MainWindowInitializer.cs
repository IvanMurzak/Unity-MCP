/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using Extensions.Unity.PlayerPrefsEx;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    static class MainWindowInitializer
    {
        static PlayerPrefsBool isInitialized = new PlayerPrefsBool("Unity-MCP.MainWindow.Initialized");

        static MainWindowInitializer()
        {
            if (isInitialized.Value)
                return;

            EditorApplication.update += PerformInitialization;
        }

        static void PerformInitialization()
        {
            if (isInitialized.Value)
                return;

            // Perform initialization
            McpPluginUnity.Init();
            MainWindowEditor.ShowWindow();

            isInitialized.Value = true;
        }
    }
}