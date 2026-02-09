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
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
    using Consts = McpPlugin.Common.Consts;

    public static partial class Startup
    {
        public static class Server
        {
            public const string ExecutableName = "unity-mcp-server";

            public static string McpServerName
                => string.IsNullOrEmpty(Application.productName)
                    ? "Unity Unknown"
                    : $"Unity {Application.productName}";

            public static string OperationSystem =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                "unknown";

            public static string CpuArch => RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                _ => "unknown"
            };

            public static string PlatformName => $"{OperationSystem}-{CpuArch}";

            // Server executable file name
            // Sample (mac linux): unity-mcp-server
            // Sample   (windows): unity-mcp-server.exe
            public static string ExecutableFullName
                => ExecutableName.ToLowerInvariant() + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? ".exe"
                    : string.Empty);

            // Full path to the server executable
            // Sample (mac linux): ../Library/mcp-server
            // Sample   (windows): ../Library/mcp-server
            public static string ExecutableFolderRootPath
                => Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "../Library",
                        "mcp-server"
                    )
                );

            // Full path to the server executable
            // Sample (mac linux): ../Library/mcp-server/osx-x64
            // Sample   (windows): ../Library/mcp-server/win-x64
            public static string ExecutableFolderPath
                => Path.GetFullPath(
                    Path.Combine(
                        ExecutableFolderRootPath,
                        PlatformName
                    )
                );

            // Full path to the server executable
            // Sample (mac linux): ../Library/mcp-server/osx-x64/unity-mcp-server
            // Sample   (windows): ../Library/mcp-server/win-x64/unity-mcp-server.exe
            public static string ExecutableFullPath
                => Path.GetFullPath(
                    Path.Combine(
                        ExecutableFolderPath,
                        ExecutableFullName
                    )
                );

            public static string VersionFullPath
                => Path.GetFullPath(
                    Path.Combine(
                        ExecutableFolderPath,
                        "version"
                    )
                );

            // ------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Generates a JSON configuration for stdio transport.
            /// <code>
            /// {
            ///   "mcpServers": {
            ///     "Unity ProjectName": {
            ///       "type": "...",    // optional, only if provided
            ///       "command": "path/to/unity-mcp-server",
            ///       "args": ["port=...", "plugin-timeout=...", "client-transport=stdio"]
            ///     }
            ///   }
            /// }
            /// </code>
            /// </summary>
            public static JsonNode RawJsonConfigurationStdio(
                int port,
                string bodyPath = "mcpServers",
                int timeoutMs = Consts.Hub.DefaultTimeoutMs,
                string? type = null)
            {
                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Build innermost content first
                var serverConfig = new JsonObject();

                if (type != null)
                    serverConfig["type"] = type;

                serverConfig["command"] = ExecutableFullPath.Replace('\\', '/');
                serverConfig["args"] = new JsonArray
                {
                    $"{Consts.MCP.Server.Args.Port}={port}",
                    $"{Consts.MCP.Server.Args.PluginTimeout}={timeoutMs}",
                    $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
                };

                var innerContent = new JsonObject
                {
                    [AiAgentConfig.DefaultMcpServerName] = serverConfig
                };

                // Build nested structure from innermost to outermost
                var result = innerContent;
                for (int i = pathSegments.Length - 1; i >= 0; i--)
                {
                    result = new JsonObject { [pathSegments[i]] = result };
                }

                return result;
            }

            /// <summary>
            /// Generates a JSON configuration for HTTP transport.
            /// <code>
            /// {
            ///   "mcpServers": {
            ///     "Unity ProjectName": {
            ///       "type": "...",  // optional, only if provided
            ///       "url": "http://localhost:port"
            ///     }
            ///   }
            /// }
            /// </code>
            /// </summary>
            public static JsonNode RawJsonConfigurationHttp(
                string url,
                string bodyPath = "mcpServers",
                string? type = null)
            {
                var pathSegments = Consts.MCP.Server.BodyPathSegments(bodyPath);

                // Build innermost content first
                var serverConfig = new JsonObject();

                if (type != null)
                    serverConfig["type"] = type;

                serverConfig["url"] = url;

                var innerContent = new JsonObject
                {
                    [AiAgentConfig.DefaultMcpServerName] = serverConfig
                };

                // Build nested structure from innermost to outermost
                var result = innerContent;
                for (int i = pathSegments.Length - 1; i >= 0; i--)
                {
                    result = new JsonObject { [pathSegments[i]] = result };
                }

                return result;
            }

            // ------------------------------------------------------------------------------------------------------------------------------------

            public static string DockerRunCommand()
            {
                var dockerPortMapping = $"-p {UnityMcpPlugin.Port}:{UnityMcpPlugin.Port}";
                var dockerEnvVars = $"-e MCP_PLUGIN_CLIENT_TRANSPORT={TransportMethod.streamableHttp} -e MCP_PLUGIN_PORT={UnityMcpPlugin.Port} -e MCP_PLUGIN_CLIENT_TIMEOUT={UnityMcpPlugin.TimeoutMs}";
                var dockerContainer = $"--name unity-mcp-server-{UnityMcpPlugin.Port}";
                var dockerImage = $"ivanmurzakdev/unity-mcp-server:{UnityMcpPlugin.Version}";
                return $"docker run -d {dockerPortMapping} {dockerEnvVars} {dockerContainer} {dockerImage}";
            }

            public static string ExecutableZipUrl
                => $"https://github.com/IvanMurzak/Unity-MCP/releases/download/{UnityMcpPlugin.Version}/{ExecutableName.ToLowerInvariant()}-{PlatformName}.zip";

            // ------------------------------------------------------------------------------------------------------------------------------------

            public static bool IsBinaryExists()
            {
                if (string.IsNullOrEmpty(ExecutableFullPath))
                    return false;

                return File.Exists(ExecutableFullPath);
            }

            public static string? GetBinaryVersion()
            {
                if (!File.Exists(VersionFullPath))
                    return null;

                return File.ReadAllText(VersionFullPath);
            }

            public static bool IsVersionMatches()
            {
                var binaryVersion = GetBinaryVersion();
                if (binaryVersion == null)
                    return false;

                return binaryVersion == UnityMcpPlugin.Version;
            }

            public static bool DeleteBinaryFolderIfExists()
            {
                if (Directory.Exists(ExecutableFolderRootPath))
                {
                    // Intentional infinite loop:
                    // - Deletion can fail while the MCP server binaries are in use (e.g., server still running).
                    // - On the first failure, we automatically attempt to stop the server process via McpServerManager.
                    // - The retry/exit behavior is fully controlled by the user via the dialog below.
                    // - We do not impose a fixed maximum retry count so the user can take as long as needed
                    //   to shut down their MCP client and release file locks before trying again.
                    // - The loop terminates when the user selects "Skip", at which point the exception is rethrown.
                    var silentRetries = 0;
                    while (true)
                    {
                        try
                        {
                            Directory.Delete(ExecutableFolderRootPath, recursive: true);
                            Debug.Log($"Deleted existing MCP server folder: <color=orange>{ExecutableFolderRootPath}</color>");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // First failure: try to stop the running server process that may be locking files
                            if (silentRetries == 0)
                            {
                                silentRetries++;
                                Debug.Log($"Failed to delete MCP server folder. Attempting to stop the server process...");
                                try
                                {
                                    if (!McpServerManager.StopServer(force: true))
                                    {
                                        Debug.LogWarning($"No running MCP server process found to stop.");
                                    }
                                    else
                                    {
                                        Debug.Log($"Stop signal sent to MCP server process. Retrying deletion...");
                                        Thread.Sleep(2000); // Wait a moment for the process to exit and release file locks
                                    }
                                }
                                catch (Exception stopEx)
                                {
                                    Debug.LogWarning($"Failed to stop MCP server: {stopEx.Message}");
                                }
                                continue; // Retry deletion after stopping the server
                            }

                            // Second failure: retry once more silently (OS may need time to release file locks)
                            if (silentRetries <= 1)
                            {
                                silentRetries++;
                                continue;
                            }

                            var retry = UnityEditor.EditorUtility.DisplayDialog(
                                title: "Failed to Delete MCP Server Binaries",
                                message: $"The current unity-mcp-server binaries can't be deleted. " +
                                    $"This is very likely because the MCP server is currently running.\n\n" +
                                    $"Please close your MCP client to make sure the server is not running, then click \"Retry\".\n\n" +
                                    $"Path: {ExecutableFolderRootPath}\n\n" +
                                    $"Error: {ex.Message}",
                                ok: "Retry",
                                cancel: "Skip"
                            );

                            if (!retry)
                            {
                                throw;
                            }
                            // If retry is true, loop continues and tries again
                        }
                    }
                }
                return false;
            }

            public static Task<bool> DownloadServerBinaryIfNeeded()
            {
                if (EnvironmentUtils.IsCi())
                {
                    // Ignore in CI environment
                    Debug.Log($"Ignore MCP server downloading in CI environment");
                    return Task.FromResult(false);
                }

                if (IsBinaryExists() && IsVersionMatches())
                    return Task.FromResult(true);

                return DownloadAndUnpackBinary();
            }

            public static async Task<bool> DownloadAndUnpackBinary()
            {
                Debug.Log($"Downloading Unity-MCP-Server binary from: <color=yellow>{ExecutableZipUrl}</color>");

                try
                {
                    var previousKeepServerRunning = UnityMcpPlugin.KeepServerRunning;

                    // Clear existed server folder
                    DeleteBinaryFolderIfExists();

                    // Create folder if needed
                    if (!Directory.Exists(ExecutableFolderPath))
                        Directory.CreateDirectory(ExecutableFolderPath);

                    var archiveFilePath = Path.GetFullPath($"{Application.temporaryCachePath}/{ExecutableName.ToLowerInvariant()}-{PlatformName}-{UnityMcpPlugin.Version}.zip");
                    Debug.Log($"Temporary archive file path: <color=yellow>{archiveFilePath}</color>");

                    // Download the zip file from the GitHub release notes
                    using (var client = new WebClient())
                    {
                        await client.DownloadFileTaskAsync(ExecutableZipUrl, archiveFilePath);
                    }

                    // Unpack zip archive
                    Debug.Log($"Unpacking Unity-MCP-Server binary to: <color=yellow>{ExecutableFolderPath}</color>");
                    ZipFile.ExtractToDirectory(archiveFilePath, ExecutableFolderRootPath, overwriteFiles: true);

                    if (!File.Exists(ExecutableFullPath))
                    {
                        Debug.LogError($"Failed to unpack server binary to: {ExecutableFolderRootPath}");
                        Debug.LogError($"Binary file not found at: {ExecutableFullPath}");
                        return false;
                    }

                    Debug.Log($"Downloaded and unpacked Unity-MCP-Server binary to: <color=green>{ExecutableFullPath}</color>");

                    // Set executable permission on macOS and Linux
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Debug.Log($"Setting executable permission for: <color=green>{ExecutableFullPath}</color>");
                        UnixUtils.Set0755(ExecutableFullPath);
                    }

                    File.WriteAllText(VersionFullPath, UnityMcpPlugin.Version);

                    Debug.Log($"MCP server version file created at: <color=green><b>COMPLETED</b></color>");

                    var binaryExists = IsBinaryExists();
                    var versionMatches = IsVersionMatches();
                    var success = binaryExists && versionMatches;

                    if (success && previousKeepServerRunning)
                    {
                        if (!McpServerManager.StartServer())
                            Debug.LogError($"Failed to start MCP server after updating binary. Please try starting the server manually.");
                    }

                    NotificationPopupWindow.Show(
                        windowTitle: success
                            ? "Updated"
                            : "Update Failed",
                        height: 235,
                        minHeight: 235,
                        title: success
                            ? "Server Binary Updated"
                            : "Server Binary Update Failed",
                        message: success
                            ? "The MCP server binary was successfully downloaded and updated. \n\n" +
                                $"Version: {GetBinaryVersion()}\n\n" +
                                "You may need to restart your AI agent to reconnect to the updated server."
                            : "Failed to download and update the MCP server binary. Please check the logs for details.");

                    return success;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to download and unpack server binary: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
