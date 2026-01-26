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
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
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

            public static JsonNode RawJsonConfigurationStdio(
                int port,
                string bodyPath = "mcpServers",
                int timeoutMs = Consts.Hub.DefaultTimeoutMs)
            {
                return Consts.MCP.Server.Config(
                    executablePath: ExecutableFullPath.Replace('\\', '/'),
                    serverName: Utils.AiAgentConfig.DefaultMcpServerName,
                    bodyPath: bodyPath,
                    port: port,
                    timeoutMs: timeoutMs
                );
            }

            public static JsonNode RawJsonConfigurationHttp(
                string url,
                string bodyPath = "mcpServers")
            {
                return new JsonObject()
                {
                    [bodyPath] = new JsonObject()
                    {
                        [Utils.AiAgentConfig.DefaultMcpServerName] = new JsonObject
                        {
                            ["url"] = url,
                            ["type"] = "streamableHttp"
                        }
                    }
                };
            }

            public static string DockerRunCommand()
            {
                var dockerPortMapping = $"-p {UnityMcpPlugin.Port}:{UnityMcpPlugin.Port}";
                var dockerEnvVars = $"-e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT={UnityMcpPlugin.Port} -e MCP_PLUGIN_CLIENT_TIMEOUT={UnityMcpPlugin.TimeoutMs}";
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

            public static bool IsVersionMatches()
            {
                if (!File.Exists(VersionFullPath))
                    return false;

                var existingVersion = File.ReadAllText(VersionFullPath);
                return existingVersion == UnityMcpPlugin.Version;
            }

            public static void DeleteBinaryFolderIfExists()
            {
                if (Directory.Exists(ExecutableFolderRootPath))
                {
                    // Intentional infinite loop:
                    // - Deletion can fail while the MCP server binaries are in use (e.g., server still running).
                    // - The retry/exit behavior is fully controlled by the user via the dialog below.
                    // - We do not impose a fixed maximum retry count so the user can take as long as needed
                    //   to shut down their MCP client and release file locks before trying again.
                    // - The loop terminates when the user selects "Skip", at which point the exception is rethrown.
                    while (true)
                    {
                        try
                        {
                            Directory.Delete(ExecutableFolderRootPath, recursive: true);
                            Debug.Log($"Deleted existing MCP server folder: <color=orange>{ExecutableFolderRootPath}</color>");
                            return;
                        }
                        catch (Exception ex)
                        {
                            var retry = UnityEditor.EditorUtility.DisplayDialog(
                                "Failed to Delete MCP Server Binaries",
                                $"The current unity-mcp-server binaries can't be deleted. " +
                                $"This is very likely because the MCP server is currently running.\n\n" +
                                $"Please close your MCP client to make sure the server is not running, then click \"Retry\".\n\n" +
                                $"Path: {ExecutableFolderRootPath}\n\n" +
                                $"Error: {ex.Message}",
                                "Retry",
                                "Skip"
                            );

                            if (!retry)
                            {
                                throw;
                            }
                            // If retry is true, loop continues and tries again
                        }
                    }
                }
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

                    return IsBinaryExists() && IsVersionMatches();
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
