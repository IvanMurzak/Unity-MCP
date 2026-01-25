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
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Base class for MCP client configurator UI components.
    /// Each MCP client has its own configurator that provides specific configuration instructions.
    /// </summary>
    public abstract class McpClientConfiguratorBase
    {
        /// <summary>
        /// The display name of the MCP client.
        /// </summary>
        public abstract string ClientName { get; }

        /// <summary>
        /// The unique identifier for this client (used for dropdown values and PlayerPrefs).
        /// </summary>
        public abstract string ClientId { get; }

        /// <summary>
        /// Gets the UXML template paths for this client's configuration UI.
        /// </summary>
        protected abstract string[] UxmlPaths { get; }

        /// <summary>
        /// Creates and returns the visual element containing the configuration UI for this client.
        /// </summary>
        /// <param name="container">The parent container where the UI will be added.</param>
        /// <returns>The created visual element, or null if the template couldn't be loaded.</returns>
        public virtual VisualElement? CreateUI(VisualElement container)
        {
            var template = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(UxmlPaths);
            if (template == null)
            {
                UnityEngine.Debug.LogError($"Failed to load UXML template for {ClientName} configurator.");
                return null;
            }

            var element = template.CloneTree();
            OnUICreated(element);
            return element;
        }

        /// <summary>
        /// Called after the UI is created. Override to add custom behavior or bindings.
        /// </summary>
        /// <param name="root">The root visual element of the created UI.</param>
        protected virtual void OnUICreated(VisualElement root)
        {
            // Override in derived classes to add custom initialization
        }
    }
}
