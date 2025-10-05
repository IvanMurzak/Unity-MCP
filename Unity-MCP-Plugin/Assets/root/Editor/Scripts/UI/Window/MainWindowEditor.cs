/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using com.IvanMurzak.Unity.MCP.Utils;
using R3;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public partial class MainWindowEditor : EditorWindow
    {
        readonly CompositeDisposable _disposables = new();

        public static MainWindowEditor ShowWindow()
        {
            var window = GetWindow<MainWindowEditor>();
            window.titleContent = new GUIContent(text: "AI Game Developer");
            window.Focus();

            return window;
        }
        public static void ShowWindowVoid() => ShowWindow();

        public void Invalidate() => CreateGUI();
        void OnValidate() => McpPluginUnity.Validate();

        private void SaveChanges(string message)
        {
            if (McpPluginUnity.IsLogActive(LogLevel.Info))
                Debug.Log(message);

            saveChangesMessage = message;

            Undo.RecordObject(McpPluginUnity.AssetFile, message); // Undo record started
            base.SaveChanges();
            McpPluginUnity.Save();
            McpPluginUnity.InvalidateAssetFile();
            EditorUtility.SetDirty(McpPluginUnity.AssetFile); // Undo record completed
        }

        private void OnChanged(McpPluginUnity.Data data) => Repaint();

        private void OnEnable()
        {
            _disposables.Add(McpPluginUnity.SubscribeOnChanged(OnChanged));
        }
        private void OnDisable()
        {
            _disposables.Clear();
        }
    }
}