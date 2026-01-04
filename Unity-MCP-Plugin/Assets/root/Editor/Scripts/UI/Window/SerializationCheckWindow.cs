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
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// An editor window for testing object serialization using the MCP reflector.
    /// </summary>
    public class SerializationCheckWindow : McpWindowBase
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/SerializationCheckWindow.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/SerializationCheckWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "Serialization Check";

        private ObjectField? targetField;
        private Toggle? recursiveToggle;
        private Button? serializeButton;
        private TextField? outputField;

        public static void ShowWindow()
        {
            var window = GetWindow<SerializationCheckWindow>(utility: false, "Serialization Check", focus: true);
            window.SetupWindowWithIcon();
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        protected override void OnGUICreated(VisualElement root)
        {
            BindUI(root);
        }

        private void BindUI(VisualElement root)
        {
            // Bind target object field
            targetField = root.Q<ObjectField>("target-field");
            if (targetField == null)
                throw new InvalidOperationException("target-field ObjectField not found in UXML");
            targetField.objectType = typeof(UnityEngine.Object);

            // Bind recursive toggle
            recursiveToggle = root.Q<Toggle>("recursive-toggle");
            if (recursiveToggle == null)
                throw new InvalidOperationException("recursive-toggle Toggle not found in UXML");

            // Bind serialize button
            serializeButton = root.Q<Button>("btn-serialize");
            if (serializeButton == null)
                throw new InvalidOperationException("btn-serialize Button not found in UXML");
            serializeButton.clicked += OnSerializeClicked;

            // Bind output field
            outputField = root.Q<TextField>("output-field");
            if (outputField == null)
                throw new InvalidOperationException("output-field TextField not found in UXML");
        }

        private void OnSerializeClicked()
        {
            if (targetField == null || recursiveToggle == null || outputField == null)
                return;

            var target = targetField.value;
            var recursive = recursiveToggle.value;

            try
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(SerializationCheckWindow));
                var reflector = UnityMcpPlugin.Instance.Reflector ?? throw new InvalidOperationException("Reflector is null");

                logger.LogInformation($"Serializing target '{target?.name}' of type '{target?.GetType().GetTypeId()}' with recursive={recursive}");

                var serialized = reflector.Serialize(
                    obj: target,
                    fallbackType: null,
                    name: target?.name,
                    recursive: recursive,
                    context: null,
                    logger: logger);

                var json = serialized.ToPrettyJson();
                logger.LogInformation(json);

                outputField.value = json;
            }
            catch (Exception ex)
            {
                outputField.value = $"Error: {ex.Message}\n\n{ex.StackTrace}";
                Logger.LogError(ex, "Failed to serialize target");
            }
        }

        private void OnDestroy()
        {
            if (serializeButton != null)
                serializeButton.clicked -= OnSerializeClicked;
        }
    }
}
