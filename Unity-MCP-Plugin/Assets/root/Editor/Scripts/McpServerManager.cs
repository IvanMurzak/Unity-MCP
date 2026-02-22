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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;
using UnityEngine;
using McpConsts = com.IvanMurzak.McpPlugin.Common.Consts;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
    using Consts = McpPlugin.Common.Consts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public enum McpServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        External
    }

    /// <summary>
    /// Manages the MCP server binary and process lifecycle independently from UI.
    /// Provides cross-platform support for Windows, macOS, and Linux.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerManager
    {
        const string ProcessIdKey = "McpServerManager_ProcessId";
        const string McpServerProcessName = "unity-mcp-server";

        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(typeof(McpServerManager));
        static readonly ReactiveProperty<McpServerStatus> _serverStatus = new(McpServerStatus.Stopped);
        static readonly object _processMutex = new();

        static Process? _serverProcess;

        public static ReadOnlyReactiveProperty<McpServerStatus> ServerStatus => _serverStatus;

        public static bool IsRunning => _serverStatus.CurrentValue == McpServerStatus.Running;

        static McpServerManager()
        {
            // Register for editor quit to clean up the server process
            EditorApplication.quitting += OnEditorQuitting;

            // Check if server process is still running (e.g., after domain reload)
            EditorApplication.update += CheckExistingProcess;

            DownloadServerBinaryIfNeeded()
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || !task.Result)
                        return; // Failed to download binaries, skip auto-start

                    if (!task.Result)
                        return; // No binaries available (either in CI or failed to download), skip auto-start

                    if (EnvironmentUtils.IsCi())
                        return; // Skip auto-start in CI environment

                    EditorApplication.update += StartServerIfNeeded;
                });
        }

        #region Binary Metadata

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

        public static string ExecutableZipUrl
            => $"https://github.com/IvanMurzak/Unity-MCP/releases/download/{UnityMcpPlugin.Version}/{ExecutableName.ToLowerInvariant()}-{PlatformName}.zip";

        #endregion // Binary Metadata

        #region Binary Lifecycle

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
                        UnityEngine.Debug.Log($"Deleted existing MCP server folder: <color=orange>{ExecutableFolderRootPath}</color>");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // First failure: try to stop the running server process that may be locking files
                        if (silentRetries == 0)
                        {
                            silentRetries++;
                            UnityEngine.Debug.Log($"Failed to delete MCP server folder. Attempting to stop the server process...");
                            try
                            {
                                if (!StopServer(force: true))
                                {
                                    UnityEngine.Debug.LogWarning($"No running MCP server process found to stop.");
                                }
                                else
                                {
                                    UnityEngine.Debug.Log($"Stop signal sent to MCP server process. Retrying deletion...");
                                    Thread.Sleep(2000); // Wait a moment for the process to exit and release file locks
                                }
                            }
                            catch (Exception stopEx)
                            {
                                UnityEngine.Debug.LogWarning($"Failed to stop MCP server: {stopEx.Message}");
                            }
                            continue; // Retry deletion after stopping the server
                        }

                        // Second failure: retry once more silently (OS may need time to release file locks)
                        if (silentRetries <= 1)
                        {
                            silentRetries++;
                            continue;
                        }

                        var retry = EditorUtility.DisplayDialog(
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
                UnityEngine.Debug.Log($"Ignore MCP server downloading in CI environment");
                return Task.FromResult(false);
            }

            if (IsBinaryExists() && IsVersionMatches())
                return Task.FromResult(true);

            return DownloadAndUnpackBinary();
        }

        public static async Task<bool> DownloadAndUnpackBinary()
        {
            UnityEngine.Debug.Log($"Downloading Unity-MCP-Server binary from: <color=yellow>{ExecutableZipUrl}</color>");

            try
            {
                var previousKeepServerRunning = UnityMcpPlugin.KeepServerRunning;

                // Clear existed server folder
                DeleteBinaryFolderIfExists();

                // Create folder if needed
                if (!Directory.Exists(ExecutableFolderPath))
                    Directory.CreateDirectory(ExecutableFolderPath);

                var archiveFilePath = Path.GetFullPath($"{Application.temporaryCachePath}/{ExecutableName.ToLowerInvariant()}-{PlatformName}-{UnityMcpPlugin.Version}.zip");
                UnityEngine.Debug.Log($"Temporary archive file path: <color=yellow>{archiveFilePath}</color>");

                // Download the zip file from the GitHub release notes
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(ExecutableZipUrl, archiveFilePath);
                }

                // Unpack zip archive
                UnityEngine.Debug.Log($"Unpacking Unity-MCP-Server binary to: <color=yellow>{ExecutableFolderPath}</color>");
                ZipFile.ExtractToDirectory(archiveFilePath, ExecutableFolderRootPath, overwriteFiles: true);

                if (!File.Exists(ExecutableFullPath))
                {
                    UnityEngine.Debug.LogError($"Failed to unpack server binary to: {ExecutableFolderRootPath}");
                    UnityEngine.Debug.LogError($"Binary file not found at: {ExecutableFullPath}");
                    return false;
                }

                UnityEngine.Debug.Log($"Downloaded and unpacked Unity-MCP-Server binary to: <color=green>{ExecutableFullPath}</color>");

                // Set executable permission on macOS and Linux
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    UnityEngine.Debug.Log($"Setting executable permission for: <color=green>{ExecutableFullPath}</color>");
                    UnixUtils.Set0755(ExecutableFullPath);
                }

                File.WriteAllText(VersionFullPath, UnityMcpPlugin.Version);

                UnityEngine.Debug.Log($"MCP server version file created at: <color=green><b>COMPLETED</b></color>");

                var binaryExists = IsBinaryExists();
                var versionMatches = IsVersionMatches();
                var success = binaryExists && versionMatches;

                if (success && previousKeepServerRunning)
                {
                    if (!StartServer())
                        UnityEngine.Debug.LogError($"Failed to start MCP server after updating binary. Please try starting the server manually.");
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
                UnityEngine.Debug.LogException(ex);
                UnityEngine.Debug.LogError($"Failed to download and unpack server binary: {ex.Message}");
                return false;
            }
        }

        #endregion // Binary Lifecycle

        #region Client Configuration

        /// <summary>
        /// Generates a JSON configuration for stdio transport.
        /// <code>
        /// {
        ///   "mcpServers": {
        ///     "Unity ProjectName": {
        ///       "type": "...",    // optional, only if provided
        ///       "command": "path/to/unity-mcp-server",
        ///       "args": ["port=...", "plugin-timeout=...", "client-transport=stdio", "token=..."]
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
            var pathSegments = BodyPathSegments(bodyPath);

            // Build innermost content first
            var serverConfig = new JsonObject();

            if (type != null)
                serverConfig["type"] = type;

            serverConfig["command"] = ExecutableFullPath.Replace('\\', '/');

            var args = new JsonArray
            {
                $"{Args.Port}={port}",
                $"{Args.PluginTimeout}={timeoutMs}",
                $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
                $"{Args.Authorization}={UnityMcpPlugin.AuthOption}"
            };

            var authRequired = UnityMcpPlugin.AuthOption == AuthOption.required;
            if (authRequired && string.IsNullOrEmpty(UnityMcpPlugin.Token))
            {
                args.Add($"{Args.Token}={UnityMcpPlugin.Token}");
            }

            serverConfig["args"] = args;

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
        ///       "url": "http://localhost:port",
        ///      "headers": {     // only if token is provided
        ///        "Authorization": "Bearer token"
        ///      }
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
            var pathSegments = BodyPathSegments(bodyPath);

            // Build innermost content first
            var serverConfig = new JsonObject();

            if (type != null)
                serverConfig["type"] = type;

            serverConfig["url"] = url;

            var authRequired = UnityMcpPlugin.AuthOption == AuthOption.required;
            if (authRequired && !string.IsNullOrEmpty(UnityMcpPlugin.Token))
            {
                serverConfig["headers"] = new JsonObject
                {
                    ["Authorization"] = $"Bearer {UnityMcpPlugin.Token}"
                };
            }

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

        public static string DockerSetupRunCommand()
        {
            var dockerPortMapping = $"-p {UnityMcpPlugin.Port}:{UnityMcpPlugin.Port}";
            var dockerEnvVars =
                $"-e {Env.ClientTransportMethod}={TransportMethod.streamableHttp} " +
                $"-e {Env.Port}={UnityMcpPlugin.Port} " +
                $"-e {Env.PluginTimeout}={UnityMcpPlugin.TimeoutMs} " +
                $"-e {Env.Authorization}={UnityMcpPlugin.AuthOption}";

            var authRequired = UnityMcpPlugin.AuthOption == AuthOption.required;
            var token = UnityMcpPlugin.Token;
            if (authRequired && !string.IsNullOrEmpty(token))
                dockerEnvVars += $" -e {Env.Token}={token}";

            var dockerContainer = $"--name unity-mcp-server-{UnityMcpPlugin.Port}";
            var dockerImage = $"ivanmurzakdev/unity-mcp-server:{UnityMcpPlugin.Version}";
            return $"docker run -d {dockerPortMapping} {dockerEnvVars} {dockerContainer} {dockerImage}";
        }

        public static string DockerRunCommand()
        {
            return $"docker start unity-mcp-server-{UnityMcpPlugin.Port}";
        }

        public static string DockerStopCommand()
        {
            return $"docker stop unity-mcp-server-{UnityMcpPlugin.Port}";
        }

        public static string DockerRemoveCommand()
        {
            return $"docker rm unity-mcp-server-{UnityMcpPlugin.Port}";
        }

        #endregion // Client Configuration

        #region Process Lifecycle

        static void CheckExistingProcess()
        {
            EditorApplication.update -= CheckExistingProcess;
            // Try to find an existing server process by checking if our tracked PID is still running
            // This helps maintain state across domain reloads
            var savedPid = EditorPrefs.GetInt(ProcessIdKey, -1);
            if (savedPid > 0)
            {
                try
                {
                    var process = Process.GetProcessById(savedPid);
                    if (process != null && !process.HasExited)
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        if (processName.Contains(McpServerProcessName))
                        {
                            _serverProcess = process;
                            _serverStatus.Value = McpServerStatus.Running;
                            _logger.LogInformation("Reconnected to existing MCP server process (PID: {pid})", savedPid);

                            // Re-attach exit handler
                            process.EnableRaisingEvents = true;
                            process.Exited += OnProcessExited;

                            // Schedule verification check to detect if process crashes shortly after reconnection
                            ScheduleStartupVerification(savedPid);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not reconnect to previous process: {message}", ex.Message);
                }

                // Clear stale PID
                EditorPrefs.DeleteKey(ProcessIdKey);
            }
        }

        static void OnEditorQuitting()
        {
            StopServer(force: true);
        }

        public static bool StartServer()
        {
            lock (_processMutex)
            {
                if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                    _serverStatus.CurrentValue == McpServerStatus.Starting ||
                    _serverStatus.CurrentValue == McpServerStatus.Stopping)
                {
                    _logger.LogWarning("MCP server is already {status}", _serverStatus.CurrentValue);
                    return false;
                }

                if (!IsBinaryExists())
                {
                    _logger.LogError("MCP server binary not found at: {path}", ExecutableFullPath);
                    return false;
                }

                _serverStatus.Value = McpServerStatus.Starting;

                // Kill any orphaned server processes to free the port
                KillOrphanedServerProcesses();

                try
                {
                    var executablePath = ExecutableFullPath;
                    var arguments = BuildArguments();

                    _logger.LogInformation("Starting MCP server: {path} {args}", executablePath, arguments);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = ExecutableFolderPath
                    };

                    // Set executable permissions on Unix-like systems
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        UnixUtils.Set0755(executablePath);
                    }

                    _serverProcess = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };
                    _serverProcess.Exited += OnProcessExited;
                    _serverProcess.OutputDataReceived += OnOutputDataReceived;
                    _serverProcess.ErrorDataReceived += OnErrorDataReceived;

                    if (!_serverProcess.Start())
                    {
                        _logger.LogError("Failed to start MCP server process");
                        CleanupProcess();
                        return false;
                    }

                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();

                    // Save PID for reconnection after domain reload
                    EditorPrefs.SetInt(ProcessIdKey, _serverProcess.Id);

                    // Keep status as Starting - it will be set to Running after verification
                    _logger.LogInformation("MCP server process started (PID: {pid}), awaiting verification...", _serverProcess.Id);

                    // Schedule a delayed check to verify the process is still running
                    // This catches early crashes that might not trigger the Exited event reliably
                    // Status will be set to Running only after successful verification
                    ScheduleStartupVerification(_serverProcess.Id);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to start MCP server: {message}", ex.Message);
                    CleanupProcess();
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops the MCP server process.
        /// By default, this method is non-blocking: it sends the kill/terminate signal
        /// and lets the Exited event handler perform cleanup asynchronously.
        /// When force is true (e.g., editor quitting), it blocks until the process exits.
        /// </summary>
        public static bool StopServer(bool force = false)
        {
            lock (_processMutex)
            {
                if (_serverStatus.CurrentValue == McpServerStatus.Stopped ||
                    _serverStatus.CurrentValue == McpServerStatus.Stopping)
                {
                    _logger.LogDebug("MCP server is already stopped or stopping");
                    return true;
                }

                if (_serverProcess == null)
                {
                    _serverStatus.Value = McpServerStatus.Stopped;
                    EditorPrefs.DeleteKey(ProcessIdKey);
                    return true;
                }

                _serverStatus.Value = McpServerStatus.Stopping;

                try
                {
                    _logger.LogInformation("Stopping MCP server (PID: {pid})", _serverProcess.Id);

                    if (!_serverProcess.HasExited)
                    {
                        SendTerminateSignal();
                    }

                    if (force)
                    {
                        // Synchronous path: block until exit (used during editor quitting)
                        WaitForExitAndForceKillIfNeeded();
                        CleanupProcess();
                    }
                    else
                    {
                        if (_serverProcess.HasExited)
                        {
                            CleanupProcess();
                        }
                        else
                        {
                            // Non-blocking path: schedule background wait + force kill safety net.
                            // CleanupProcess will be called by OnProcessExited or the background task.
                            ScheduleForceKillIfNeeded();
                        }
                    }

                    _logger.LogInformation("MCP server stop initiated");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error stopping MCP server: {message}", ex.Message);
                    CleanupProcess();
                    return false;
                }
            }
        }

        /// <summary>
        /// Sends the platform-appropriate terminate signal without waiting for exit.
        /// </summary>
        static void SendTerminateSignal()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _serverProcess!.Kill();
            }
            else
            {
                // On Unix-like systems, send SIGTERM for graceful shutdown
                try
                {
                    using var killProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-TERM {_serverProcess!.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    killProcess?.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("SIGTERM failed, falling back to Kill(): {message}", ex.Message);
                    _serverProcess!.Kill();
                }
            }
        }

        /// <summary>
        /// Blocking wait for process exit, with force-kill fallback.
        /// Used only during editor quitting to prevent orphaned processes.
        /// </summary>
        static void WaitForExitAndForceKillIfNeeded()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
                return;

            if (!_serverProcess.WaitForExit(5000))
            {
                _logger.LogWarning("MCP server did not exit gracefully, forcing termination");
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Force kill failed: {message}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Background safety net: waits for the process to exit and force-kills after timeout.
        /// Calls CleanupProcess on the main thread when done.
        /// </summary>
        static void ScheduleForceKillIfNeeded()
        {
            var process = _serverProcess;
            if (process == null)
                return;

            Task.Run(() =>
            {
                try
                {
                    if (!process.HasExited && !process.WaitForExit(5000))
                    {
                        _logger.LogWarning("MCP server did not exit gracefully, forcing termination");
                        try
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Force kill error: {message}", ex.Message);
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogDebug("Process already exited or disposed while waiting for exit: {message}", ex.Message);
                }

                // Ensure cleanup on the main thread.
                // Safe to call even if OnProcessExited already triggered cleanup.
                MainThread.Instance.Run(CleanupProcess);
            });
        }

        /// <summary>
        /// Kills an orphaned unity-mcp-server process that is occupying this project's port.
        /// Only targets the specific process listening on <see cref="UnityMcpPlugin.Port"/>.
        /// If the port owner cannot be determined, does nothing (fails safe).
        /// </summary>
        static void KillOrphanedServerProcesses()
        {
            try
            {
                var port = UnityMcpPlugin.Port;
                var currentPid = _serverProcess?.Id ?? -1;

                var listeningPid = GetPidListeningOnPort(port);

                if (listeningPid <= 0)
                {
                    _logger.LogDebug("No process found listening on port {port}, port is available", port);
                    return;
                }

                if (listeningPid == currentPid)
                {
                    _logger.LogDebug("Our own server process (PID: {pid}) is listening on port {port}", listeningPid, port);
                    return;
                }

                try
                {
                    using var process = Process.GetProcessById(listeningPid);
                    if (process == null || process.HasExited)
                    {
                        _logger.LogDebug("Process (PID: {pid}) on port {port} has already exited", listeningPid, port);
                        return;
                    }

                    var processName = process.ProcessName.ToLowerInvariant();
                    if (!processName.Contains(McpServerProcessName))
                    {
                        _logger.LogWarning(
                            "Port {port} is occupied by a non-MCP process '{processName}' (PID: {pid}). " +
                            "The MCP server may fail to start. Please free the port or change the port in settings.",
                            port, process.ProcessName, listeningPid);
                        return;
                    }

                    _logger.LogWarning("Killing orphaned MCP server process (PID: {pid}) occupying port {port}", listeningPid, port);
                    process.Kill();

                    if (!process.WaitForExit(3000))
                        _logger.LogWarning("Orphaned MCP server process (PID: {pid}) did not exit within 3 seconds after kill", listeningPid);
                    else
                        _logger.LogDebug("Orphaned MCP server process (PID: {pid}) exited successfully", listeningPid);
                }
                catch (ArgumentException)
                {
                    _logger.LogDebug("Process (PID: {pid}) on port {port} no longer exists", listeningPid, port);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogDebug("Process (PID: {pid}) on port {port} exited before it could be terminated", listeningPid, port);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to kill orphaned process (PID: {pid}) on port {port}: {message}", listeningPid, port, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error in orphaned server process cleanup: {message}", ex.Message);
            }
        }

        /// <summary>
        /// Returns the PID of the process listening on the specified TCP port,
        /// or -1 if no process is found or the lookup fails.
        /// </summary>
        static int GetPidListeningOnPort(int port)
        {
            try
            {
                var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano -p tcp",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                    : new ProcessStartInfo
                    {
                        FileName = "lsof",
                        Arguments = $"-ti tcp:{port} -sTCP:LISTEN",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                using var process = Process.Start(startInfo);
                if (process == null) return -1;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var portSuffix = $":{port}";
                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (!trimmed.Contains("LISTENING"))
                            continue;

                        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5)
                            continue;

                        var localAddress = parts[1];
                        if (localAddress.EndsWith(portSuffix) && int.TryParse(parts[parts.Length - 1], out var pid))
                            return pid;
                    }
                }
                else
                {
                    var trimmed = output.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        return -1;

                    var firstLine = trimmed.Split('\n')[0].Trim();
                    if (int.TryParse(firstLine, out var pid))
                        return pid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to determine PID listening on port {port}: {message}", port, ex.Message);
            }

            return -1;
        }

        static string BuildArguments()
        {
            var port = UnityMcpPlugin.Port;
            var timeout = UnityMcpPlugin.TimeoutMs;
            var transportMethod = TransportMethod.streamableHttp; // always must be streamableHttp for launching the server.
            var token = UnityMcpPlugin.Token;
            var deploymentMode = UnityMcpPlugin.AuthOption;

            // Arguments format: port=XXXXX plugin-timeout=XXXXX client-transport=<TransportMethod> token=<Token>
            var args = $"{Args.Port}={port} " +
                $"{Args.PluginTimeout}={timeout} " +
                $"{Args.ClientTransportMethod}={transportMethod} " +
                $"{Args.Authorization}={deploymentMode}";

            if (!string.IsNullOrEmpty(token))
                args += $" {Args.Token}={token}";

            return args;
        }

        /// <summary>
        /// Schedules a verification check 5 seconds after startup to detect early crashes.
        /// If the process is still running after verification, the status is set to Running.
        /// If the process has exited and no longer exists, the status is set to Stopped.
        /// </summary>
        static void ScheduleStartupVerification(int processId)
        {
            var startTime = DateTime.UtcNow;
            const double verificationDelaySeconds = 5.0;

            void CheckProcess()
            {
                // If status is no longer Starting (e.g., OnProcessExited already cleaned up), unsubscribe
                if (_serverStatus.CurrentValue != McpServerStatus.Starting)
                {
                    EditorApplication.update -= CheckProcess;
                    return;
                }

                var elapsed = DateTime.UtcNow - startTime;

                // If we haven't reached verification delay yet, wait for next frame
                if (elapsed.TotalSeconds < verificationDelaySeconds)
                    return;

                // Detect early process exit before the verification delay
                // This catches crashes that happen within the first few seconds (e.g., port already in use)
                if (!IsProcessRunning(processId))
                {
                    _logger.LogError("MCP server process (PID: {pid}) exited early within {seconds:F1} seconds after launch",
                        processId, elapsed.TotalSeconds);

                    EditorApplication.update -= CheckProcess;
                    if (_serverStatus.CurrentValue == McpServerStatus.Starting)
                        CleanupProcess();
                    return;
                }

                // Process is still running after the verification delay - mark as Running
                _logger.LogDebug("MCP server process (PID: {pid}) is still running after {seconds:F1}s verification",
                    processId, elapsed.TotalSeconds);

                EditorApplication.update -= CheckProcess;
                if (_serverStatus.CurrentValue == McpServerStatus.Starting)
                {
                    _serverStatus.Value = McpServerStatus.Running;
                    _logger.LogInformation("MCP server verified and running (PID: {pid})", processId);
                }
            }

            EditorApplication.update += CheckProcess;
        }

        /// <summary>
        /// Checks if a process with the given ID is still running and is the MCP server.
        /// </summary>
        static bool IsProcessRunning(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process == null || process.HasExited)
                    return false;

                var processName = process.ProcessName.ToLowerInvariant();
                return processName.Contains(McpServerProcessName);
            }
            catch (ArgumentException)
            {
                // Process with this ID does not exist
                return false;
            }
            catch (InvalidOperationException)
            {
                // Process has exited
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error checking process status: {message}", ex.Message);
                return false;
            }
        }

        static void OnProcessExited(object? sender, EventArgs e)
        {
            _logger.LogInformation("MCP server process exited");
            // Marshal to main thread since this event is raised from a thread pool thread
            // and CleanupProcess modifies reactive properties that may be observed on the main thread
            MainThread.Instance.Run(CleanupProcess);
        }

        static void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogDebug("[MCP Server] {output}", e.Data);
            }
        }

        static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogWarning("[MCP Server Error] {error}", e.Data);
            }
        }

        static void CleanupProcess()
        {
            _logger.LogDebug("Cleaning up MCP server process resources");
            lock (_processMutex)
            {
                var processToDispose = _serverProcess;
                _serverProcess = null;

                if (processToDispose != null)
                {
                    processToDispose.Exited -= OnProcessExited;
                    processToDispose.OutputDataReceived -= OnOutputDataReceived;
                    processToDispose.ErrorDataReceived -= OnErrorDataReceived;

                    // Dispose on a background thread to prevent deadlock.
                    // Process.Dispose() can hang on the main thread when redirected
                    // stdout/stderr streams are active, even after CancelOutputRead/CancelErrorRead.
                    Task.Run(() =>
                    {
                        try
                        {
                            try { processToDispose.CancelOutputRead(); } catch { }
                            try { processToDispose.CancelErrorRead(); } catch { }
                            processToDispose.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Error disposing MCP server process: {message}", ex.Message);
                        }
                    });
                }

                EditorPrefs.DeleteKey(ProcessIdKey);
                _serverStatus.Value = McpServerStatus.Stopped;
            }
        }

        /// <summary>
        /// Starts the MCP server if KeepServerRunning is enabled and no external server is detected.
        /// This method is called during Unity Editor startup to auto-start the server based on user preference.
        /// The external server check is performed asynchronously to avoid blocking the main thread.
        /// </summary>
        public static void StartServerIfNeeded()
        {
            EditorApplication.update -= StartServerIfNeeded;

            // Check if user wants the server to keep running
            if (!UnityMcpPlugin.KeepServerRunning)
            {
                _logger.LogDebug("StartServerIfNeeded: KeepServerRunning is false, skipping auto-start");
                return;
            }

            // Check if server is already running (either local or detected from previous session)
            if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                _serverStatus.CurrentValue == McpServerStatus.Starting)
            {
                _logger.LogDebug("StartServerIfNeeded: Server is already running or starting");
                return;
            }

            // Check if an external server is available on the port (non-blocking)
            var port = UnityMcpPlugin.Port;
            CheckExternalServerAsync(port, externalAvailable =>
            {
                if (externalAvailable)
                {
                    _logger.LogInformation("StartServerIfNeeded: External MCP server detected on port {port}, skipping local server start", port);
                    return;
                }

                // Start the local server
                _logger.LogInformation("StartServerIfNeeded: Starting local MCP server (KeepServerRunning=true)");
                StartServer();
            });
        }

        /// <summary>
        /// Checks if an external server is listening on the given port on a background thread,
        /// then invokes the callback on the main thread with the result.
        /// </summary>
        static void CheckExternalServerAsync(int port, Action<bool> onResult)
        {
            Task.Run(() =>
            {
                var result = false;
                try
                {
                    using var client = new System.Net.Sockets.TcpClient();
                    var connectTask = client.ConnectAsync("localhost", port);
                    var completed = connectTask.Wait(500); // 500ms timeout

                    if (completed && client.Connected)
                    {
                        _logger.LogDebug("CheckExternalServerAsync: Port {port} is in use by another process", port);
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("CheckExternalServerAsync: No server detected on port {port} ({message})", port, ex.Message);
                }
                return result;
            })
            .ContinueWith(task => onResult(task.Result), TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Process Lifecycle
    }
}
