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
using System.ComponentModel;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor.PackageManager;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Package
    {
        [Description("Result of package remove/uninstall operation.")]
        public class PackageRemoveResult
        {
            [Description("Whether the operation was successful.")]
            public bool Success { get; set; }

            [Description("The name of the package that was removed.")]
            public string PackageName { get; set; } = string.Empty;

            [Description("Status message describing the result.")]
            public string Message { get; set; } = string.Empty;
        }

        [McpPluginTool
        (
            "package-remove",
            Title = "Package Manager / Remove"
        )]
        [Description(@"Remove (uninstall) a package from the Unity project.
This removes the package from the project's manifest.json and triggers package resolution.
Note: Built-in packages and packages that are dependencies of other installed packages cannot be removed.")]
        public async Task<PackageRemoveResult> Remove
        (
            [Description("The name of the package to remove. Example: 'com.unity.textmeshpro'. Do not include version number.")]
            string packageName
        )
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException(Error.PackageNameIsEmpty());

            // Remove version suffix if accidentally included
            var cleanPackageName = packageName.Contains("@")
                ? packageName.Substring(0, packageName.IndexOf('@'))
                : packageName;

            return await MainThread.Instance.RunAsync(async () =>
            {
                // First verify the package is installed
                var listRequest = Client.List(true);
                while (!listRequest.IsCompleted)
                    await Task.Yield();

                if (listRequest.Status == StatusCode.Success)
                {
                    var isInstalled = false;
                    foreach (var pkg in listRequest.Result)
                    {
                        if (pkg.name.Equals(cleanPackageName, StringComparison.OrdinalIgnoreCase))
                        {
                            isInstalled = true;
                            break;
                        }
                    }

                    if (!isInstalled)
                    {
                        return new PackageRemoveResult
                        {
                            Success = false,
                            PackageName = cleanPackageName,
                            Message = Error.PackageNotFound(cleanPackageName)
                        };
                    }
                }

                var removeRequest = Client.Remove(cleanPackageName);

                while (!removeRequest.IsCompleted)
                    await Task.Yield();

                if (removeRequest.Status == StatusCode.Failure)
                {
                    return new PackageRemoveResult
                    {
                        Success = false,
                        PackageName = cleanPackageName,
                        Message = Error.PackageOperationFailed("remove", cleanPackageName, removeRequest.Error?.message ?? "Unknown error")
                    };
                }

                return new PackageRemoveResult
                {
                    Success = true,
                    PackageName = cleanPackageName,
                    Message = $"Successfully removed package '{cleanPackageName}'"
                };
            }).Unwrap();
        }
    }
}
