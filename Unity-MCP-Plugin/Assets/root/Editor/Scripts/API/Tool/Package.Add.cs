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
        [Description("Result of package add/install operation.")]
        public class PackageAddResult
        {
            [Description("Whether the operation was successful.")]
            public bool Success { get; set; }

            [Description("The name of the package that was installed.")]
            public string PackageName { get; set; } = string.Empty;

            [Description("The version that was installed.")]
            public string InstalledVersion { get; set; } = string.Empty;

            [Description("Status message describing the result.")]
            public string Message { get; set; } = string.Empty;
        }

        [McpPluginTool
        (
            "package-add",
            Title = "Package Manager / Add"
        )]
        [Description(@"Install a package from the Unity Package Manager registry, Git URL, or local path.
Supports various package identifier formats:
- Registry package: 'com.unity.textmeshpro' (latest version) or 'com.unity.textmeshpro@3.0.6' (specific version)
- Git URL: 'https://github.com/user/repo.git' or 'https://github.com/user/repo.git#branch'
- Local path: 'file:../relative/path' or 'file:C:/absolute/path'
This operation modifies the project's manifest.json and triggers package resolution.")]
        public async Task<PackageAddResult> Add
        (
            [Description(@"The package identifier to install. Formats:
- Package name: 'com.unity.textmeshpro' (installs latest compatible version)
- Package with version: 'com.unity.textmeshpro@3.0.6'
- Git URL: 'https://github.com/user/repo.git'
- Git URL with branch/tag: 'https://github.com/user/repo.git#v1.0.0'
- Local path: 'file:../MyPackage'")]
            string packageIdentifier
        )
        {
            if (string.IsNullOrWhiteSpace(packageIdentifier))
                throw new ArgumentException(Error.PackageIdentifierIsEmpty());

            return await MainThread.Instance.RunAsync(async () =>
            {
                var addRequest = Client.Add(packageIdentifier);

                while (!addRequest.IsCompleted)
                    await Task.Yield();

                if (addRequest.Status == StatusCode.Failure)
                {
                    return new PackageAddResult
                    {
                        Success = false,
                        PackageName = packageIdentifier,
                        Message = Error.PackageOperationFailed("add", packageIdentifier, addRequest.Error?.message ?? "Unknown error")
                    };
                }

                var installedPackage = addRequest.Result;
                return new PackageAddResult
                {
                    Success = true,
                    PackageName = installedPackage.name,
                    InstalledVersion = installedPackage.version,
                    Message = $"Successfully installed {installedPackage.displayName ?? installedPackage.name} version {installedPackage.version}"
                };
            }).Unwrap();
        }
    }
}
