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
using System.Runtime.InteropServices;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;
using McpConsts = com.IvanMurzak.McpPlugin.Common.Consts;

namespace com.IvanMurzak.Unity.MCP.Editor
{
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
    /// Manages the MCP server process lifecycle independently from UI.
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
            CheckExistingProcess();
        }

        static void CheckExistingProcess()
        {
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
            StopServer();
        }

        public static bool StartServer()
        {
            lock (_processMutex)
            {
                if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                    _serverStatus.CurrentValue == McpServerStatus.Starting)
                {
                    _logger.LogWarning("MCP server is already running or starting");
                    return false;
                }

                if (!Startup.Server.IsBinaryExists())
                {
                    _logger.LogError("MCP server binary not found at: {path}", Startup.Server.ExecutableFullPath);
                    return false;
                }

                _serverStatus.Value = McpServerStatus.Starting;

                try
                {
                    var executablePath = Startup.Server.ExecutableFullPath;
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
                        WorkingDirectory = Startup.Server.ExecutableFolderPath
                    };

                    // Set executable permissions on Unix-like systems
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Utils.UnixUtils.Set0755(executablePath);
                    }

                    _serverProcess = new Process { StartInfo = startInfo };
                    _serverProcess.EnableRaisingEvents = true;
                    _serverProcess.Exited += OnProcessExited;
                    _serverProcess.OutputDataReceived += OnOutputDataReceived;
                    _serverProcess.ErrorDataReceived += OnErrorDataReceived;

                    if (!_serverProcess.Start())
                    {
                        _logger.LogError("Failed to start MCP server process");
                        _serverStatus.Value = McpServerStatus.Stopped;
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
                    _serverStatus.Value = McpServerStatus.Stopped;
                    return false;
                }
            }
        }

        public static bool StopServer()
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
                        // Try graceful shutdown first
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // On Windows, use taskkill for graceful termination
                            _serverProcess.Kill();
                        }
                        else
                        {
                            // On Unix-like systems, send SIGTERM first
                            try
                            {
                                // Try to terminate gracefully using SIGTERM via kill command
                                using var killProcess = Process.Start(new ProcessStartInfo
                                {
                                    FileName = "kill",
                                    Arguments = $"-TERM {_serverProcess.Id}",
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                });
                                killProcess?.WaitForExit(1000);
                            }
                            catch (Exception ex)
                            {
                                // Fallback to Kill() if SIGTERM fails
                                _logger.LogDebug("SIGTERM failed, falling back to Kill(): {message}", ex.Message);
                                _serverProcess.Kill();
                            }
                        }

                        // Wait for process to exit with timeout
                        if (!_serverProcess.WaitForExit(5000))
                        {
                            // Force kill if it doesn't exit gracefully
                            _logger.LogWarning("MCP server did not exit gracefully, forcing termination");
                            _serverProcess.Kill();
                            _serverProcess.WaitForExit(2000);
                        }
                    }

                    CleanupProcess();
                    _logger.LogInformation("MCP server stopped successfully");
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

        static string BuildArguments()
        {
            var port = UnityMcpPlugin.Port;
            var timeout = UnityMcpPlugin.TimeoutMs;

            // Arguments format: port=XXXXX plugin-timeout=XXXXX client-transport=streamableHttp
            return $"{McpConsts.MCP.Server.Args.Port}={port} {McpConsts.MCP.Server.Args.PluginTimeout}={timeout} {McpConsts.MCP.Server.Args.ClientTransportMethod}=streamableHttp";
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
                var elapsed = DateTime.UtcNow - startTime;

                // If we haven't reached 5 seconds yet, schedule another check
                if (elapsed.TotalSeconds < verificationDelaySeconds)
                {
                    EditorApplication.delayCall += CheckProcess;
                    return;
                }

                lock (_processMutex)
                {
                    // Only check if we're still in Starting state (not stopped/stopping by user)
                    if (_serverStatus.CurrentValue != McpServerStatus.Starting)
                        return;

                    // Verify the process still exists
                    if (IsProcessRunning(processId))
                    {
                        // Process verified successfully - now we can set status to Running
                        _serverStatus.Value = McpServerStatus.Running;
                        _logger.LogInformation("MCP server verified and running (PID: {pid})", processId);
                    }
                    else
                    {
                        _logger.LogError("MCP server process (PID: {pid}) exited unexpectedly within {seconds:F1} seconds after launch", processId, elapsed.TotalSeconds);
                        CleanupProcess();
                    }
                }
            }

            EditorApplication.delayCall += CheckProcess;
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
            lock (_processMutex)
            {
                if (_serverProcess != null)
                {
                    _serverProcess.Exited -= OnProcessExited;
                    _serverProcess.OutputDataReceived -= OnOutputDataReceived;
                    _serverProcess.ErrorDataReceived -= OnErrorDataReceived;
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }

                EditorPrefs.DeleteKey(ProcessIdKey);
                _serverStatus.Value = McpServerStatus.Stopped;
            }
        }

        public static void ToggleServer()
        {
            if (IsRunning)
                StopServer();
            else
                StartServer();
        }

        /// <summary>
        /// Starts the MCP server if KeepServerRunning is enabled and no external server is detected.
        /// This method is called during Unity Editor startup to auto-start the server based on user preference.
        /// </summary>
        /// <returns>True if the server was started or is already running, false otherwise.</returns>
        public static bool StartServerIfNeeded()
        {
            // Check if user wants the server to keep running
            if (!UnityMcpPlugin.KeepServerRunning)
            {
                _logger.LogDebug("StartServerIfNeeded: KeepServerRunning is false, skipping auto-start");
                return false;
            }

            // Check if server is already running (either local or detected from previous session)
            if (_serverStatus.CurrentValue == McpServerStatus.Running ||
                _serverStatus.CurrentValue == McpServerStatus.Starting)
            {
                _logger.LogDebug("StartServerIfNeeded: Server is already running or starting");
                return true;
            }

            // Check if an external server is available on the port
            if (IsExternalServerAvailable())
            {
                _logger.LogInformation("StartServerIfNeeded: External MCP server detected on port {port}, skipping local server start", UnityMcpPlugin.Port);
                return false;
            }

            // Start the local server
            _logger.LogInformation("StartServerIfNeeded: Starting local MCP server (KeepServerRunning=true)");
            return StartServer();
        }

        /// <summary>
        /// Checks if an external MCP server is already listening on the configured port.
        /// Uses a quick TCP connection attempt to detect if the port is in use.
        /// </summary>
        /// <remarks>
        /// Note: This method only checks if any TCP listener is on the port, not specifically
        /// an MCP server. This could produce false positives if another service uses the same port.
        /// For more accurate detection, consider implementing an MCP-specific health check endpoint.
        /// </remarks>
        /// <returns>True if an external server is detected, false otherwise.</returns>
        public static bool IsExternalServerAvailable()
        {
            try
            {
                var port = UnityMcpPlugin.Port;
                using var client = new System.Net.Sockets.TcpClient();

                // Try to connect with a short timeout
                var connectTask = client.ConnectAsync("localhost", port);
                var completed = connectTask.Wait(500); // 500ms timeout

                if (completed && client.Connected)
                {
                    _logger.LogDebug("IsExternalServerAvailable: Port {port} is in use by another process", port);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("IsExternalServerAvailable: No server detected on port {port} ({message})", UnityMcpPlugin.Port, ex.Message);
                return false;
            }
        }
    }
}
