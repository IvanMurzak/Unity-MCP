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
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
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

        private const string PackageId = "com.ivanmurzak.unity.mcp";

        private string currentVersion = string.Empty;
        private string latestVersion = string.Empty;
        private AddRequest? addRequest;

        /// <summary>
        /// Shows the update popup window with version information.
        /// </summary>
        public static UpdatePopupWindow ShowWindow(string currentVersion, string latestVersion)
        {
            var window = GetWindow<UpdatePopupWindow>(utility: false, "Update Available", focus: true);
            window.currentVersion = currentVersion ?? "Unknown";
            window.latestVersion = latestVersion ?? "Unknown";

            // Set window size and position (center on screen)
            var windowWidth = 350;
            var windowHeight = 450;

            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var x = mainWindowRect.x + (mainWindowRect.width - windowWidth) / 2f;
            var y = mainWindowRect.y + (mainWindowRect.height - windowHeight) / 2f;

            window.minSize = new Vector2(windowWidth, windowHeight);
            window.maxSize = new Vector2(windowWidth, windowHeight);
            window.position = new Rect(x, y, windowWidth, windowHeight);

            window.CreateGUI();
            window.Show();
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
            if (visualTree == null)
                throw new System.InvalidOperationException("UXML template not found in specified paths");
            visualTree.CloneTree(rootVisualElement);
            BindUI(rootVisualElement);
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
                UnityMcpPlugin.Instance.LogWarn("Failed to load USS from paths: {paths}",
                    typeof(UpdatePopupWindow), string.Join(", ", WindowUssPaths));
            }
        }

        private void BindUI(VisualElement root)
        {
            // Set icon
            var iconContainer = root.Q<VisualElement>("icon-container");
            if (iconContainer == null)
                throw new System.InvalidOperationException("icon-container VisualElement not found in UXML");
            var icon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(LogoIconPaths);
            if (icon == null)
                throw new System.InvalidOperationException("Logo icon not found in specified paths");
            iconContainer.style.backgroundImage = new StyleBackground(icon);

            // Set version labels
            var currentVersionLabel = root.Q<Label>("current-version-value");
            if (currentVersionLabel == null)
                throw new System.InvalidOperationException("current-version-value Label not found in UXML");
            currentVersionLabel.text = currentVersion;

            var latestVersionLabel = root.Q<Label>("latest-version-value");
            if (latestVersionLabel == null)
                throw new System.InvalidOperationException("latest-version-value Label not found in UXML");
            latestVersionLabel.text = latestVersion;

            // Bind buttons
            var installUpdateButton = root.Q<Button>("btn-install-update");
            if (installUpdateButton == null)
                throw new System.InvalidOperationException("btn-install-update Button not found in UXML");
            installUpdateButton.clicked += OnInstallUpdateClicked;

            var viewReleasesButton = root.Q<Button>("btn-view-releases");
            if (viewReleasesButton == null)
                throw new System.InvalidOperationException("btn-view-releases Button not found in UXML");
            viewReleasesButton.clicked += OnViewReleasesClicked;

            var skipVersionButton = root.Q<Button>("btn-skip-version");
            if (skipVersionButton == null)
                throw new System.InvalidOperationException("btn-skip-version Button not found in UXML");
            skipVersionButton.clicked += OnSkipVersionClicked;

            var doNotShowAgainButton = root.Q<Button>("btn-do-not-show-again");
            if (doNotShowAgainButton == null)
                throw new System.InvalidOperationException("btn-do-not-show-again Button not found in UXML");
            doNotShowAgainButton.clicked += OnDoNotShowAgainClicked;
        }

        private void OnInstallUpdateClicked()
        {
            if (addRequest != null)
                return; // Already in progress

            addRequest = Client.Add($"{PackageId}@{latestVersion}");
            EditorApplication.update += OnPackageInstallProgress;

            // Disable the button to prevent multiple clicks
            var installButton = rootVisualElement.Q<Button>("btn-install-update");
            if (installButton == null)
                throw new System.InvalidOperationException("btn-install-update Button not found in UXML");
            installButton.SetEnabled(false);
            installButton.text = "Installing...";
        }

        private void OnPackageInstallProgress()
        {
            if (addRequest == null)
            {
                EditorApplication.update -= OnPackageInstallProgress;
                return;
            }

            if (!addRequest.IsCompleted)
                return; // wait until completed

            EditorApplication.update -= OnPackageInstallProgress;

            if (addRequest.Status == StatusCode.Success)
            {
                UnityMcpPlugin.Instance.LogInfo("Package updated to version {version}", typeof(UpdatePopupWindow), latestVersion);
                EditorUtility.DisplayDialog(
                    "Update Complete",
                    $"AI Game Developer has been updated to version {latestVersion}.\n\nUnity will recompile scripts automatically.",
                    "OK");
            }
            else if (addRequest.Status >= StatusCode.Failure)
            {
                var errorMessage = addRequest.Error?.message ?? "Unknown error";
                UnityMcpPlugin.Instance.LogError("Failed to update package: {error}", typeof(UpdatePopupWindow), errorMessage);
                EditorUtility.DisplayDialog(
                    "Update Failed",
                    $"Failed to update the package:\n{errorMessage}",
                    "OK");

                // Re-enable the button on failure
                var installButton = rootVisualElement.Q<Button>("btn-install-update");
                if (installButton != null)
                {
                    installButton.SetEnabled(true);
                    installButton.text = "Install Update";
                }
            }

            addRequest = null;
            Close();
        }

        private void OnViewReleasesClicked()
        {
            Application.OpenURL(UpdateChecker.ReleasesUrl);
        }

        private void OnSkipVersionClicked()
        {
            UpdateChecker.SkipVersion(latestVersion);
            Close();
        }

        private void OnDoNotShowAgainClicked()
        {
            UpdateChecker.IsDoNotShowAgain = true;
            Close();
        }

        void OnDestroy()
        {
            EditorApplication.update -= OnPackageInstallProgress;
        }
    }
}
