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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static class MenuItems
    {
        [MenuItem("Window/AI Game Developer (Unity-MCP) %&a", priority = 1006)]
        public static void ShowWindow() => MainWindowEditor.ShowWindow();

        [MenuItem("Tools/AI Game Developer/Download Server Binaries", priority = 1000)]
        public static Task DownloadServer() => Startup.Server.DownloadAndUnpackBinary();

        [MenuItem("Tools/AI Game Developer/Delete Server Binaries", priority = 1001)]
        public static void DeleteServer() => Startup.Server.DeleteBinaryFolderIfExists();

        [MenuItem("Tools/AI Game Developer/Open Server Logs", priority = 1002)]
        public static void OpenServerLogs() => OpenFile(Startup.Server.ExecutableFolderPath + "/logs/server-log.txt");

        [MenuItem("Tools/AI Game Developer/Open Server Log errors", priority = 1003)]
        public static void OpenServerLogErrors() => OpenFile(Startup.Server.ExecutableFolderPath + "/logs/server-log-error.txt");

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