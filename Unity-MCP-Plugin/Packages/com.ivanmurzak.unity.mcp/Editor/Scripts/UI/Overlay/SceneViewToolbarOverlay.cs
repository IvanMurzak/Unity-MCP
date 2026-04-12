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
using UnityEditor.Overlays;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    [Overlay(typeof(SceneView), id: Id, displayName: "AI",
        defaultDisplay = true,
        defaultDockZone = DockZone.TopToolbar,
        defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 0,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon(EditorAssetLoader.PackageLogoIconPath)]
    public class SceneViewToolbarOverlay : ToolbarOverlay
    {
        public const string Id = "ai-game-developer-toolbar";

        private SceneViewToolbarOverlay() : base(OpenWindowButton.Id)
        {
            collapsedIcon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(EditorAssetLoader.PackageLogoIcon);
        }
    }
}
