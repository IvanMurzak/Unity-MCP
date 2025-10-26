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
        void OnValidate() => UnityMcpPlugin.Validate();

        private void SaveChanges(string message)
        {
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log(message);

            saveChangesMessage = message;

            Undo.RecordObject(UnityMcpPlugin.AssetFile, message); // Undo record started
            base.SaveChanges();
            UnityMcpPlugin.Save();
            UnityMcpPlugin.InvalidateAssetFile();
            EditorUtility.SetDirty(UnityMcpPlugin.AssetFile); // Undo record completed
        }

        private void OnChanged(UnityMcpPlugin.Data data) => Repaint();

        private void OnEnable()
        {
            _disposables.Add(UnityMcpPlugin.SubscribeOnChanged(OnChanged));
        }
        private void OnDisable()
        {
            _disposables.Clear();
        }
    }
}