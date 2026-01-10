/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using UnityEditor;
using UnityEngine.UIElements;
using com.IvanMurzak.Unity.MCP.Editor.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Editor window for checking MCP tool status and diagnostics.
    /// Analyzed from HTML mockup and implemented as a Unity Editor Window.
    /// </summary>
    public class McpStatusChecksWindow : McpWindowBase
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpStatusChecksWindow.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpStatusChecksWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "MCP Status Checks";

        /// <summary>
        /// Shows the MCP Status Checks window.
        /// </summary>
        [MenuItem("Window/MCP/Status Checks")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpStatusChecksWindow>("MCP Status Checks");
            window.SetupWindowWithIcon();
            window.minSize = new UnityEngine.Vector2(400, 500);
            window.Show();
            window.Focus();
        }

        protected override void OnGUICreated(VisualElement root)
        {
            base.OnGUICreated(root);
            
            // Proactive enhancement: add click handlers for cards if needed in future
            root.Query<VisualElement>(null, "status-card").ForEach(card => {
                card.RegisterCallback<MouseDownEvent>(evt => {
                    // Logic for expansion or troubleshooting can be added here
                });
            });
        }
    }
}
