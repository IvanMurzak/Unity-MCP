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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        static ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(Startup));

        static Startup()
        {
            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            // Defer connection to avoid blocking during domain reload.
            // Starting async SignalR connections during [InitializeOnLoad] can cause
            // Unity to freeze because async continuations may run on the main thread
            // while it's still processing the domain reload.
            if (!EnvironmentUtils.IsCi())
                EditorApplication.delayCall += () => UnityMcpPlugin.ConnectIfNeeded();

            Server.DownloadServerBinaryIfNeeded();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of AI Game Developer. Please consider the move the project to a folder without spaces.");

            SubscribeOnEditorEvents();

            // Initialize sub-systems
            API.Tool_Tests.Init();
            UpdateChecker.Init();
            PackageUtils.Init();
        }
    }
}
