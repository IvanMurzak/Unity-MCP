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
using System.Collections.Generic;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        public const string Version = "0.16.2";

        static Startup()
        {
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
            Server.DownloadServerBinaryIfNeeded();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of Unity-MCP. Please consider the move the project to a folder without spaces.");

            Application.unloading += OnBeforeReload;
            Application.quitting += OnBeforeReload;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;

            // Initialize sub-systems
            API.Tool_TestRunner.Init();
        }
        static async void OnBeforeReload()
        {
            var instance = McpPlugin.Instance;
            if (instance == null)
            {
                await McpPlugin.StaticDisposeAsync();
                return; // ignore
            }

            await (instance.RpcRouter?.Disconnect() ?? Task.CompletedTask);
            //await instance.Disconnect();
            //await McpPlugin.StaticDisposeAsync();
            // await Task.WhenAll(
            //     instance.Disconnect(),
            //     McpPlugin.StaticDisposeAsync()
            // );
        }
        static void OnAfterReload()
        {
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// </summary>
        public static bool IsCi()
        {
            var commandLineArgs = ArgsUtils.ParseCommandLineArguments();

            var ci = commandLineArgs.GetValueOrDefault("CI") ?? Environment.GetEnvironmentVariable("CI");
            var gha = commandLineArgs.GetValueOrDefault("GITHUB_ACTIONS") ?? Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var az = commandLineArgs.GetValueOrDefault("TF_BUILD") ?? Environment.GetEnvironmentVariable("TF_BUILD"); // Azure Pipelines

            return string.Equals(ci?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gha?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(az?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
