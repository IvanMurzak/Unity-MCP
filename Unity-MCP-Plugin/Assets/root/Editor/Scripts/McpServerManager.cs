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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public enum McpServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping
    }

    /// <summary>
    /// Manages the MCP server process lifecycle independently from UI.
    /// Provides cross-platform support for Windows, macOS, and Linux.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerManager
    {
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
            var savedPid = EditorPrefs.GetInt("McpServerManager_ProcessId", -1);
            if (savedPid > 0)
            {
                try
                {
                    var process = Process.GetProcessById(savedPid);
                    if (process != null && !process.HasExited)
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        if (processName.Contains("unity-mcp-server"))
                        {
                            _serverProcess = process;
                            _serverStatus.Value = McpServerStatus.Running;
                            _logger.LogInformation("Reconnected to existing MCP server process (PID: {pid})", savedPid);

                            // Re-attach exit handler
                            process.EnableRaisingEvents = true;
                            process.Exited += OnProcessExited;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not reconnect to previous process: {message}", ex.Message);
                }

                // Clear stale PID
                EditorPrefs.DeleteKey("McpServerManager_ProcessId");
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
                    EditorPrefs.SetInt("McpServerManager_ProcessId", _serverProcess.Id);

                    _serverStatus.Value = McpServerStatus.Running;
                    _logger.LogInformation("MCP server started successfully (PID: {pid})", _serverProcess.Id);

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
                    EditorPrefs.DeleteKey("McpServerManager_ProcessId");
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
                            catch
                            {
                                // Fallback to Kill() if SIGTERM fails
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
            return $"{Consts.MCP.Server.Args.Port}={port} {Consts.MCP.Server.Args.PluginTimeout}={timeout} {Consts.MCP.Server.Args.ClientTransportMethod}=streamableHttp";
        }

        static void OnProcessExited(object? sender, EventArgs e)
        {
            _logger.LogInformation("MCP server process exited");
            CleanupProcess();
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

                EditorPrefs.DeleteKey("McpServerManager_ProcessId");
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
    }
}
