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
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Package
    {
        public static class Error
        {
            public static string InvalidRequestID()
                => "[Error] Original request with valid RequestID must be provided.";

            public static string ManifestNotFound(string manifestPath)
                => $"[Error] Package manifest not found at: {manifestPath}";

            public static string ManifestParseError(string manifestPath)
                => $"[Error] Failed to parse manifest.json at: {manifestPath}";

            public static string PackageNotFound(string packageId)
                => $"[Warning] Package {packageId} not found";

            public static string CompilationError(string message)
                => $"[Error] Compilation failed: {message}";
        }
    }
}