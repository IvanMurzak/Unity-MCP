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
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Package
    {
        public static class Error
        {
            public static string PackageNameIsEmpty()
                => "[Error] Package name is empty. Please provide a valid package name. Sample: 'com.unity.textmeshpro'.";

            public static string PackageIdentifierIsEmpty()
                => "[Error] Package identifier is empty. Please provide a valid package identifier. Sample: 'com.unity.textmeshpro' or 'com.unity.textmeshpro@3.0.6'.";

            public static string PackageNotFound(string packageName)
                => $"[Error] Package '{packageName}' not found in the project.";

            public static string PackageOperationFailed(string operation, string packageName, string error)
                => $"[Error] Failed to {operation} package '{packageName}': {error}";

            public static string PackageSearchFailed(string query, string error)
                => $"[Error] Failed to search for packages with query '{query}': {error}";

            public static string PackageListFailed(string error)
                => $"[Error] Failed to list packages: {error}";
        }
    }
}
