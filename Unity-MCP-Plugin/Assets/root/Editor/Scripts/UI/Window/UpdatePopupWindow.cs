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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// A popup window that notifies the user when a new version of AI Game Developer is available.
    /// </summary>
    public class UpdatePopupWindow : EditorWindow
    {
        private static readonly string[] WindowUxmlPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/UpdatePopupWindow.uxml",
            "Assets/root/Editor/UI/uxml/UpdatePopupWindow.uxml"
        };

        private static readonly string[] WindowUssPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uss/UpdatePopupWindow.uss",
            "Assets/root/Editor/UI/uss/UpdatePopupWindow.uss"
        };

        private static readonly string[] LogoIconPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/Gizmos/logo_512.png",
            "Assets/root/Editor/Gizmos/logo_512.png"
        };

        private string _currentVersion = string.Empty;
        private string _latestVersion = string.Empty;

        /// <summary>
        /// Shows the update popup window with version information.
        /// </summary>
        public static UpdatePopupWindow ShowWindow(string currentVersion, string latestVersion)
        {
            var window = GetWindow<UpdatePopupWindow>(true, "Update Available", true);
            window._currentVersion = currentVersion;
            window._latestVersion = latestVersion;

            // Set window size and position (center on screen)
            var windowWidth = 350;
            var windowHeight = 410;
            var x = windowWidth / 2;
            var y = windowHeight / 2;

            window.minSize = new Vector2(windowWidth, windowHeight);
            window.maxSize = new Vector2(windowWidth, windowHeight);
            window.position = new Rect(x, y, windowWidth, windowHeight);

            window.ShowUtility();
            window.Focus();

            return window;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            // Apply style-sheet first
            ApplyStyleSheets(rootVisualElement);

            // Try to load UXML template
            var visualTree = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPaths);
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
                BindUI(rootVisualElement);
            }
        }

        private void ApplyStyleSheets(VisualElement root)
        {
            var sheet = EditorAssetLoader.LoadAssetAtPath<StyleSheet>(WindowUssPaths);
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }
            else
            {
                UnityMcpPlugin.Instance.LogWarn("Failed to load USS from paths: {paths}", typeof(UpdatePopupWindow), string.Join(", ", WindowUssPaths));
            }
        }

        private void BindUI(VisualElement root)
        {
            // Set icon
            var iconContainer = root.Q<VisualElement>("icon-container");
            if (iconContainer != null)
            {
                var icon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(LogoIconPaths);
                if (icon != null)
                    iconContainer.style.backgroundImage = new StyleBackground(icon);
            }

            // Set version labels
            var currentVersionLabel = root.Q<Label>("current-version-value");
            if (currentVersionLabel != null)
                currentVersionLabel.text = _currentVersion;

            var latestVersionLabel = root.Q<Label>("latest-version-value");
            if (latestVersionLabel != null)
                latestVersionLabel.text = _latestVersion;

            // Bind buttons
            var installUpdateButton = root.Q<Button>("btn-install-update");
            if (installUpdateButton != null)
                installUpdateButton.clicked += OnInstallUpdateClicked;

            var viewReleasesButton = root.Q<Button>("btn-view-releases");
            if (viewReleasesButton != null)
                viewReleasesButton.clicked += OnViewReleasesClicked;

            var skipVersionButton = root.Q<Button>("btn-skip-version");
            if (skipVersionButton != null)
                skipVersionButton.clicked += OnSkipVersionClicked;
        }

        private void OnInstallUpdateClicked()
        {
            // TODO: Implement auto-update installation
        }

        private void OnViewReleasesClicked()
        {
            Application.OpenURL(UpdateChecker.ReleasesUrl);
            Close();
        }

        private void OnSkipVersionClicked()
        {
            UpdateChecker.SkipVersion(_latestVersion);
            Close();
        }
    }
}
