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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using R3;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor : McpWindowBase
    {
        readonly CompositeDisposable _disposables = new();

        protected override string WindowTitle => "Game Developer";
        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;

        public static MainWindowEditor ShowWindow()
        {
            var window = GetWindow<MainWindowEditor>("Game Developer");
            window.SetupWindowWithIcon();
            window.Focus();

            return window;
        }
        public static void ShowWindowVoid() => ShowWindow();

        public void Invalidate() => CreateGUI();
        void OnValidate() => UnityMcpPlugin.Instance.Validate();

        private void SaveChanges(string message)
        {
            if (UnityMcpPlugin.IsLogEnabled(LogLevel.Info))
                Debug.Log(message);

            saveChangesMessage = message;

            base.SaveChanges();
            UnityMcpPlugin.Instance.Save();
        }

        private void OnChanged(UnityMcpPlugin.UnityConnectionConfig data) => Repaint();

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposables.Add(UnityMcpPlugin.SubscribeOnChanged(OnChanged));
        }
        private void OnDisable()
        {
            _disposables.Clear();
        }

        private static void UnityBuildAndConnect()
        {
            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
            UnityMcpPlugin.ConnectIfNeeded();
        }
    }
}