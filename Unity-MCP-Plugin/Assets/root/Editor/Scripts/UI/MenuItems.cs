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
#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public static class MenuItems
    {
        [MenuItem("Window/AI Game Developer (Unity-MCP) %&a", priority = 1006)]
        public static void ShowWindow() => MainWindowEditor.ShowWindow();

        [MenuItem("Tools/AI Game Developer/Check for Updates", priority = 999)]
        public static void CheckForUpdates() => _ = UpdateChecker.CheckForUpdatesAsync(forceCheck: true);

        [MenuItem("Tools/AI Game Developer/Download Server Binaries", priority = 1000)]
        public static Task DownloadServer() => McpServerManager.DownloadAndUnpackBinary();

        [MenuItem("Tools/AI Game Developer/Delete Server Binaries", priority = 1001)]
        public static void DeleteServer()
        {
            var result = McpServerManager.DeleteBinaryFolderIfExists();
            if (result)
            {
                NotificationPopupWindow.Show(
                    windowTitle: "Success",
                    title: "MCP Server Binaries Deleted",
                    message: "The MCP server binaries were successfully deleted. You can download them again from the Tools menu.",
                    width: 350,
                    minWidth: 350,
                    height: 200,
                    minHeight: 200);
            }
            else
            {
                NotificationPopupWindow.Show(
                    windowTitle: "Error",
                    title: "MCP Server Binaries Not Found",
                    message: "No MCP server binaries were found to delete. They may have already been deleted or were never downloaded.",
                    width: 350,
                    minWidth: 350,
                    height: 200,
                    minHeight: 200);
            }
        }

        [MenuItem("Tools/AI Game Developer/Open Server Logs", priority = 1002)]
        public static void OpenServerLogs() => OpenFile(McpServerManager.ExecutableFolderPath + "/logs/server-log.txt");

        [MenuItem("Tools/AI Game Developer/Open Server Log errors", priority = 1003)]
        public static void OpenServerLogErrors() => OpenFile(McpServerManager.ExecutableFolderPath + "/logs/server-log-error.txt");

        [MenuItem("Tools/AI Game Developer/Debug/Show Update Popup", priority = 2000)]
        public static void ShowUpdatePopup() => UpdatePopupWindow.ShowWindow(UnityMcpPlugin.Version, "99.99.99");

        [MenuItem("Tools/AI Game Developer/Debug/Reset Update Preferences", priority = 2001)]
        public static void ResetUpdatePreferences()
        {
            UpdateChecker.ClearPreferences();
            Debug.Log("Update preferences have been reset.");
        }

        [MenuItem("Tools/AI Game Developer/Debug/Serialization Check", priority = 2002)]
        public static void ShowSerializationCheck() => SerializationCheckWindow.ShowWindow();

        static void OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"File not found: {path}");
                return;
            }
            Application.OpenURL(path);
        }
    }
}
#endif