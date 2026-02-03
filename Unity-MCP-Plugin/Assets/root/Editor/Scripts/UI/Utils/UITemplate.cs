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
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public class UITemplate<T> where T : VisualElement
    {
        public T Value { get; private set; }

        public UITemplate(string templatePath)
        {
            var paths = EditorAssetLoader.GetEditorAssetPaths(templatePath);
            var template = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(paths) ?? throw new NullReferenceException($"Failed to load UXML template at path: {templatePath}");
            var root = template.CloneTree();
            Value = root as T ?? throw new InvalidCastException($"Root element is not of type {typeof(T).Name}");
        }
    }
}