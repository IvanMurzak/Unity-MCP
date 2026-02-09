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

using System;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// A non-blocking popup window for displaying notification messages.
    /// Uses ShowUtility() so it floats above the editor without blocking interaction.
    /// Serves as a base class for popup windows with customizable UXML/USS and UI binding.
    /// </summary>
    public class NotificationPopupWindow : McpWindowBase
    {
        static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/NotificationPopupWindow.uxml");
        static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/NotificationPopupWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => _title;

        public const float DefaultWidth = 350;
        public const float DefaultHeight = 200;
        public const float DefaultMinWidth = 350;
        public const float DefaultMinHeight = 100;
        public const float DefaultMaxWidth = 350;
        public const float DefaultMaxHeight = 300;

        string _title = "Notification";
        string _message = string.Empty;

        protected static void CenterAndSizeWindow(EditorWindow window, float width, float height, float minWidth, float minHeight, float maxWidth, float maxHeight)
        {
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var x = mainWindowRect.x + (mainWindowRect.width - width) / 2f;
            var y = mainWindowRect.y + (mainWindowRect.height - height) / 2f;

            window.minSize = new Vector2(minWidth, minHeight);
            window.maxSize = new Vector2(maxWidth, maxHeight);
            window.position = new Rect(x, y, width, height);
        }

        public static void Show(
            string windowTitle,
            string title,
            string message,
            float width = DefaultWidth,
            float height = DefaultHeight,
            float minWidth = DefaultMinWidth,
            float minHeight = DefaultMinHeight,
            float maxWidth = DefaultMaxWidth,
            float maxHeight = DefaultMaxHeight)
        {
            var window = GetWindow<NotificationPopupWindow>(utility: false, title: windowTitle, focus: true); // CreateInstance<NotificationPopupWindow>();
            window._title = title;
            window._message = message;
            window.titleContent = new GUIContent(windowTitle);
            window.SetupWindowWithIcon(windowTitle);

            CenterAndSizeWindow(window, width, height, minWidth, minHeight, maxWidth, maxHeight);

            window.CreateGUI();
            window.ShowUtility();
        }

        public override void CreateGUI()
        {
            rootVisualElement.Clear();
            ApplyStyleSheets(rootVisualElement);

            var visualTree = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPaths);
            if (visualTree == null)
                throw new InvalidOperationException("UXML template not found in specified paths");

            visualTree.CloneTree(rootVisualElement);
            BindUI(rootVisualElement);
        }

        protected virtual void BindUI(VisualElement root)
        {
            var titleLabel = root.Q<Label>("title");
            if (titleLabel != null)
                titleLabel.text = _title;

            var messageLabel = root.Q<Label>("message");
            if (messageLabel != null)
                messageLabel.text = _message;

            var okButton = root.Q<Button>("btn-ok");
            if (okButton != null)
                okButton.clicked += Close;
        }
    }
}
