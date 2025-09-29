/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Manages file-based storage and retrieval of log entries
    /// </summary>
    internal static class LogFileManager
    {
        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB max file size
        private const int MaxLogEntriesInFile = 10000; // Max entries before rotation
        private const string LogFileName = "editor-logs.txt";
        private static readonly object _fileLock = new object();
        
        private static string LogDirectoryPath => Path.Combine(Application.dataPath, "..", "Temp", "mcp-server");
        private static string LogFilePath => Path.Combine(LogDirectoryPath, LogFileName);

        /// <summary>
        /// Ensures the log directory exists
        /// </summary>
        private static void EnsureLogDirectory()
        {
            if (!Directory.Exists(LogDirectoryPath))
            {
                Directory.CreateDirectory(LogDirectoryPath);
            }
        }

        /// <summary>
        /// Appends a log entry to the file
        /// </summary>
        public static void AppendLogEntry(LogEntry logEntry)
        {
            lock (_fileLock)
            {
                try
                {
                    EnsureLogDirectory();
                    
                    // Check if file rotation is needed
                    if (ShouldRotateFile())
                    {
                        RotateLogFile();
                    }

                    // Serialize log entry to JSON and append to file
                    var json = JsonUtility.ToJson(logEntry);
                    File.AppendAllText(LogFilePath, json + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to append log entry to file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reads log entries from file with filtering options
        /// </summary>
        public static LogEntry[] ReadLogEntries(int maxEntries = int.MaxValue, LogType? filterType = null, DateTime? sinceTime = null)
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(LogFilePath))
                    {
                        return new LogEntry[0];
                    }

                    var lines = File.ReadAllLines(LogFilePath);
                    var logEntries = new List<LogEntry>();

                    // Read from end of file backwards for efficiency (most recent entries first)
                    for (int i = lines.Length - 1; i >= 0 && logEntries.Count < maxEntries; i--)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;

                        try
                        {
                            var logEntry = JsonUtility.FromJson<LogEntry>(lines[i]);
                            
                            // Apply filters
                            if (filterType.HasValue && logEntry.logType != filterType.Value)
                                continue;
                                
                            if (sinceTime.HasValue && logEntry.timestamp < sinceTime.Value)
                                continue;

                            logEntries.Add(logEntry);
                        }
                        catch (Exception)
                        {
                            // Skip malformed JSON lines
                            continue;
                        }
                    }

                    // Reverse to get chronological order (oldest first)
                    logEntries.Reverse();
                    return logEntries.ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read log entries from file: {ex.Message}");
                    return new LogEntry[0];
                }
            }
        }

        /// <summary>
        /// Gets the last N log entries efficiently
        /// </summary>
        public static LogEntry[] GetLastLogEntries(int count, LogType? filterType = null)
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(LogFilePath))
                    {
                        return new LogEntry[0];
                    }

                    var lines = File.ReadAllLines(LogFilePath);
                    var logEntries = new List<LogEntry>();

                    // Read from end of file backwards for efficiency
                    for (int i = lines.Length - 1; i >= 0 && logEntries.Count < count; i--)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;

                        try
                        {
                            var logEntry = JsonUtility.FromJson<LogEntry>(lines[i]);
                            
                            // Apply log type filter if specified
                            if (filterType.HasValue && logEntry.logType != filterType.Value)
                                continue;

                            logEntries.Add(logEntry);
                        }
                        catch (Exception)
                        {
                            // Skip malformed JSON lines
                            continue;
                        }
                    }

                    // Reverse to get chronological order
                    logEntries.Reverse();
                    return logEntries.ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get last log entries from file: {ex.Message}");
                    return new LogEntry[0];
                }
            }
        }

        /// <summary>
        /// Checks if the log file should be rotated
        /// </summary>
        private static bool ShouldRotateFile()
        {
            if (!File.Exists(LogFilePath))
                return false;

            var fileInfo = new FileInfo(LogFilePath);
            
            // Rotate if file is too large or has too many entries
            if (fileInfo.Length > MaxFileSize)
                return true;

            // Check line count (approximation)
            var lineCount = File.ReadAllLines(LogFilePath).Length;
            return lineCount > MaxLogEntriesInFile;
        }

        /// <summary>
        /// Rotates the log file to prevent unlimited growth
        /// </summary>
        private static void RotateLogFile()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                // Keep only the most recent 50% of entries when rotating
                var lines = File.ReadAllLines(LogFilePath);
                var keepCount = lines.Length / 2;
                
                if (keepCount > 0)
                {
                    var linesToKeep = lines.Skip(lines.Length - keepCount);
                    File.WriteAllLines(LogFilePath, linesToKeep);
                }
                else
                {
                    // If file is very small, just clear it
                    File.WriteAllText(LogFilePath, "");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to rotate log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all log entries from the file
        /// </summary>
        public static void ClearLogFile()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.WriteAllText(LogFilePath, "");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to clear log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the count of log entries in the file
        /// </summary>
        public static int GetLogEntryCount()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(LogFilePath))
                        return 0;

                    return File.ReadAllLines(LogFilePath)
                        .Count(line => !string.IsNullOrWhiteSpace(line));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
    }
}