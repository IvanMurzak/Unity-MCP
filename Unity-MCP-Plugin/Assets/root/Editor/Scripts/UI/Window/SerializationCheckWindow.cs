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
using System.Collections.Generic;
using System.Diagnostics;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
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
        private Button? copyButton;
        private Label? outputHeader;
        private ListView? outputList;
        private readonly List<string> outputLines = new();
        private string fullOutputText = string.Empty;

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

            // Bind copy button
            copyButton = root.Q<Button>("btn-copy");
            if (copyButton == null)
                throw new InvalidOperationException("btn-copy Button not found in UXML");
            copyButton.clicked += OnCopyClicked;

            // Bind output header
            outputHeader = root.Q<Label>("output-header");
            if (outputHeader == null)
                throw new InvalidOperationException("output-header Label not found in UXML");

            // Bind output list
            outputList = root.Q<ListView>("output-list");
            if (outputList == null)
                throw new InvalidOperationException("output-list ListView not found in UXML");

            outputList.itemsSource = outputLines;
            outputList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            outputList.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("json-text-line");
                return label;
            };
            outputList.bindItem = (element, index) =>
            {
                if (element is Label label && index < outputLines.Count)
                    label.text = outputLines[index];
            };
            outputList.selectionType = SelectionType.None;
        }

        private void OnSerializeClicked()
        {
            if (targetField == null || recursiveToggle == null || outputList == null || outputHeader == null)
                return;

            var target = targetField.value;
            var recursive = recursiveToggle.value;

            try
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(SerializationCheckWindow));
                var reflector = UnityMcpPlugin.Instance.Reflector ?? throw new InvalidOperationException("Reflector is null");

                logger.LogInformation($"Serializing target '{target?.name}' of type '{target?.GetType().GetTypeId()}' with recursive={recursive}");

                var stopwatch = Stopwatch.StartNew();

                var serialized = reflector.Serialize(
                    obj: target,
                    fallbackType: null,
                    name: target?.name,
                    recursive: recursive,
                    context: null,
                    logger: logger);

                var json = serialized.ToPrettyJson();

                stopwatch.Stop();

                logger.LogInformation(json);

                outputHeader.text = $"Output ({stopwatch.ElapsedMilliseconds}ms)";
                SetOutput(json);
            }
            catch (Exception ex)
            {
                outputHeader.text = "Output";
                SetOutput($"Error: {ex.Message}\n\n{ex.StackTrace}");
                Logger.LogError(ex, "Failed to serialize target");
            }
        }

        private void SetOutput(string text)
        {
            fullOutputText = text;
            outputLines.Clear();
            outputLines.AddRange(text.Split('\n'));
            outputList?.RefreshItems();
        }

        private void OnCopyClicked()
        {
            EditorGUIUtility.systemCopyBuffer = fullOutputText;

            if (copyButton != null)
            {
                var originalText = copyButton.text;
                copyButton.text = "Copied!";

                copyButton.schedule.Execute(() =>
                {
                    if (copyButton != null)
                    {
                        copyButton.text = originalText;
                    }
                }).ExecuteLater(1500);
            }
        }

        private void OnDestroy()
        {
            if (serializeButton != null)
                serializeButton.clicked -= OnSerializeClicked;
            if (copyButton != null)
                copyButton.clicked -= OnCopyClicked;
        }
    }
}
