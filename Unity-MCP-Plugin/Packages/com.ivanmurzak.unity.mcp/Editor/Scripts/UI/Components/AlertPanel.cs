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
    /// <summary>
    /// Reusable alert panel component backed by the TemplateAlertPanel.uxml template.
    /// Provides a styled notification panel with title, message, optional list items, and an optional action button.
    /// </summary>
    public class AlertPanel
    {
        private static readonly string[] TemplatePaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/elements/TemplateAlertPanel.uxml");

        private readonly Label _title;
        private readonly Label _message;
        private readonly VisualElement _itemsContainer;
        private readonly Button _button;
        private EventCallback<ClickEvent>? _buttonClickCallback;

        /// <summary>
        /// The root visual element of the alert panel. Add this to a parent container.
        /// </summary>
        public VisualElement Root { get; }

        /// <summary>
        /// Creates a new alert panel from the UXML template.
        /// </summary>
        /// <param name="title">The alert title text.</param>
        /// <param name="message">The alert message text.</param>
        public AlertPanel(string title, string message)
        {
            var template = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(TemplatePaths)
                ?? throw new NullReferenceException($"Failed to load TemplateAlertPanel.uxml. Checked: {string.Join(", ", TemplatePaths)}");

            var tree = template.CloneTree();
            Root = tree.Q<VisualElement>("alertPanel")
                ?? throw new NullReferenceException("VisualElement 'alertPanel' not found in TemplateAlertPanel.uxml.");

            _title = Root.Q<Label>("alertTitle") ?? throw new NullReferenceException("Label 'alertTitle' not found in TemplateAlertPanel.uxml.");
            _message = Root.Q<Label>("alertMessage") ?? throw new NullReferenceException("Label 'alertMessage' not found in TemplateAlertPanel.uxml.");
            _itemsContainer = Root.Q<VisualElement>("alertItemsContainer") ?? throw new NullReferenceException("VisualElement 'alertItemsContainer' not found in TemplateAlertPanel.uxml.");
            _button = Root.Q<Button>("alertButton") ?? throw new NullReferenceException("Button 'alertButton' not found in TemplateAlertPanel.uxml.");

            _title.text = title;
            _message.text = message;
        }

        /// <summary>
        /// Adds a text item (e.g. a bullet point) to the alert panel.
        /// </summary>
        /// <param name="text">The item text.</param>
        /// <param name="cssClasses">Additional USS classes to apply to the item label.</param>
        /// <returns>This instance for fluent chaining.</returns>
        public AlertPanel AddItem(string text, params string[] cssClasses)
        {
            var item = new Label(text);
            item.AddToClassList("alert-frame-item");
            foreach (var cls in cssClasses)
                item.AddToClassList(cls);
            _itemsContainer.Add(item);
            return this;
        }

        /// <summary>
        /// Configures the action button on the alert panel.
        /// </summary>
        /// <param name="text">Button label text.</param>
        /// <param name="onClick">Click handler.</param>
        /// <param name="cssClasses">Additional USS classes to apply (the button already has btn-primary).</param>
        /// <returns>This instance for fluent chaining.</returns>
        public AlertPanel SetButton(string text, Action onClick, params string[] cssClasses)
        {
            _button.text = text;
            _button.style.display = DisplayStyle.Flex;

            if (_buttonClickCallback != null)
                _button.UnregisterCallback(_buttonClickCallback);

            _buttonClickCallback = _ => onClick();
            _button.RegisterCallback(_buttonClickCallback);

            foreach (var cls in cssClasses)
                _button.AddToClassList(cls);
            return this;
        }

        /// <summary>
        /// Shows the alert panel.
        /// </summary>
        public void Show() => Root.style.display = DisplayStyle.Flex;

        /// <summary>
        /// Hides the alert panel.
        /// </summary>
        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Sets the visibility of the alert panel.
        /// </summary>
        /// <param name="visible">True to show, false to hide.</param>
        public void SetVisible(bool visible) => Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
