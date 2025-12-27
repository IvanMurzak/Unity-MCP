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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor.PackageManager;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Package
    {
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
This operation modifies the project's manifest.json and triggers package resolution.
Note: Package installation may trigger a domain reload. The result will be sent after the reload completes.")]
        public static ResponseCallTool Add
        (
            [Description(@"The package identifier to install. Formats:
- Package name: 'com.unity.textmeshpro' (installs latest compatible version)
- Package with version: 'com.unity.textmeshpro@3.0.6'
- Git URL: 'https://github.com/user/repo.git'
- Git URL with branch/tag: 'https://github.com/user/repo.git#v1.0.0'
- Local path: 'file:../MyPackage'")]
            string packageIdentifier,
            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("[Error] Original request with valid RequestID must be provided.");

            if (string.IsNullOrWhiteSpace(packageIdentifier))
                return ResponseCallTool.Error(Error.PackageIdentifierIsEmpty()).SetRequestID(requestId);

            MainThread.Instance.RunAsync(async () =>
            {
                await Task.Yield();

                var addRequest = Client.Add(packageIdentifier);

                while (!addRequest.IsCompleted)
                    await Task.Yield();

                var success = addRequest.Status == StatusCode.Success;
                var packageName = success ? addRequest.Result.name : packageIdentifier;
                var version = success ? addRequest.Result.version : string.Empty;

                if (!success)
                {
                    // If the operation failed immediately, send error response
                    var errorMessage = Error.PackageOperationFailed("add", packageIdentifier, addRequest.Error?.message ?? "Unknown error");
                    _ = UnityMcpPlugin.NotifyToolRequestCompleted(new RequestToolCompletedData
                    {
                        RequestId = requestId,
                        Result = ResponseCallTool.Error(errorMessage).SetRequestID(requestId)
                    });
                    return;
                }

                // Schedule notification to be sent after domain reload completes
                var displayName = addRequest.Result.displayName ?? packageName;
                PackageUtils.SchedulePostDomainReloadNotification(
                    requestId,
                    $"{displayName} v{version}",
                    "add",
                    expectedResult: true
                );
            });

            return ResponseCallTool.Processing($"Adding package '{packageIdentifier}'. Waiting for package resolution and potential domain reload...").SetRequestID(requestId);
        }
    }
}
