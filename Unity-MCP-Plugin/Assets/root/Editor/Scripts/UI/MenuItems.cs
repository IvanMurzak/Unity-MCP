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
using System.Threading.Tasks;
using UnityEditor;

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
    }
}
#endif