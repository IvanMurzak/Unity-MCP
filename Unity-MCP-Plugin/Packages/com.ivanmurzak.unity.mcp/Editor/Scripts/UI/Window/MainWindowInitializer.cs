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
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Extensions.Unity.PlayerPrefsEx;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    [InitializeOnLoad]
    static class MainWindowInitializer
    {
        static PlayerPrefsBool isInitialized;

        static MainWindowInitializer()
        {
            if (McpDisable.IsDisabled)
                return;

            isInitialized = new PlayerPrefsBool("Unity-MCP.MainWindow.Initialized");

            // An asset import worker shares these prefs but has no window to open; opening it would connect (#1050).
            if (AssetDatabase.IsAssetImportWorkerProcess() || isInitialized.Value)
                return;

            EditorApplication.update += PerformInitialization;
        }

        static void PerformInitialization()
        {
            EditorApplication.update -= PerformInitialization;

            if (isInitialized.Value)
                return;

            // A batch-mode editor has no window to show; open it on the first interactive start instead.
            if (Application.isBatchMode)
                return;

            UnityMcpPluginEditor.InitSingletonIfNeeded();

            var configuredExternally = IsConfiguredExternally(
                hasConnectionOverride: EnvironmentUtils.HasConnectionOverride(),
                isCliSession: EnvironmentUtils.IsCliSession(ArgsUtils.ParseCommandLineArguments(), System.Environment.GetEnvironmentVariable),
                configCreatedThisSession: UnityMcpPluginEditor.ConfigCreatedThisSession);
            if (!configuredExternally)
                MainWindowEditor.ShowWindow();

            isInitialized.Value = true;
        }

        /// <summary>
        /// The first-run window is onboarding for a user who just added the package. Skip it when another
        /// program set the plugin up: the AI Game Dev App or <c>unity-mcp-cli</c> launched the editor with
        /// connection env vars / flags or as a CLI session, or the config file existed before the plugin first
        /// loaded (written before launch). The window stays one click away (<c>Window/AI Game Dev</c>).
        /// </summary>
        internal static bool IsConfiguredExternally(bool hasConnectionOverride, bool isCliSession, bool configCreatedThisSession)
            => hasConnectionOverride || isCliSession || !configCreatedThisSession;
    }
}