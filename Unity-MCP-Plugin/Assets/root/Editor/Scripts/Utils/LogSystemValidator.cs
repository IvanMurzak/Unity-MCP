/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Validation utilities for the file-based logging system
    /// </summary>
    public static class LogSystemValidator
    {
        [MenuItem("Tools/MCP/Validate Log System")]
        public static void ValidateLogSystem()
        {
            Debug.Log("=== MCP Log System Validation ===");
            
            // Test basic functionality
            var testId = System.Guid.NewGuid().ToString("N")[..8];
            
            Debug.Log($"Test message {testId}");
            Debug.LogWarning($"Test warning {testId}");
            Debug.LogError($"Test error {testId}");
            
            // Wait a bit for file operations
            System.Threading.Thread.Sleep(100);
            
            // Check file location
            var logPath = Path.Combine(Application.dataPath, "..", "Temp", "mcp-server", "editor-logs.txt");
            if (File.Exists(logPath))
            {
                var fileInfo = new FileInfo(logPath);
                Debug.Log($"✓ Log file exists at: {logPath} (Size: {fileInfo.Length} bytes)");
                
                // Check file content
                var lines = File.ReadAllLines(logPath);
                Debug.Log($"✓ Log file contains {lines.Length} entries");
                
                // Show last few entries
                var lastEntries = System.Math.Min(3, lines.Length);
                Debug.Log($"Last {lastEntries} entries:");
                for (int i = lines.Length - lastEntries; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        try
                        {
                            var entry = JsonUtility.FromJson<LogEntry>(lines[i]);
                            Debug.Log($"  [{entry.timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.logTypeString}] {entry.message}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"  Failed to parse line {i}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"✗ Log file not found at: {logPath}");
            }
            
            // Test LogUtils API
            var allLogs = LogUtils.GetAllLogs();
            Debug.Log($"✓ LogUtils.GetAllLogs() returned {allLogs.Length} entries");
            
            var lastLogs = LogUtils.GetLastLogs(5);
            Debug.Log($"✓ LogUtils.GetLastLogs(5) returned {lastLogs.Length} entries");
            
            var errorLogs = LogUtils.GetLastLogs(10, LogType.Error);
            Debug.Log($"✓ LogUtils.GetLastLogs(10, LogType.Error) returned {errorLogs.Length} entries");
            
            // Test Console.GetLogs integration
            var consoleTool = new API.Tool_Console();
            var consoleResult = consoleTool.GetLogs(maxEntries: 10);
            Debug.Log($"✓ Console.GetLogs() result length: {consoleResult.Length} chars");
            
            Debug.Log("=== Validation Complete ===");
        }
        
        [MenuItem("Tools/MCP/Clear Log System")]
        public static void ClearLogSystem()
        {
            LogUtils.ClearLogs();
            Debug.Log("✓ Log system cleared");
            
            var logPath = Path.Combine(Application.dataPath, "..", "Temp", "mcp-server", "editor-logs.txt");
            if (!File.Exists(logPath))
            {
                Debug.Log("✓ Log file successfully removed");
            }
            else
            {
                var fileInfo = new FileInfo(logPath);
                Debug.Log($"Log file size after clear: {fileInfo.Length} bytes");
            }
        }
        
        [MenuItem("Tools/MCP/Generate Test Logs")]
        public static void GenerateTestLogs()
        {
            var testId = System.Guid.NewGuid().ToString("N")[..6];
            
            for (int i = 0; i < 20; i++)
            {
                switch (i % 4)
                {
                    case 0:
                        Debug.Log($"Test log {i} - {testId}");
                        break;
                    case 1:
                        Debug.LogWarning($"Test warning {i} - {testId}");
                        break;
                    case 2:
                        Debug.LogError($"Test error {i} - {testId}");
                        break;
                    case 3:
                        Debug.Log($"Another log {i} - {testId}");
                        break;
                }
                
                if (i % 5 == 0)
                {
                    System.Threading.Thread.Sleep(10); // Create some time separation
                }
            }
            
            Debug.Log($"✓ Generated 20 test logs with ID: {testId}");
        }
    }
}
#endif