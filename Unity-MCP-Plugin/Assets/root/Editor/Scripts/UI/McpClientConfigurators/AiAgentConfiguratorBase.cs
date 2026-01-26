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
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Base class for MCP client configurator UI components.
    /// Each MCP client has its own configurator that provides specific configuration instructions.
    /// </summary>
    public abstract class AiAgentConfiguratorBase
    {
        private AiAgentConfig? _clientConfig;

        /// <summary>
        /// The display name of the AI agent.
        /// </summary>
        public abstract string AgentName { get; }

        /// <summary>
        /// The unique identifier for this agent (used for dropdown values and PlayerPrefs).
        /// </summary>
        public abstract string AgentId { get; }

        /// <summary>
        /// The download URL for the AI agent.
        /// </summary>
        public abstract string DownloadUrl { get; }

        /// <summary>
        /// Gets the UXML template paths for this agent's configuration UI.
        /// </summary>
        protected abstract string[] UxmlPaths { get; }

        /// <summary>
        /// Gets the icon file name for this agent (e.g., "claude-64.png").
        /// Return null if no icon should be displayed.
        /// </summary>
        protected abstract string? IconFileName { get; }

        /// <summary>
        /// Gets the icon paths for this agent.
        /// </summary>
        protected string[]? IconPaths => IconFileName != null
            ? EditorAssetLoader.GetEditorAssetPaths($"Editor/Gizmos/ai-agents/{IconFileName}")
            : null;

        /// <summary>
        /// Gets the agent configuration for the current platform.
        /// </summary>
        public AiAgentConfig ClientConfig
        {
            get
            {
                if (_clientConfig == null)
                {
#if UNITY_EDITOR_WIN
                    _clientConfig = CreateConfigWindows();
#else
                    _clientConfig = CreateConfigMacLinux();
#endif
                }
                return _clientConfig;
            }
        }

        /// <summary>
        /// Gets the Unity project root path (without /Assets suffix).
        /// </summary>
        protected static string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        /// <summary>
        /// Creates the client configuration for Windows platform.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigWindows();

        /// <summary>
        /// Creates the client configuration for Mac and Linux platforms.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigMacLinux();

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
                UnityEngine.Debug.LogError($"Failed to load UXML template for {AgentName} configurator.");
                return null;
            }

            var element = template.CloneTree();
            OnUICreated(element);
            McpWindowBase.EnableSmoothFoldoutTransitions(element);
            return element;
        }

        /// <summary>
        /// Called after the UI is created. Override to add custom behavior or bindings.
        /// </summary>
        /// <param name="root">The root visual element of the created UI.</param>
        protected virtual void OnUICreated(VisualElement root)
        {
            var downloadLink = root.Q<Label>("downloadLink");
            if (downloadLink != null)
                downloadLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(DownloadUrl));

            SetAgentIcon(root);
            CreateConfigureStatusIndicator(root);
        }

        /// <summary>
        /// Sets the agent icon on the agentIcon element.
        /// </summary>
        /// <param name="root">The root visual element containing the agentIcon element.</param>
        protected virtual void SetAgentIcon(VisualElement root)
        {
            var agentIcon = root.Q<VisualElement>("agentIcon");
            if (agentIcon == null)
                return;

            if (IconPaths == null)
            {
                agentIcon.style.display = DisplayStyle.None;
                return;
            }

            var icon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(IconPaths);
            if (icon != null)
            {
                agentIcon.style.backgroundImage = new StyleBackground(icon);
            }
            else
            {
                agentIcon.style.display = DisplayStyle.None;
            }
        }

        protected virtual void CreateConfigureStatusIndicator(VisualElement root)
        {
            var statusCircle = root.Q<VisualElement>("configureStatusCircle");
            var statusText = root.Q<Label>("configureStatusText");
            var btnConfigure = root.Q<Button>("btnConfigure");

            if (statusCircle == null || statusText == null || btnConfigure == null)
            {
                Debug.LogWarning($"Config panel elements not found in client UI for {ClientConfig.Name}.");
                return;
            }

            var isConfiguredResult = ClientConfig.IsConfigured();

            statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Connected);
            statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Connecting);
            statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Disconnected);

            statusCircle.AddToClassList(isConfiguredResult
                ? MainWindowEditor.USS_IndicatorClass_Connected
                : MainWindowEditor.USS_IndicatorClass_Disconnected);
            statusText.text = isConfiguredResult ? "Configured (stdio)" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = ClientConfig.Configure();

                statusText.text = configureResult ? "Configured (stdio)" : "Not Configured";

                statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Connected);
                statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Connecting);
                statusCircle.RemoveFromClassList(MainWindowEditor.USS_IndicatorClass_Disconnected);

                statusCircle.AddToClassList(configureResult
                    ? MainWindowEditor.USS_IndicatorClass_Connected
                    : MainWindowEditor.USS_IndicatorClass_Disconnected);

                btnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
        }
    }
}
