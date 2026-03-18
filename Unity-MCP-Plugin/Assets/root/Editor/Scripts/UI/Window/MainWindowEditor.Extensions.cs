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
using UnityEditor.PackageManager;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private static readonly string[] _extensionItemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ExtensionItem.uxml");

        private static readonly (string name, string description, string gitUrl)[] _extensions =
        {
            (
                "Animation",
                "AI-driven animation control and playback tools.",
                "https://github.com/IvanMurzak/Unity-AI-Animation.git"
            ),
            (
                "ParticleSystem",
                "AI-powered particle system creation and control tools.",
                "https://github.com/IvanMurzak/Unity-AI-ParticleSystem.git"
            ),
            (
                "ProBuilder",
                "AI-assisted ProBuilder geometry modeling tools.",
                "https://github.com/IvanMurzak/Unity-AI-ProBuilder.git"
            ),
        };

        private void SetupExtensionsSection(VisualElement root)
        {
            var container = root.Q<VisualElement>("ExtensionsSection");
            if (container == null)
                return;

            var itemTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(_extensionItemUxmlPaths);
            if (itemTemplate == null)
                return;

            foreach (var (name, description, gitUrl) in _extensions)
            {
                var item = itemTemplate.CloneTree();

                item.Q<Label>("extension-name").text = name;
                item.Q<Label>("extension-desc").text = description;

                var installBtn = item.Q<Button>("extension-install-btn");
                installBtn.tooltip = $"Install {name} extension via Unity Package Manager.\nPackage: {gitUrl}";

                var capturedUrl = gitUrl;
                installBtn.RegisterCallback<ClickEvent>(_ => Client.Add(capturedUrl));

                container.Add(item);
            }
        }
    }
}
