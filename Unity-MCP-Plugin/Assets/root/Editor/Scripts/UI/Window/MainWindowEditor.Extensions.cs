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
using UnityEditor.PackageManager;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
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

            foreach (var (name, description, gitUrl) in _extensions)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2;
                row.style.marginBottom = 2;

                var nameLabel = new Label(name);
                nameLabel.AddToClassList("header");
                nameLabel.style.flexShrink = 0;
                nameLabel.style.marginBottom = 0;

                var descLabel = new Label(description);
                descLabel.AddToClassList("section-desc");
                descLabel.style.flexGrow = 1;
                descLabel.style.flexShrink = 1;
                descLabel.style.marginBottom = 0;
                descLabel.style.marginLeft = 6;

                var installBtn = new Button();
                installBtn.text = "Install";
                installBtn.AddToClassList("btn-compact");
                installBtn.AddToClassList("btn-secondary");
                installBtn.tooltip = $"Install {name} extension via Unity Package Manager.\nPackage: {gitUrl}";

                var capturedUrl = gitUrl;
                installBtn.RegisterCallback<ClickEvent>(_ => Client.Add(capturedUrl));

                row.Add(nameLabel);
                row.Add(descLabel);
                row.Add(installBtn);

                container.Add(row);
            }
        }
    }
}
