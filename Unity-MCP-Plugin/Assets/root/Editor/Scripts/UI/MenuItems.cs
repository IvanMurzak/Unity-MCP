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
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public static class MenuItems
    {
        [MenuItem("Window/AI Game Developer — MCP %&a", priority = 1006)]
        public static void ShowWindow() => MainWindowEditor.ShowWindow();

        [MenuItem("Tools/AI Game Developer/Check for Updates", priority = 999)]
        public static void CheckForUpdates() => _ = UpdateChecker.CheckForUpdatesAsync(forceCheck: true);

        [MenuItem("Tools/AI Game Developer/Server/Download Binaries", priority = 1000)]
        public static Task DownloadServer() => McpServerManager.DownloadAndUnpackBinary();

        [MenuItem("Tools/AI Game Developer/Server/Delete Binaries", priority = 1001)]
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

        [MenuItem("Tools/AI Game Developer/Server/Open Logs", priority = 1002)]
        public static void OpenServerLogs() => OpenFile(McpServerManager.ExecutableFolderPath + "/logs/server-log.txt");

        [MenuItem("Tools/AI Game Developer/Server/Open Log Errors", priority = 1003)]
        public static void OpenServerLogErrors() => OpenFile(McpServerManager.ExecutableFolderPath + "/logs/server-log-error.txt");

        [MenuItem("Tools/AI Game Developer/Server/Launch MCP Inspector", priority = 1004)]
        public static void LaunchMcpInspector()
        {
            if (UnityMcpPluginEditor.TransportMethod != TransportMethod.streamableHttp)
            {
                NotificationPopupWindow.Show(
                    windowTitle: "Error",
                    title: "HTTP Transport required",
                    message: "The MCP Inspector can only be launched when the transport method is set to HTTP. Please change the transport method in the plugin settings and try again.",
                    width: 350,
                    minWidth: 350,
                    height: 200,
                    minHeight: 200);
                return;
            }

            // Run command in a terminal window: npx @modelcontextprotocol/inspector http://localhost:8080 --transport http
            var npxArgs = $"-y @modelcontextprotocol/inspector {UnityMcpPluginEditor.Host} --transport http";
            Debug.Log($"Launching MCP Inspector with command: npx {npxArgs}");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npx",
                Arguments = npxArgs,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            try
            {
                System.Diagnostics.Process.Start(processInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                var command = $"{processInfo.FileName} {processInfo.Arguments}";
                NotificationPopupWindow.Show(
                    windowTitle: "Launch Failed",
                    title: "Unable to start MCP Inspector",
                    message:
                        "The MCP Inspector could not be started from Unity.\n\n" +
                        "This usually means that Node.js (and npx) is not installed, or 'npx' is not available on your PATH.\n\n" +
                        "Prerequisites:\n" +
                        " - Install Node.js (which includes npx)\n" +
                        " - Ensure 'npx' is available from your terminal/command prompt\n\n" +
                        "You can try running the following command manually in a terminal:\n" +
                        command + "\n\n" +
                        "System error:\n" +
                        ex.Message,
                    width: 450,
                    minWidth: 450,
                    height: 460,
                    minHeight: 460);
            }
            catch (System.Exception ex)
            {
                var command = $"{processInfo.FileName} {processInfo.Arguments}";
                NotificationPopupWindow.Show(
                    windowTitle: "Launch Failed",
                    title: "Unexpected error starting MCP Inspector",
                    message:
                        "An unexpected error occurred while trying to start the MCP Inspector.\n\n" +
                        "You can try running the following command manually in a terminal:\n" +
                        command + "\n\n" +
                        "Error details:\n" +
                        ex.Message,
                    width: 450,
                    minWidth: 450,
                    height: 460,
                    minHeight: 460);
            }
        }

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