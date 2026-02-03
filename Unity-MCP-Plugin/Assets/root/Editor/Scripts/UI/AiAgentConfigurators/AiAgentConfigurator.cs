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
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Base class for AI agent configurator UI components.
    /// Each AI agent has its own configurator that provides specific configuration instructions.
    /// </summary>
    public abstract class AiAgentConfigurator
    {
        #region Properties

        private AiAgentConfig? _clientConfigStdio;
        private AiAgentConfig? _clientConfigHttp;

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
        /// The tutorial URL for configuring the AI agent.
        /// </summary>
        public virtual string TutorialUrl => string.Empty;

        /// <summary>
        /// Gets the icon file name for this agent (e.g., "claude-64.png").
        /// Return null if no icon should be displayed.
        /// </summary>
        protected abstract string? IconFileName { get; }

        protected VisualElement? Root { get; private set; }

        /// <summary>
        /// Gets the icon paths for this agent.
        /// </summary>
        protected string[]? IconPaths => IconFileName != null
            ? EditorAssetLoader.GetEditorAssetPaths($"Editor/Gizmos/ai-agents/{IconFileName}")
            : null;

        /// <summary>
        /// Gets the agent configuration for the current platform.
        /// </summary>
        public AiAgentConfig ClientConfigStdio
        {
            get
            {
                if (_clientConfigStdio == null)
                {
#if UNITY_EDITOR_WIN
                    _clientConfigStdio = CreateConfigStdioWindows();
#else
                    _clientConfigStdio = CreateConfigMacLinux();
#endif
                }
                return _clientConfigStdio;
            }
        }

        /// <summary>
        /// Gets the agent configuration for the current platform.
        /// </summary>
        public AiAgentConfig ClientConfigHttp
        {
            get
            {
                if (_clientConfigHttp == null)
                {
#if UNITY_EDITOR_WIN
                    _clientConfigHttp = CreateConfigHttpWindows();
#else
                    _clientConfigHttp = CreateConfigHttpMacLinux();
#endif
                }
                return _clientConfigHttp;
            }
        }

        /// <summary>
        /// Gets the Unity project root path (without /Assets suffix).
        /// </summary>
        protected static string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

        #endregion

        #region Abstract

        /// <summary>
        /// Creates the AI agent STDIO configuration for Windows platform.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigStdioWindows();

        /// <summary>
        /// Creates the AI agent STDIO configuration for Mac and Linux platforms.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigStdioMacLinux();

        /// <summary>
        /// Creates the AI agent HTTP configuration for Windows platform.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigHttpWindows();

        /// <summary>
        /// Creates the AI agent HTTP configuration for Mac and Linux platforms.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigHttpMacLinux();

        #endregion

        #region UI Templates

        protected Label TemplateLabelDescription() => new UITemplate<Label>("TemplateLabelDescription").Value;
        protected Label TemplateWarningLabel() => new UITemplate<Label>("TemplateWarningLabel").Value;
        protected Label TemplateAlertLabel() => new UITemplate<Label>("TemplateAlertLabel").Value;
        protected TextField TemplateTextFieldReadOnly() => new UITemplate<TextField>("TemplateTextFieldReadOnly").Value;
        protected Foldout TemplateFoldoutFirst() => new UITemplate<Foldout>("TemplateFoldoutFirst").Value;
        protected Foldout TemplateFoldout() => new UITemplate<Foldout>("TemplateFoldout").Value;
        protected ConfigurationElements TemplateConfigurationElements() => new ConfigurationElements();

        public class ConfigurationElements
        {
            public VisualElement Root { get; }
            public VisualElement StatusCircle { get; }
            public Label StatusText { get; }
            public Button BtnConfigure { get; }

            public ConfigurationElements()
            {
                Root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateConfigureStatus.uxml").Value;
                StatusCircle = Root.Q<VisualElement>("configureStatusCircle") ?? throw new NullReferenceException("VisualElement 'configureStatusCircle' not found in UI.");
                StatusText = Root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
                BtnConfigure = Root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");
            }
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// Creates and returns the visual element containing the configuration UI for this client.
        /// </summary>
        /// <param name="container">The parent container where the UI will be added.</param>
        /// <returns>The created visual element, or null if the template couldn't be loaded.</returns>
        public virtual VisualElement? CreateUI(VisualElement container)
        {
            var paths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/AiAgentTemplateConfigure.uxml");
            var template = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(paths) ?? throw new NullReferenceException("Failed to load UXML template for AiAgentTemplateConfigure.");
            var root = template.CloneTree();
            Root = root;
            OnUICreated(root);
            McpWindowBase.EnableSmoothFoldoutTransitions(root);
            return root;
        }

        /// <summary>
        /// Called after the UI is created. Override to add custom behavior or bindings.
        /// </summary>
        /// <param name="root">The root visual element of the created UI.</param>
        protected virtual void OnUICreated(VisualElement root)
        {
            SetAgentIcon();
            SetAgentDownloadUrl(DownloadUrl);
            SetTutorialUrl(TutorialUrl);
            SetConfigureStatusIndicator();
        }

        protected virtual AiAgentConfigurator SetAgentDownloadUrl(string url)
        {
            ThrowIfRootNotSet();
            var downloadLink = Root!.Q<Label>("downloadLink");
            if (downloadLink != null)
                downloadLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(DownloadUrl));
            return this;
        }

        protected virtual AiAgentConfigurator SetTutorialUrl(string url, string label = "YouTube")
        {
            ThrowIfRootNotSet();
            var tutorialLink = Root!.Q<Label>("tutorialLink");
            if (tutorialLink != null)
            {
                tutorialLink.text = label;

                if (TutorialUrl == string.Empty)
                {
                    tutorialLink.style.display = DisplayStyle.None;
                    var tutorialSeparator = Root!.Q<Label>("tutorialSeparator");
                    if (tutorialSeparator != null)
                        tutorialSeparator.style.display = DisplayStyle.None;
                }
                else
                {
                    tutorialLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(TutorialUrl));
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the agent icon on the agentIcon element.
        /// </summary>
        /// <param name="root">The root visual element containing the agentIcon element.</param>
        protected virtual AiAgentConfigurator SetAgentIcon()
        {
            ThrowIfRootNotSet();
            var agentIcon = Root!.Q<VisualElement>("agentIcon") ?? throw new NullReferenceException("VisualElement 'agentIcon' not found in UI.");

            if (IconPaths == null)
            {
                agentIcon.style.display = DisplayStyle.None;
                return this;
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
            return this;
        }

        protected virtual AiAgentConfigurator SetConfigureStatusIndicator()
        {
            ThrowIfRootNotSet();
            var statusCircle = Root!.Q<VisualElement>("configureStatusCircle") ?? throw new NullReferenceException("VisualElement 'configureStatusCircle' not found in UI.");
            var statusText = Root!.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
            var btnConfigure = Root!.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");

            var isConfiguredResult = ClientConfigStdio.IsConfigured();

            statusCircle.RemoveFromClassList(MainWindowEditor.USS_Connected);
            statusCircle.RemoveFromClassList(MainWindowEditor.USS_Connecting);
            statusCircle.RemoveFromClassList(MainWindowEditor.USS_Disconnected);

            statusCircle.AddToClassList(isConfiguredResult
                ? MainWindowEditor.USS_Connected
                : MainWindowEditor.USS_Disconnected);
            statusText.text = isConfiguredResult ? "Configured (stdio)" : "Not Configured";
            btnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            btnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = ClientConfigStdio.Configure();

                statusText.text = configureResult ? "Configured (stdio)" : "Not Configured";

                statusCircle.RemoveFromClassList(MainWindowEditor.USS_Connected);
                statusCircle.RemoveFromClassList(MainWindowEditor.USS_Connecting);
                statusCircle.RemoveFromClassList(MainWindowEditor.USS_Disconnected);

                statusCircle.AddToClassList(configureResult
                    ? MainWindowEditor.USS_Connected
                    : MainWindowEditor.USS_Disconnected);

                btnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
            return this;
        }

        #endregion

        #region Helpers

        protected void ThrowIfRootNotSet()
        {
            if (Root == null)
                throw new InvalidOperationException("Root visual element is not set. Ensure CreateUI has been called.");
        }

        #endregion
    }
}
