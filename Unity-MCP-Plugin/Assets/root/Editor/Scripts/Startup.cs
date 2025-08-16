#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        public const string Version = "0.14.2";

        static Startup()
        {
            McpPluginUnity.BuildAndStart(openConnection: !IsCi());
            Server.DownloadServerBinaryIfNeeded();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of Unity-MCP. Please consider the move the project to a folder without spaces.");
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// </summary>
        public static bool IsCi()
        {
            var ci = Environment.GetEnvironmentVariable("CI");
            var gha = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var az = Environment.GetEnvironmentVariable("TF_BUILD"); // Azure Pipelines
            return string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gha, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(az, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}